using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using Octokit;

namespace Internal.AspNetCore.ReportGenerator.Reports
{
    public static class StatusReport
    {
        private static readonly Regex _repoNameFromPRUrlExtractor = new Regex(@"^https://github.com/(?<owner>.*)/(?<name>.*)/pull/\d+$");

        public static async Task<StatusReportModel> GenerateReportModelAsync(GitHubClient github, DataStore data, Data.Team team, string milestone, DateTime? startDate, DateTime? endDate)
        {
            var model = new StatusReportModel();
            model.Name = team.Name;
            model.ReportDate = DateTime.Now.ToString("yyyy-MM-dd");

            var cache = new GitHubCache(github, data);

            // Establish the time window
            var now = DateTime.Now;
            endDate = endDate ?? new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Local);
            startDate = startDate ?? (endDate.Value.AddDays(-7));
            var range = new DateRange(startDate.Value, endDate.Value);

            var releaseMilestone = data.GetMilestone(milestone);
            model.Milestone = new List<MilestoneDates>()
            {
                ComputeMilestoneDates(data, endDate.Value, releaseMilestone)
            };

            var repos = new RepositoryCollection();
            foreach (var repo in data.Organization.Repositories)
            {
                repos.Add(repo.Owner, repo.Name);
            }

            await GeneratePullRequestInfoAsync(github, data, team, model, cache, range, repos);
            model.Areas = await GenerateIssueInfoAsync(github, team, repos, milestone);

            return model;
        }

        private static MilestoneDates ComputeMilestoneDates(DataStore data, DateTime endDate, ReleaseMilestone releaseMilestone)
        {
            // Identify the number of working days between the end date and the start date
            // Look. I know this could be done with cool math, but this is the easiest thing right now :). We can optimize later
            var current = endDate;
            var workDays = 0;
            while(current <= releaseMilestone.BranchCloses!.Value)
            {
                if(!data.IsHoliday(current, WorkLocation.US) && current.DayOfWeek != DayOfWeek.Sunday && current.DayOfWeek != DayOfWeek.Saturday)
                {
                    workDays += 1;
                }
                current = current.AddDays(1);
            }

            return new MilestoneDates()
            {
                Name = releaseMilestone.Name,
                BranchCloses = releaseMilestone.BranchCloses!.Value.ToString("yyyy-MM-dd"),
                WorkDaysRemaining = workDays,
            };
        }

        private static async Task<IList<AreaSummaryModel>> GenerateIssueInfoAsync(GitHubClient github, Data.Team team, RepositoryCollection repos, string milestone)
        {
            var areaSummaries = new List<AreaSummaryModel>();
            foreach (var area in team.AreaLabels)
            {
                var query = new SearchIssuesRequest()
                {
                    Is = new[] { IssueIsQualifier.Issue },
                    Labels = new[] { area },
                    Milestone = milestone,
                    Repos = repos,
                };
                var results = await github.Search.SearchIssues(query);

                var open = 0;
                var closed = 0;
                var accepted = 0;
                foreach(var result in results.Items)
                {
                    if(result.State == ItemState.Open)
                    {
                        open += 1;
                    }
                    else if(result.Labels.Any(l => l.Name.Equals("accepted")))
                    {
                        Debug.Assert(result.State == ItemState.Closed);
                        accepted += 1;
                    }
                    else
                    {
                        Debug.Assert(result.State == ItemState.Closed);
                        closed += 1;
                    }
                }
                areaSummaries.Add(new AreaSummaryModel()
                {
                    Label = area,
                    Open = open,
                    Closed = closed,
                    Accepted = accepted
                });
            }
            return areaSummaries;
        }

        private static async Task GeneratePullRequestInfoAsync(GitHubClient github, DataStore data, Data.Team team, StatusReportModel model, GitHubCache cache, DateRange range, RepositoryCollection repos)
        {
            // Collect merged PRs
            var mergedPrs = new HashSet<Issue>(IssueByIdEqualityComparer.Instance);
            await CollectByAreas(github, team, range, repos, mergedPrs);
            await CollectByAuthor(data, github, cache, team, range, repos, mergedPrs);

            // Group by author
            var groupedPrs = mergedPrs.GroupBy(p => p.User.Login);

            // Filter into internal and external lists
            var internalUsers = await cache.GetInternalUsersAsync();
            var internalPrs = groupedPrs.Where(g => internalUsers.Contains(g.Key));
            var externalPrs = groupedPrs.Where(g => !internalUsers.Contains(g.Key));

            // Build the models
            model.InternalContributors = await CreateContributorModelAsync(internalPrs, cache);
            model.ExternalContributors = await CreateContributorModelAsync(externalPrs, cache);
        }

        private static async Task<IList<ContributorModel>> CreateContributorModelAsync(IEnumerable<IGrouping<string, Issue>> groups, GitHubCache cache)
        {
            var list = new List<ContributorModel>();
            foreach (var group in groups)
            {
                list.Add(await CreateContributorModelAsync(group, cache));
            }
            return list;
        }

        private static async Task<ContributorModel> CreateContributorModelAsync(IGrouping<string, Issue> prs, GitHubCache cache)
        {
            var user = await cache.GetUserAsync(prs.Key);
            return new ContributorModel()
            {
                AvatarUrl = user.AvatarUrl,
                DisplayName = string.IsNullOrEmpty(user.Name) ? user.Login : $"{user.Name} ({user.Login})",
                ProfileUrl = user.HtmlUrl,
                PullRequests = prs.Select(i => CreatePrSummary(i)).ToList(),
            };
        }

        private static PullRequestSummaryModel CreatePrSummary(Issue i)
        {
            var match = _repoNameFromPRUrlExtractor.Match(i.HtmlUrl);
            if(!match.Success)
            {
                throw new InvalidDataException($"Bad PR HtmlUrl: {i.HtmlUrl}");
            }

            return new PullRequestSummaryModel()
            {
                RepoOwner = match.Groups["owner"].Value,
                RepoName = match.Groups["name"].Value,
                Number = i.Number,
                Title = i.Title,
                Url = i.Url,
            };
        }

        private static async Task CollectByAuthor(Data.DataStore data, GitHubClient github, GitHubCache cache, Data.Team team, DateRange range, RepositoryCollection repos, HashSet<Issue> mergedPrs)
        {
            var teamMembers = await cache.GetTeamMembersAsync(team.GitHubTeam);
            foreach (var user in teamMembers)
            {
                var request = new SearchIssuesRequest()
                {
                    Is = new[] { IssueIsQualifier.PullRequest, IssueIsQualifier.Merged },
                    Author = user,
                    Merged = range,
                    Repos = repos,
                };
                var results = await github.Search.SearchIssues(request);
                mergedPrs.UnionWith(results.Items);
            }
        }

        private static async Task CollectByAreas(GitHubClient github, Data.Team team, DateRange range, RepositoryCollection repos, HashSet<Issue> mergedPrs)
        {
            foreach (var area in team.AreaLabels)
            {
                var request = new SearchIssuesRequest()
                {
                    Is = new[] { IssueIsQualifier.PullRequest, IssueIsQualifier.Merged },
                    Labels = new[] { area },
                    Merged = range,
                    Repos = repos,
                };
                var results = await github.Search.SearchIssues(request);
                mergedPrs.UnionWith(results.Items);
            }
        }
    }
}

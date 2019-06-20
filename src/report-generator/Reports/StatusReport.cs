using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Internal.AspNetCore.ReportGenerator.Reports
{
    public class StatusReport
    {
        private static readonly Regex _repoNameFromPRUrlExtractor = new Regex(@"^https://github.com/(?<owner>.*)/(?<name>.*)/pull/\d+$");

        public static async Task<StatusReportModel> GenerateReportModelAsync(GitHubClient github, DataStore data, Data.Team team, string milestone, DateTime startDate, DateTime endDate, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<StatusReport>();
            var model = new StatusReportModel();
            model.Name = team.Name;
            model.ReportDate = endDate.ToString("yyyy-MM-dd");

            var cache = new GitHubCache(github, data, loggerFactory.CreateLogger<GitHubCache>());

            // Establish the time window. Exclude the endDate.
            var range = new DateRange(startDate, endDate.AddDays(-1));
            logger.LogInformation("Using date range {Range}", range);

            var releaseMilestone = data.GetMilestone(milestone);
            model.Milestone = new List<MilestoneDates>()
            {
                ComputeMilestoneDates(data, endDate, releaseMilestone, logger)
            };

            var repos = new RepositoryCollection();
            foreach (var repo in data.Organization.Repositories)
            {
                repos.Add(repo.Owner, repo.Name);
            }

            await GeneratePullRequestInfoAsync(github, data, team, model, cache, range, repos, logger);
            await GenerateIssueInfoAsync(github, team, repos, milestone, model, logger);

            // Try to render burndown data
            var burndown = await data.LoadBurndownAsync(milestone);

            // Check for burndown data for the end date
            if (!burndown.Weeks.Any(w => w.EndDate.Year == endDate.Year && w.EndDate.Month == endDate.Month && w.EndDate.Day == endDate.Day))
            {
                logger.LogWarning("No burndown data is associated with today's date! The burndown data in this report may be out-of-date.");
            }

            model.Burndown = GenerateBurndownModel(team, burndown);

            return model;
        }

        private static BurndownModel GenerateBurndownModel(Data.Team team, Burndown burndown)
        {
            var burndownModel = new BurndownModel()
            {
                Areas = team.AreaLabels.OrderBy(l => l).ToList(),
                Weeks = new List<WeekBurndownModel>(),
            };

            foreach (var week in burndown.Weeks)
            {
                var weekModel = new WeekBurndownModel()
                {
                    Date = week.EndDate.ToString("yyyy-MM-dd"),
                    Areas = new List<AreaBurndownModel>(),
                };
                var areaDict = week.Areas.ToDictionary(a => a.Label);
                foreach (var area in burndownModel.Areas)
                {
                    AreaBurndownModel areaModel;
                    if (areaDict.TryGetValue(area, out var areaBurndown))
                    {
                        areaModel = new AreaBurndownModel()
                        {
                            Label = area,
                            Open = areaBurndown.Open,
                            Closed = areaBurndown.Closed,
                            Accepted = areaBurndown.Accepted,
                        };
                    }
                    else
                    {
                        areaModel = new AreaBurndownModel()
                        {
                            Label = area,
                            Open = 0,
                            Closed = 0,
                            Accepted = 0,
                        };
                    }
                    weekModel.Areas.Add(areaModel);
                }
                burndownModel.Weeks.Add(weekModel);
            }

            return burndownModel;
        }

        private static MilestoneDates ComputeMilestoneDates(DataStore data, DateTime endDate, ReleaseMilestone releaseMilestone, ILogger logger)
        {
            // Identify the number of working days between the end date and the start date
            // Look. I know this could be done with cool math, but this is the easiest thing right now :). We can optimize later
            var current = endDate;
            var workDays = 0;
            logger.LogDebug("Computing work days in {Milestone}...", releaseMilestone.Name);
            while (current <= releaseMilestone.BranchCloses!.Value)
            {
                if (!data.IsHoliday(current, WorkLocation.US) && current.DayOfWeek != DayOfWeek.Sunday && current.DayOfWeek != DayOfWeek.Saturday)
                {
                    workDays += 1;
                }
                current = current.AddDays(1);
            }
            logger.LogDebug("Computed work days in {Milestone}", releaseMilestone.Name);

            return new MilestoneDates()
            {
                Name = releaseMilestone.Name,
                BranchCloses = releaseMilestone.BranchCloses!.Value.ToString("yyyy-MM-dd"),
                WorkDaysRemaining = workDays,
            };
        }

        private static async Task GenerateIssueInfoAsync(GitHubClient github, Data.Team team, RepositoryCollection repos, string milestone, StatusReportModel model, ILogger logger)
        {
            model.Areas = new List<AreaSummaryModel>();
            foreach (var area in team.AreaLabels)
            {
                var query = new SearchIssuesRequest()
                {
                    Is = new[] { IssueIsQualifier.Issue },
                    Labels = new[] { area },
                    Milestone = milestone,
                    Repos = repos,
                };
                var results = await github.SearchIssuesLogged(query, logger);

                var open = 0;
                var closed = 0;
                var accepted = 0;
                foreach (var result in results.Items)
                {
                    if (result.State == ItemState.Open)
                    {
                        model.TotalOpen += 1;
                        open += 1;
                    }
                    else if (result.Labels.Any(l => l.Name.Equals("accepted")))
                    {
                        Debug.Assert(result.State == ItemState.Closed);
                        model.TotalAccepted += 1;
                        accepted += 1;
                    }
                    else
                    {
                        Debug.Assert(result.State == ItemState.Closed);
                        model.TotalClosed += 1;
                        closed += 1;
                    }
                }
                model.Areas.Add(new AreaSummaryModel()
                {
                    Label = area,
                    Open = open,
                    Closed = closed,
                    Accepted = accepted
                });
            }
        }

        private static async Task GeneratePullRequestInfoAsync(GitHubClient github, DataStore data, Data.Team team, StatusReportModel model, GitHubCache cache, DateRange range, RepositoryCollection repos, ILogger logger)
        {
            // Collect merged PRs
            var mergedPrs = new HashSet<Issue>(IssueByIdEqualityComparer.Instance);
            await CollectByAreas(github, team, range, repos, mergedPrs, logger);
            await CollectByAuthor(data, github, cache, team, range, repos, mergedPrs, logger);

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
                Login = user.Login,
                PullRequests = prs.Select(i => CreatePrSummary(i)).ToList(),
            };
        }

        private static PullRequestSummaryModel CreatePrSummary(Issue i)
        {
            var match = _repoNameFromPRUrlExtractor.Match(i.HtmlUrl);
            if (!match.Success)
            {
                throw new InvalidDataException($"Bad PR HtmlUrl: {i.HtmlUrl}");
            }

            return new PullRequestSummaryModel()
            {
                RepoOwner = match.Groups["owner"].Value,
                RepoName = match.Groups["name"].Value,
                Number = i.Number,
                Title = i.Title,
                Url = i.HtmlUrl,
            };
        }

        private static async Task CollectByAuthor(Data.DataStore data, GitHubClient github, GitHubCache cache, Data.Team team, DateRange range, RepositoryCollection repos, HashSet<Issue> mergedPrs, ILogger logger)
        {
            var teamMembers = await cache.GetTeamMembersAsync(team.GitHubTeam);
            foreach (var user in teamMembers)
            {
                var query = new SearchIssuesRequest()
                {
                    Is = new[] { IssueIsQualifier.PullRequest, IssueIsQualifier.Merged },
                    Author = user,
                    Merged = range,
                    Repos = repos,
                };
                var results = await github.SearchIssuesLogged(query, logger);
                mergedPrs.UnionWith(results.Items);
            }
        }

        private static async Task CollectByAreas(GitHubClient github, Data.Team team, DateRange range, RepositoryCollection repos, HashSet<Issue> mergedPrs, ILogger logger)
        {
            foreach (var area in team.AreaLabels)
            {
                var query = new SearchIssuesRequest()
                {
                    Is = new[] { IssueIsQualifier.PullRequest, IssueIsQualifier.Merged },
                    Labels = new[] { area },
                    Merged = range,
                    Repos = repos,
                };
                var results = await github.SearchIssuesLogged(query, logger);
                mergedPrs.UnionWith(results.Items);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Internal.AspNetCore.ReportGenerator.Commands
{
    [Command("burndown", Description = "Updates Burndown data.")]
    internal class BurndownCommand: GitHubCommandBase
    {
        [Option("--milestone <MILESTONE>", Description = "The milestone to generate the data for.")]
        public string? Milestone { get; set; }

        public async Task<int> OnExecuteAsync()
        {
            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger<StatusCommand>();

            if (string.IsNullOrEmpty(Milestone))
            {
                throw new CommandLineException("Missing required option '--milestone'.");
            }

            logger.LogInformation("Preparing burndown for {Milestone}", Milestone);

            var data = await LoadDataStoreAsync();
            var github = GetGitHubClient();

            logger.LogDebug("Loading existing burndown...");
            var burndown = await data.LoadBurndownAsync(Milestone);
            logger.LogDebug("Loaded existing burndown.");

            var repos = new RepositoryCollection();
            foreach (var repo in data.Organization.Repositories)
            {
                repos.Add(repo.Owner, repo.Name);
            }

            // Query for issues
            var query = new SearchIssuesRequest()
            {
                Is = new[] { IssueIsQualifier.Issue },
                Milestone = Milestone,
                Repos = repos,
            };
            var results = await github.SearchIssuesLogged(query, logger);

            // Create area breakdowns
            var areas = new Dictionary<string, AreaBurndown>();
            foreach(var issue in results)
            {
                foreach(var label in issue.Labels)
                {
                    if(label.Name.StartsWith("area-"))
                    {
                        if(!areas.TryGetValue(label.Name, out var areaBurndown))
                        {
                            areaBurndown = new AreaBurndown(label.Name);
                            areas[label.Name] = areaBurndown;
                        }

                        if(issue.State == ItemState.Open)
                        {
                            areaBurndown.Open += 1;
                        }
                        else if(issue.Labels.Any(l => l.Name.Equals("accepted")))
                        {
                            Debug.Assert(issue.State == ItemState.Closed);
                            areaBurndown.Accepted += 1;
                        }
                        else
                        {
                            Debug.Assert(issue.State == ItemState.Closed);
                            areaBurndown.Closed += 1;
                        }
                    }
                }
            }

            // Add the item to the existing burndown and save
            burndown.Weeks.Add(new WeekBurndown(DateTime.Now, areas.Values.OrderBy(a => a.Label).ToList()));
            logger.LogDebug("Saving burndown...");
            await data.SaveBurndownAsync(Milestone, burndown);
            logger.LogDebug("Saved burndown.");

            return 0;
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Internal.AspNetCore.ReportGenerator
{
    internal static class GitHubClientExtensions
    {
        public static async Task<IReadOnlyList<Issue>> SearchIssuesLogged(this GitHubClient github, SearchIssuesRequest query, ILogger logger)
        {
            var queryText = string.Join(" ", query.MergedQualifiers());
            logger.LogInformation("Executing GitHub query: {Query}", queryText);

            var issues = new List<Issue>();
            while (true)
            {
                var results = await github.Search.SearchIssues(query);
                if(results.Items.Count == 0)
                {
                    break;
                }

                if (results.IncompleteResults)
                {
                    logger.LogWarning("Results are incomplete!");
                }

                issues.AddRange(results.Items);
                query.Page += 1;
            }

            // Get all the issues

            var apiInfo = github.GetLastApiInfo();
            logger.LogInformation("Received {Count} results. Rate Limit Remaining {Remaining} (Resets At: {ResetAt})", issues.Count, apiInfo.RateLimit.Remaining, apiInfo.RateLimit.Reset.LocalDateTime);
            return issues;
        }
    }
}

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Internal.AspNetCore.ReportGenerator
{
    internal static class GitHubClientExtensions
    {
        public static async Task<SearchIssuesResult> SearchIssuesLogged(this GitHubClient github, SearchIssuesRequest query, ILogger logger)
        {
            var queryText = string.Join(" ", query.MergedQualifiers());
            logger.LogInformation("Executing GitHub query: {Query}", queryText);
            var results = await github.Search.SearchIssues(query);
            var apiInfo = github.GetLastApiInfo();
            logger.LogInformation("Received {Count} results. Rate Limit Remaining {Remaining} (Resets At: {ResetAt})", results.TotalCount, apiInfo.RateLimit.Remaining, apiInfo.RateLimit.Reset.LocalDateTime);
            return results;
        }
    }
}

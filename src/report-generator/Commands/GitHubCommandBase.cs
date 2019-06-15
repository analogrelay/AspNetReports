using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using Octokit;
using Octokit.Internal;

namespace Internal.AspNetCore.ReportGenerator.Commands
{
    internal abstract class GitHubCommandBase : CommandBase
    {
        [Option("--github <TOKEN>", Description = "GitHub authentication token.")]
        public string? GitHubToken { get; set; }

        public GitHubClient GetGitHubClient()
        {
            if (string.IsNullOrEmpty(GitHubToken))
            {
                throw new CommandLineException("Missing required argument '--github'.");
            }

            return new GitHubClient(
                new ProductHeaderValue("aspnet-release", typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0"),
                new InMemoryCredentialStore(new Credentials(GitHubToken)));
        }
    }
}

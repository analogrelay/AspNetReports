using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class Organization
    {
        public Organization()
        {
            Name = string.Empty;
            Repositories = Array.Empty<RepositoryRef>();
            InternalGitHubTeams = Array.Empty<string>();
        }

        [JsonConstructor]
        public Organization(string name, IReadOnlyList<string> repositories, IReadOnlyList<string> internalGitHubTeams)
        {
            Name = name;
            InternalGitHubTeams = internalGitHubTeams;

            var repos = new List<RepositoryRef>();
            foreach (var repoRef in repositories)
            {
                repos.Add(RepositoryRef.Parse(repoRef));
            }
            Repositories = repos;
        }

        public string Name { get; }
        public IReadOnlyList<RepositoryRef> Repositories { get; }
        public IReadOnlyList<string> InternalGitHubTeams { get; }
    }
}

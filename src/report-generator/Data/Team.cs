using System;
using System.Collections.Generic;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class Team
    {
        public Team(string name, string gitHubTeam, IReadOnlyList<string> areaLabels)
        {
            Name = name;
            GitHubTeam = gitHubTeam;
            AreaLabels = areaLabels ?? Array.Empty<string>();
        }

        public string Name { get; }
        public string GitHubTeam { get; }
        public IReadOnlyList<string> AreaLabels { get; }
    }
}

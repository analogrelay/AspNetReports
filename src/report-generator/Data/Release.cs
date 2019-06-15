using System;
using System.Collections.Generic;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class Release
    {
        public Release(string name, IReadOnlyList<ReleaseMilestone> milestones)
        {
            Name = name;
            Milestones = milestones ?? Array.Empty<ReleaseMilestone>();
        }

        public string Name { get; }
        public IReadOnlyList<ReleaseMilestone> Milestones { get; }
    }
}

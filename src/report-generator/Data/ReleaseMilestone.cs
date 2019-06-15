using System;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class ReleaseMilestone
    {
        public ReleaseMilestone(string name, DateTime? branchOpens, DateTime? branchCloses)
        {
            Name = name;
            BranchCloses = branchCloses;
            BranchOpens = branchOpens;
        }

        public string Name { get; }
        public DateTime? BranchCloses { get; }
        public DateTime? BranchOpens { get; }
    }
}
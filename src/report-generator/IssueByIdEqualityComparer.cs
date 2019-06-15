using System.Collections.Generic;
using Octokit;

namespace Internal.AspNetCore.ReportGenerator
{
    internal class IssueByIdEqualityComparer : IEqualityComparer<Issue>
    {
        public static readonly IEqualityComparer<Issue> Instance = new IssueByIdEqualityComparer();

        private IssueByIdEqualityComparer()
        {
        }

        public bool Equals(Issue x, Issue y) => x.Id == y.Id;

        public int GetHashCode(Issue obj) => obj.Id;
    }
}
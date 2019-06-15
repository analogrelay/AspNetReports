using System;
using System.Collections.Generic;
using System.Text;

namespace Internal.AspNetCore.ReportGenerator.Reports
{
    public class StatusReportModel
    {
        public string? Name { get; set; }
        public string? ReportDate { get; set; }
        public IList<MilestoneDates>? Milestone { get; set; }
        public IList<ContributorModel>? ExternalContributors { get; set; }
        public IList<ContributorModel>? InternalContributors { get; set; }
        public IList<AreaSummaryModel>? Areas { get; set; }
    }

    public class AreaSummaryModel
    {
        public string? Label { get; set; }
        public int Open { get; set; }
        public int Closed { get; set; }
        public int Incomplete => Open + Closed;
        public int Accepted { get; set; }
    }

    public class ContributorModel
    {
        public string? AvatarUrl { get; set; }
        public string? DisplayName { get; set; }
        public string? ProfileUrl { get; set; }
        public IList<PullRequestSummaryModel>? PullRequests { get; set; }
    }

    public class PullRequestSummaryModel
    {
        public string? RepoOwner { get; set; }
        public string? RepoName { get; set; }
        public int? Number { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
    }

    public class MilestoneDates
    {
        public string? Name { get; set; }
        public string? BranchCloses { get; set; }
        public int? WorkDaysRemaining { get; set; }
    }
}

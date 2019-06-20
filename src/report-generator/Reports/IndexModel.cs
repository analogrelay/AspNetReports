using System.Collections.Generic;

namespace Internal.AspNetCore.ReportGenerator.Reports
{
    public class IndexModel
    {
        public IList<StatusReportTeam>? StatusReports { get; set; }
    }

    public class StatusReportTeam
    {
        public string? TeamName { get; set; }
        public IList<ReportModel>? Reports { get; set; }
    }

    public class ReportModel
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
    }
}

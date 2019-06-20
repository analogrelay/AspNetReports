using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;

namespace Internal.AspNetCore.ReportGenerator.Reports
{
    public class Index
    {
        public static async Task<IndexModel> GenerateReportModelAsync(ReportStore reports, DataStore data)
        {
            var statusReports = new List<StatusReportTeam>();
            foreach (var team in data.Teams.Values)
            {
                var teamStatusRoot = $"status/{team.Name}";
                var reportNames = await reports.LoadReportNamesAsync(teamStatusRoot);
                var statusReportTeam = new StatusReportTeam()
                {
                    TeamName = team.Name,
                    Reports = new List<ReportModel>(),
                };

                foreach (var report in reportNames.OrderByDescending(r => r))
                {
                    var reportName = Path.GetFileName(report);
                    if (DateTime.TryParseExact(reportName, "yyyy-MM-dd", CultureInfo.CurrentCulture, DateTimeStyles.None, out _))
                    {
                        statusReportTeam.Reports.Add(new ReportModel()
                        {
                            Name = reportName,
                            Url = $"/reports/status/{team.Name}/{reportName}",
                        });
                    }
                }

                if (statusReportTeam.Reports.Count > 0)
                {
                    statusReports.Add(statusReportTeam);
                }
            }

            return new IndexModel()
            {
                StatusReports = statusReports
            };
        }
    }
}

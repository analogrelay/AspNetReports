using System;
using System.IO;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Reports;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Internal.AspNetCore.ReportGenerator.Commands
{
    [Command("index", Description = "Generates the Index page.")]
    internal class IndexCommand : CommandBase
    {
        [Option("--reports-dir <PATH>", Description = "The path in which reports are stored. Defaults to '<current directory>/docs/reports'.")]
        public string? ReportsDirectory { get; set; }

        [Option("--templates-dir <PATH>", Description = "The path in which report templates are stored. Defaults to '<current directory>/templates'.")]
        public string? TemplatesDirectory { get; set; }

        [Option("--output <INDEX_PAGE_PATH>", Description = "The path to which the index page is written. Defaults to '<current directory>/docs/index.html'.")]
        public string? OutputFile { get; set; }

        [Option("--regenerate", Description = "Re-generates the report from an existing model.")]
        public bool RegenerateFromModel { get; set; }

        public async Task<int> OnExecuteAsync()
        {
            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger<StatusCommand>();

            if(string.IsNullOrEmpty(OutputFile))
            {
                OutputFile = Path.Combine(Directory.GetCurrentDirectory(), "docs", "index.html");
            }

            logger.LogInformation("Preparing index page report");

            var data = await LoadDataStoreAsync();
            var reports = ReportStore.Create(ReportsDirectory, TemplatesDirectory);

            logger.LogInformation("Computing new report model.");
            var model = await Reports.Index.GenerateReportModelAsync(reports, data);

            // Save the report
            var report = await reports.RenderReportAsync("HomePage.html", model);

            await File.WriteAllTextAsync(OutputFile, report);

            return 0;
        }
    }
}

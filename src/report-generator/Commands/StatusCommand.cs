﻿using System;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Reports;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Internal.AspNetCore.ReportGenerator.Commands
{
    [Command("status", Description = "Generates the KCore Status Report.")]
    internal class StatusCommand : GitHubCommandBase
    {
        [Option("--team <TEAM>", Description = "The team to generate the report for.")]
        public string? Team { get; set; }

        [Option("--milestone <MILESTONE>", Description = "The milestone to generate the report for.")]
        public string? Milestone { get; set; }

        [Option("--start-date <START_DATE>", Description = "The start date for the PR query. Defaults to midnight local time, 7 days prior to the start date.")]
        public DateTime? StartDate { get; set; }

        [Option("--end-date <END_DATE>", Description = "The end date for the PR query. Defaults to midnight local time, today.")]
        public DateTime? EndDate { get; set; }

        [Option("--name <NAME>", Description = "The name for the report. Defaults to 'status/{Team}/{yyyy-MM-dd}'. Can contain '/' to serve as path separators.")]
        public string? ReportName { get; set; }

        [Option("--reports-dir <PATH>", Description = "The path in which reports are stored. Defaults to '<current directory>/reports'.")]
        public string? ReportsDirectory { get; set; }

        [Option("--templates-dir <PATH>", Description = "The path in which report templates are stored. Defaults to '<current directory>/templates'.")]
        public string? TemplatesDirectory { get; set; }

        public async Task<int> OnExecuteAsync()
        {
            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger<StatusCommand>();

            if (string.IsNullOrEmpty(Team))
            {
                throw new CommandLineException("Missing required option '--team'.");
            }

            if (string.IsNullOrEmpty(Milestone))
            {
                throw new CommandLineException("Missing required option '--milestone'.");
            }

            if (string.IsNullOrEmpty(ReportName))
            {
                ReportName = $"status/{Team}/{DateTime.Now:yyyy-MM-dd}";
            }

            logger.LogInformation("Preparing report: {ReportName}", ReportName);

            var data = await LoadDataStoreAsync();
            var github = GetGitHubClient();
            var reports = ReportStore.Create(ReportsDirectory, TemplatesDirectory);

            // Load up the team
            if (!data.Teams.TryGetValue(Team, out var team))
            {
                throw new CommandLineException($"Unknown team '{Team}'.");
            }

            var model = await StatusReport.GenerateReportModelAsync(github, data, team, Milestone, StartDate, EndDate, loggerFactory);

            // Save the report and model
            await reports.SaveReportAsync(ReportName, "index.html", "StatusReport.html", model);

            return 0;
        }
    }
}

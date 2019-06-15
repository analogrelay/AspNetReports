using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using Newtonsoft.Json;
using Stubble.Core.Builders;

namespace Internal.AspNetCore.ReportGenerator.Reports
{
    public class ReportStore
    {
        private readonly string _reportsDirectory;
        private readonly string _templatesDirectory;

        public ReportStore(string reportsDirectory, string templatesDirectory)
        {
            _reportsDirectory = reportsDirectory;
            _templatesDirectory = templatesDirectory;
        }

        public async Task SaveReportModelAsync(string name, object model)
        {
            var reportDir = Path.Combine(_reportsDirectory, name);
            var modelPath = Path.Combine(reportDir, "model.json");

            if(!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }

            var json = JsonConvert.SerializeObject(model, DataStore.JsonSerializerSettings);
            await File.WriteAllTextAsync(modelPath, json);
        }

        public async Task SaveReportAsync(string name, string templateName, object model)
        {
            var reportDir = Path.Combine(_reportsDirectory, name);
            var reportPath = Path.Combine(reportDir, "Report.md");
            var templatePath = Path.Combine(_templatesDirectory, $"{templateName}.md");

            if(!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }

            var builder = new StubbleBuilder().Build();
            var report = builder.Render(await File.ReadAllTextAsync(templatePath), model);
            await File.WriteAllTextAsync(reportPath, report);
        }

        internal static ReportStore Create(string? reportsDirectory, string? templatesDirectory)
        {
            var reports = string.IsNullOrEmpty(reportsDirectory) ?
                Path.Combine(Directory.GetCurrentDirectory(), "reports") :
                reportsDirectory;
            var templates = string.IsNullOrEmpty(templatesDirectory) ?
                Path.Combine(Directory.GetCurrentDirectory(), "templates") :
                templatesDirectory;
            return new ReportStore(reports, templates);
        }
    }
}

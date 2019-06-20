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

        public async Task SaveReportAsync(string name, string outputFileName, string templateFileName, object model)
        {
            var reportDir = Path.Combine(_reportsDirectory, name);
            var reportPath = Path.Combine(reportDir, outputFileName);
            var templatePath = Path.Combine(_templatesDirectory, templateFileName);
            var modelPath = Path.Combine(reportDir, "model.json");

            if(!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }

            // Save the report model.
            var json = JsonConvert.SerializeObject(model, DataStore.JsonSerializerSettings);
            await File.WriteAllTextAsync(modelPath, json);

            // Save the report itself.
            var template = await File.ReadAllTextAsync(templatePath);
            var builder = new StubbleBuilder()
                .Build();
            var report = builder.Render(template, model);
            await File.WriteAllTextAsync(reportPath, report);
        }

        internal static ReportStore Create(string? reportsDirectory, string? templatesDirectory)
        {
            var reports = string.IsNullOrEmpty(reportsDirectory) ?
                Path.Combine(Directory.GetCurrentDirectory(), "docs", "reports") :
                reportsDirectory;
            var templates = string.IsNullOrEmpty(templatesDirectory) ?
                Path.Combine(Directory.GetCurrentDirectory(), "templates") :
                templatesDirectory;
            return new ReportStore(reports, templates);
        }
    }
}

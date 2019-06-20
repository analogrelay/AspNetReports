using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using Newtonsoft.Json;
using Stubble.Core.Builders;
using Stubble.Core.Interfaces;

namespace Internal.AspNetCore.ReportGenerator.Reports
{
    public class ReportStore
    {
        private readonly string _templatesDirectory;
        private readonly StubbleBuilder _stubbleBuilder;

        public string ReportsDirectory { get; }

        public ReportStore(string reportsDirectory, string templatesDirectory)
        {
            ReportsDirectory = reportsDirectory;
            _templatesDirectory = templatesDirectory;

            _stubbleBuilder = new StubbleBuilder()
                .Configure(settings =>
                {
                    settings.AddToPartialTemplateLoader(new PartialTemplateLoader(Path.Combine(templatesDirectory, "partials")));
                });
        }
        
        public Task<IReadOnlyList<string>> LoadReportNamesAsync(string basePath)
        {
            var reportDir = Path.Combine(ReportsDirectory, basePath.Replace('/', Path.DirectorySeparatorChar));
            return Task.FromResult<IReadOnlyList<string>>(Directory.GetDirectories(reportDir));
        }

        public async Task<T> LoadReportModelAsync<T>(string reportName)
        {
            var reportDir = Path.Combine(ReportsDirectory, reportName.Replace('/', Path.DirectorySeparatorChar));
            var modelPath = Path.Combine(reportDir, "model.json");
            var json = await File.ReadAllTextAsync(modelPath);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task SaveReportAsync(string name, string outputFileName, string templateFileName, object model)
        {
            var reportDir = Path.Combine(ReportsDirectory, name.Replace('/', Path.DirectorySeparatorChar));
            var reportPath = Path.Combine(reportDir, outputFileName);
            var modelPath = Path.Combine(reportDir, "model.json");

            if(!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }

            // Save the report model.
            var json = JsonConvert.SerializeObject(model, DataStore.JsonSerializerSettings);
            await File.WriteAllTextAsync(modelPath, json);

            // Save the report itself.
            var report = await RenderReportAsync(templateFileName, model);
            await File.WriteAllTextAsync(reportPath, report);
        }

        public async Task<string> RenderReportAsync(string templateFileName, object model)
        {
            var templatePath = Path.Combine(_templatesDirectory, templateFileName);
            var template = await File.ReadAllTextAsync(templatePath);
            var renderer = _stubbleBuilder.Build();
            return renderer.Render(template, model);
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

        private class PartialTemplateLoader : IStubbleLoader
        {
            public PartialTemplateLoader(string rootDirectory)
            {
                RootDirectory = rootDirectory;
            }

            public string RootDirectory { get; }

            public IStubbleLoader Clone() => new PartialTemplateLoader(RootDirectory);
            public string Load(string name) => File.ReadAllText(ResolveTemplate(name));
            public ValueTask<string> LoadAsync(string name) => new ValueTask<string>(File.ReadAllTextAsync(ResolveTemplate(name)));
            private string ResolveTemplate(string name) => Path.Combine(RootDirectory, $"{name}.html");
        }
    }
}

using System.IO;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Internal.AspNetCore.ReportGenerator.Commands
{
    internal abstract class CommandBase
    {
        [Option("--data <DATA_PATH>", Description = "Data Store path. Defaults to the '<current directory>/data'.")]
        public string? DataStorePath { get; set; }

        [Option("-v|--verbose", Description = "Be verbose.")]
        public bool Verbose { get; set; }

        public Task<DataStore> LoadDataStoreAsync() => DataStore.LoadAsync(string.IsNullOrEmpty(DataStorePath) ? GetDefaultDataStorePath() : DataStorePath);

        private string GetDefaultDataStorePath() => Path.Combine(Directory.GetCurrentDirectory(), "data");

        protected ILoggerFactory CreateLoggerFactory()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.AddConsole();

                if(!Verbose)
                {
                    logging.AddFilter(level => level >= LogLevel.Information);
                }
                else
                {
                    logging.AddFilter(_ => true);
                }
            });
            return services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
        }
    }
}

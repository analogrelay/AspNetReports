using System.IO;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using McMaster.Extensions.CommandLineUtils;

namespace Internal.AspNetCore.ReportGenerator.Commands
{
    internal abstract class CommandBase
    {
        [Option("--data <DATA_PATH>", Description = "Data Store path. Defaults to the '<current directory>/data'.")]
        public string? DataStorePath { get; set; }

        public Task<DataStore> LoadDataStoreAsync() => DataStore.LoadAsync(string.IsNullOrEmpty(DataStorePath) ? GetDefaultDataStorePath() : DataStorePath);

        private string GetDefaultDataStorePath() => Path.Combine(Directory.GetCurrentDirectory(), "data");
    }
}

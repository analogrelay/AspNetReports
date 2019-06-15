using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Internal.AspNetCore.ReportGenerator.Commands
{
    [Command("dump-data", Description = "Dumps data read from the data store.")]
    internal class DumpDataCommand : CommandBase
    {
        public async Task<int> OnExecuteAsync(IConsole console)
        {
            var dataStore = await LoadDataStoreAsync();

            console.WriteLine($"Organization '{dataStore.Organization.Name}'");
            console.WriteLine("");

            console.WriteLine("Repositories:");
            foreach(var repo in dataStore.Organization.Repositories)
            {
                console.WriteLine($"* {repo.Owner}/{repo.Name}");
            }

            console.WriteLine("Internal Teams:");
            foreach(var team in dataStore.Organization.InternalGitHubTeams)
            {
                console.WriteLine($"* {team}");
            }

            console.WriteLine("Upcoming Holidays:");
            foreach (var holiday in dataStore.Holidays)
            {
                console.WriteLine($"* {holiday.Date:yyyy-MM-dd} - {holiday.Name} (Location: {holiday.Location})");
            }

            console.WriteLine("Issue Sets:");
            foreach (var pair in dataStore.Teams)
            {
                console.WriteLine($"* {pair.Value.Name} ({string.Join(", ", pair.Value.AreaLabels)})");
            }

            console.WriteLine("Releases:");
            foreach (var pair in dataStore.Releases)
            {
                console.WriteLine($"* {pair.Key}");
                foreach (var milestone in pair.Value.Milestones)
                {
                    console.WriteLine($"  * {milestone.Name} (Branch Closes: {milestone.BranchCloses?.ToString("yyyy-MM-dd") ?? "<unknown>"})");
                }
            }


            return 0;
        }
    }
}

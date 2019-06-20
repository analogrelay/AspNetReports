using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class DataStore
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private readonly string _directory;

        public DataStore(string directory, IReadOnlyList<Holiday> holidays, IReadOnlyDictionary<string, Team> teams, IReadOnlyDictionary<string, Release> releases, Organization organization)
        {
            _directory = directory;
            Holidays = holidays;
            Teams = teams;
            Releases = releases;
            Organization = organization;
        }

        public IReadOnlyList<Holiday> Holidays { get; }

        public IReadOnlyDictionary<string, Team> Teams { get; }
        public IReadOnlyDictionary<string, Release> Releases { get; }
        public Organization Organization { get; }

        public ReleaseMilestone GetMilestone(string milestone)
        {
            return Releases.Values.SelectMany(r => r.Milestones).FirstOrDefault(m => string.Equals(m.Name, milestone, StringComparison.OrdinalIgnoreCase));
        }

        public Task<Burndown> LoadBurndownAsync(string milestone)
        {
            var path = Path.Combine(_directory, "burndown");
            return LoadObjectAsync<Burndown>(path, $"{milestone}.json", () => new Burndown(milestone, new List<WeekBurndown>()));
        }

        public Task SaveBurndownAsync(string milestone, Burndown burndown)
        {
            var path = Path.Combine(_directory, "burndown");
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var file = Path.Combine(path, $"{milestone}.json");
            var json = JsonConvert.SerializeObject(burndown, JsonSerializerSettings);
            return File.WriteAllTextAsync(file, json);
        }

        public static async Task<DataStore> LoadAsync(string directory)
        {
            var holidays = await LoadListAsync<Holiday>(directory, "holidays.json");
            var teams = await LoadListAsync<Team>(directory, "teams.json");
            var releases = await LoadListAsync<Release>(directory, "releases.json");
            var org = await LoadObjectAsync<Organization>(directory, "org.json", () => new Organization());

            return new DataStore(directory, holidays, teams.ToDictionary(i => i.Name), releases.ToDictionary(r => r.Name), org);
        }

        public bool IsHoliday(DateTime current, WorkLocation location) => Holidays.Any(h => h.Location == location && h.Date.Date == current.Date);

        private static async Task<T> LoadObjectAsync<T>(string directory, string fileName, Func<T> defaultCreator)
        {
            var path = Path.Combine(directory, fileName);
            if (!File.Exists(path))
            {
                return defaultCreator();
            }

            var content = await File.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<T>(content, JsonSerializerSettings);
        }

        private static async Task<IReadOnlyList<T>> LoadListAsync<T>(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            if (!File.Exists(path))
            {
                return Array.Empty<T>();
            }

            var content = await File.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<IReadOnlyList<T>>(content, JsonSerializerSettings);
        }
    }
}

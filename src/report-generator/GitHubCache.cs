using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using Octokit;

namespace Internal.AspNetCore.ReportGenerator
{
    /// <summary>
    /// **Single-threaded** cache for GitHub data.
    /// </summary>
    internal class GitHubCache
    {
        private readonly GitHubClient _github;
        private readonly DataStore _data;

        private readonly Dictionary<string, User> _userCache = new Dictionary<string, User>();
        private readonly Dictionary<(string owner, string name), Repository> _repoCache = new Dictionary<(string, string), Repository>(TupleComparer.Create(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase));
        private Dictionary<string, List<string>>? _teamMembersCache = null;
        private HashSet<string>? _internalUsers = null;

        public GitHubCache(GitHubClient github, DataStore data)
        {
            _github = github;
            _data = data;
        }

        public Task<User> GetUserAsync(string login) => GetAsync(login, _userCache, (l) => _github.User.Get(l));

        public async Task<ISet<string>> GetInternalUsersAsync()
        {
            if (_internalUsers == null)
            {
                _internalUsers = new HashSet<string>();
                foreach(var team in _data.Organization.InternalGitHubTeams)
                {
                    _internalUsers.UnionWith(await GetTeamMembersAsync(team));
                }
            }

            return _internalUsers;
        }

        public async Task<IReadOnlyList<string>> GetTeamMembersAsync(string teamName)
        {
            if(_teamMembersCache == null)
            {
                var ghTeams = await _github.Organization.Team.GetAll(_data.Organization.Name);
                _teamMembersCache = new Dictionary<string, List<string>>();
                foreach(var ghTeam in ghTeams)
                {
                    var teamMembers = await _github.Organization.Team.GetAllMembers(ghTeam.Id);
                    _teamMembersCache.Add(ghTeam.Name, teamMembers.Select(u => u.Login).ToList());
                }
            }

            return _teamMembersCache[teamName];
        }

        public Task<Repository> GetRepoAsync(string owner, string name) => GetAsync((owner, name), _repoCache, (t) => _github.Repository.Get(t.owner, t.name));

        private async Task<TValue> GetAsync<TKey, TValue>(TKey key, Dictionary<TKey, TValue> cache, Func<TKey, Task<TValue>> loader)
        {
            if (cache.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                value = await loader(key);
                cache[key] = value;
                return value;
            }
        }
    }
}

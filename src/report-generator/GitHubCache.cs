using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Internal.AspNetCore.ReportGenerator.Data;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;
        private readonly Dictionary<string, User> _userCache = new Dictionary<string, User>();
        private readonly Dictionary<(string owner, string name), Repository> _repoCache = new Dictionary<(string, string), Repository>(TupleComparer.Create(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase));
        private Dictionary<string, List<string>> _teamMembersCache = new Dictionary<string, List<string>>();
        private Dictionary<string, int>? _teamIdsCache = null;
        private HashSet<string>? _internalUsers = null;

        public GitHubCache(GitHubClient github, DataStore data, ILogger logger)
        {
            _github = github;
            _data = data;
            _logger = logger;
        }

        public async Task<ISet<string>> GetInternalUsersAsync()
        {
            if (_internalUsers == null)
            {
                _logger.LogDebug("Loading internal users list...");
                _internalUsers = new HashSet<string>();
                foreach (var team in _data.Organization.InternalGitHubTeams)
                {
                    _internalUsers.UnionWith(await GetTeamMembersAsync(team));
                }
                _logger.LogDebug("Loaded {Count} internal users", _internalUsers.Count);
            }

            return _internalUsers;
        }

        public async Task<IReadOnlyList<string>> GetTeamMembersAsync(string teamName)
        {
            if (!_teamMembersCache.TryGetValue(teamName, out var members))
            {
                var id = await GetTeamIdAsync(teamName);
                _logger.LogDebug("Loading members for {TeamName}...", teamName);
                var teamMembers = await _github.Organization.Team.GetAllMembers(id);
                members = teamMembers.Select(u => u.Login).ToList();
                _teamMembersCache.Add(teamName, members);
                _logger.LogDebug("Loaded {Count} members for {TeamName}...", teamMembers.Count, teamName);
            }

            return members;
        }

        public Task<User> GetUserAsync(string login) => GetAsync("user", login, login, _userCache, (l) => _github.User.Get(l));

        public Task<Repository> GetRepoAsync(string owner, string name) => GetAsync("repository", $"{owner}/{name}", (owner, name), _repoCache, (t) => _github.Repository.Get(t.owner, t.name));

        private async Task<TValue> GetAsync<TKey, TValue>(string objectType, string objectName, TKey key, Dictionary<TKey, TValue> cache, Func<TKey, Task<TValue>> loader)
        {
            if (cache.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Cache hit for {ObjectType} {ObjectName}.", objectType, objectName);
                return value;
            }
            else
            {
                _logger.LogDebug("Cache miss for {ObjectType} {ObjectName}. Loading from GitHub...", objectType, objectName);
                value = await loader(key);
                _logger.LogDebug("Loaded {ObjectType} {ObjectName} from GitHub", objectType, objectName);
                cache[key] = value;
                return value;
            }
        }

        private async Task<int> GetTeamIdAsync(string name)
        {
            if (_teamIdsCache == null)
            {
                _logger.LogDebug("Loading team names...");
                var ghTeams = await _github.Organization.Team.GetAll(_data.Organization.Name);
                _teamIdsCache = new Dictionary<string, int>();
                foreach (var ghTeam in ghTeams)
                {
                    _teamIdsCache[ghTeam.Name] = ghTeam.Id;
                }
            }

            return _teamIdsCache[name];
        }
    }
}

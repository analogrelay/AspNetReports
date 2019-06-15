using System;
using System.Text.RegularExpressions;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class RepositoryRef
    {
        private static readonly Regex _parser = new Regex("^(?<owner>.*)/(?<name>.*)$");

        public RepositoryRef(string owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        public string Owner { get; }
        public string Name { get; }

        internal static RepositoryRef Parse(string repoRef)
        {
            var match = _parser.Match(repoRef);
            if (!match.Success)
            {
                throw new FormatException($"Invalid repository reference '{repoRef}', expected a string in the format 'owner/name'.");
            }
            return new RepositoryRef(match.Groups["owner"].Value, match.Groups["name"].Value);
        }
    }
}
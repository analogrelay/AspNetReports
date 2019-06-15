using System;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class Holiday
    {
        public Holiday(string name, WorkLocation location, DateTime date)
        {
            Name = name;
            Location = location;
            Date = date;
        }

        public string Name { get; }
        public WorkLocation Location { get; }
        public DateTime Date { get; }
    }
}
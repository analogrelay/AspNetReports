using System;
using System.Collections.Generic;
using System.Text;

namespace Internal.AspNetCore.ReportGenerator.Data
{
    public class Burndown
    {
        public Burndown(string milestone, IList<WeekBurndown> weeks)
        {
            Milestone = milestone;
            Weeks = weeks;
        }

        public string Milestone { get; }
        public IList<WeekBurndown> Weeks { get; }
    }

    public class WeekBurndown
    {
        public WeekBurndown(DateTime endDate, IList<AreaBurndown> areas)
        {
            EndDate = endDate;
            Areas = areas;
        }

        public DateTime EndDate { get; }
        public IList<AreaBurndown> Areas { get; }
    }

    public class AreaBurndown
    {
        public AreaBurndown(string label)
        {
            Label = label;
        }

        public string Label { get; }
        public int Open { get; set; }
        public int Closed { get; set; }
        public int Accepted { get; set; }
    }
}

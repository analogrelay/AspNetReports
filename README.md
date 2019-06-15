# ASP.NET Core Reports

**NOTE:** There are dates associated with specific previews here. They **do not** represent any committed ship date. They represent the current plan for engineering deadlines (i.e. when the master branch closes to unmoderated check-ins, etc.).

## Infrastructure

The `report-generator` console app is used to generate reports.

## Data Sources

Data is sourced from GitHub and the `data/` folder of this repository.

* `holidays.json` - A list of known holidays for ASP.NET Core team members
* `org.json` - Basic organizational data
* `releases.json` - Releases and milestones
* `teams.json` - "Team" definitions (what areas do they own, who is represented in them, etc.)

## Reports

All historical reports are stored in the `reports/` folder. Models are stored along with the report allowing them to be used for "differential" reporting (Issue Burndown, and other metrics that are compared with a previous report) and to allow regenerating a report.

## Templates

Templates use the [Mustache](https://mustache.github.io/) templating language.
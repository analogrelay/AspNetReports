# {{Name}} Status Report ({{ReportDate}})

## Overview

To be supplied

## Important Dates

| Milestone | Branch Closes | Work Days Remaining |
| - | - | - |
{{#Milestone}}
| {{Name}} | {{BranchCloses}} | {{WorkDaysRemaining}}
{{/Milestone}}

## PRs Merged

### External Contributors

Thanks to all our external contributors!

{{#ExternalContributors}}
* [<img src="{{AvatarUrl}}" width="20" height="20" /> {{DisplayName}}]({{ProfileUrl}})
{{#PullRequests}}
    * [**{{RepoOwner}}/{{RepoName}}#{{Number}}** {{Title}}]({{Url}})
{{/PullRequests}}
{{/ExternalContributors}}

### Internal Contributors

{{#InternalContributors}}
* [<img src="{{AvatarUrl}}" width="20" height="20" /> {{DisplayName}}]({{ProfileUrl}})
{{#PullRequests}}
    * [**{{RepoOwner}}/{{RepoName}}#{{Number}}** {{Title}}]({{Url}})
{{/PullRequests}}
{{/InternalContributors}}

## Active Issues

**Closed** refers to issues that are closed but do not have the `accepted` label applied. **Incomplete** is the total of both **Open** and **Closed**.

| Area | Open | Closed | Incomplete | Accepted |
| - | - | - | - | - |
{{#Areas}}
| `{{Label}}` | {{Open}} | {{Closed}} | {{Incomplete}} | {{Accepted}} |
{{/Areas}}
| **Total** | {{TotalOpen}} | {{TotalClosed}} | {{TotalIncomplete}} | {{TotalAccepted}} |

## Burndown

TBD
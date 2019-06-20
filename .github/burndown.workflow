workflow "Update burndown" {
  resolves = ["stefanzweifel/git-auto-commit-action@v1.0.0"]
  on = "repository_dispatch"
}

action "./actions/report-generator" {
  uses = "./actions/report-generator"
  args = "burndown --milestone \"3.0.0-preview7\" --github \"$GITHUB_TOKEN\""
  secrets = ["GITHUB_TOKEN"]
}

action "stefanzweifel/git-auto-commit-action@v1.0.0" {
  uses = "stefanzweifel/git-auto-commit-action@v1.0.0"
  needs = ["./actions/report-generator"]
  env = {
    COMMIT_MESSAGE = "Update burndown data"
  }
}

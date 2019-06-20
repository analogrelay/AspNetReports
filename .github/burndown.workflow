workflow "Update burndown" {
  on = "schedule(0 7 * * 5)"
  resolves = ["docker://debian"]
}

action "docker://debian" {
  uses = "docker://debian"
  runs = "git"
  args = "status"
}

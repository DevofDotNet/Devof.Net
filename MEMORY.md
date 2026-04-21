# MEMORY.md

## Cron Jobs

- **daily-dotnet-blog**: Scheduled 12 PM IST daily. Publishes to Devof.net. Currently healthy, last ran April 21 06:30 UTC, next run April 22 06:30 UTC.

- **DevofDotNet-bug-fixer**: Scheduled hourly. Last ran April 21 02:10 UTC (status: ok): Fixed issue #80 (added null/empty checks in GetBySlugAsync methods for Post, Tag, Author, Username repositories), PR #84 created and auto-merged, build passed (32s). Job is now healthy and running successfully.

## Issues Resolved

- Morning Briefing cron job was removed (delivery issues, StreamChat channel problems)
- daily-dotnet-blog had timeout issue (120s limit) but recovered automatically
- **Issue #80**: Fixed null/empty checks in GetBy* methods (PR #84, merged April 21 05:07 UTC)
- **Issue #89**: Added null check for created comment in CommentService.CreateAsync (PR #89, created April 21 05:41 UTC)
- **Issue #90**: Fixed null check for created comment in CommentService.CreateAsync (PRs #90/#91, created April 21 06:08-06:09 UTC)
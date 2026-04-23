# MEMORY.md

## Cron Jobs

- **daily-dotnet-blog**: Scheduled 12 PM IST daily. Publishes to Devof.net. Currently healthy, last ran April 21 06:30 UTC, next run April 22 06:30 UTC.

- **DevofDotNet-bug-fixer**: Scheduled hourly. Issues #80 through #108 fixed (via cron runs and subagent runs). Last successful scheduled run April 22 around 00:00 UTC (GenerateSlug fix).

## Issues Resolved

- Morning Briefing cron job was removed (delivery issues, StreamChat channel problems)
- daily-dotnet-blog had timeout issue (120s limit) but recovered automatically
- **Issue #80**: Fixed null/empty checks in GetBy* methods (PR #84, merged April 21 05:07 UTC)
- **Issue #89**: Added null check for created comment in CommentService.CreateAsync (PR #89, created April 21 05:41 UTC)
- **Issue #90**: Fixed null check for created comment in CommentService.CreateAsync (PRs #90/#91, created April 21 06:08-06:09 UTC)
- **Issue #92**: Removed duplicate admin from SeedUsersAsync list (PR #92)
- **Issue #95**: Added EmailOptions validation
- **Issue #98**: Fixed newsletter confirmation broken link - NewsletterModel was sending /NewsletterConfirm link but page didn't exist. Created NewsletterConfirm.cshtml.cs and NewsletterConfirm.cshtml. Fixed Newsletter.cshtml.cs to persist Subscriber record before emailing. (PR #98)
- **Issue #99**: Added [Required], [StringLength], [MinLength] validation to Post entity (PR #100)
- **Issue #101**: Fixed newsletter confirmation - missing confirmation page (deployed April 21 19:15 UTC)
- **Issue #103**: Fixed CommentDto.Author nullable, added null guards in Razor pages
- **Issue #104/#105**: Fixed NullReferenceException in sitemap when Author is null (added filter for null authors)
- **Issue #106**: Fixed RSS feed HTTPS URLs (Feed.cshtml.cs only handled http:// URLs, added check for https and absolute paths)
- **Issue #107**: Fixed pagination - use correct TotalCount for tag/search results
- **Issue #108**: Fixed GenerateSlug bug - could leave trailing dash after truncation to 200 chars (trim dashes both before AND after truncation)
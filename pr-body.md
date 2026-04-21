Fixes Issue #85

## Summary
The Post.Author navigation property was marked with null-forgiving operator (= null!) but could genuinely be null when accessed before the ORM lazy loads it, causing NullReferenceException at runtime.

## Changes Made
- Changed Post.Author from ApplicationUser! to ApplicationUser? (nullable)
- Added null checks in EngagementService.cs, PostService.cs, Sitemap.cshtml.cs, Admin/Posts.cshtml.cs, and Post/Edit.cshtml.cs
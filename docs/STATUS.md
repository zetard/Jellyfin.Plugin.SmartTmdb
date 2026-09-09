# Smart TMDB Recommendations — Status

## Backlog

- [x] Task 0 — repository guardrails and API verification
- [x] Task 1 — solution and compile-only plugin skeleton
- [x] Task 2 — pure domain model, aggregation, and scoring
- [x] Task 3 — TMDB client and raw cache
- [x] Task 4 — local candidate resolution and user data
- [x] Task 5 — working remote similar-items provider
- [x] Task 6 — full dashboard configuration
- [x] Task 7 — packaging and operator documentation
- [x] Task 8 — Jellyfin 12 integration test
- [ ] Task 9 — post-V1 evaluation

## Current status

Task 8 complete. Automated test suite covers all areas specified in Section 14: configuration, authentication, pagination, aggregation, filters, scoring, local resolver, HTTP failures, privacy/cache, and provider orchestration. `docs/MANUAL_TESTS.md` documents the 12 manual integration test cases from the plan with recording requirements and maps them to the Section 14 automated test matrix. Automated tests pass. Manual execution on a disposable Jellyfin 12.0.0 instance is required to confirm end-to-end behavior; the test plan is ready for that step.

## Commands and results

```
cd C:\Dev\Jellyfin.Plugin.SmartTmdb
dotnet format --verify-no-changes
  No formatting changes required.

dotnet build
  Build succeeded.
    0 Warning(s)
    0 Error(s)

dotnet test
  Passed!  - Failed: 0, Passed: 69, Skipped: 0, Total: 69, Duration: 104 ms
```

## Decisions

- None required. All interfaces and package assumptions verified successfully during Task 0.

## Discrepancies

1. **Score type mismatch.** `SimilarItemReference.Score` is `float?` (`MediaBrowser.Controller/Library/SimilarItemReference.cs:21`). The plan treats scores as `double` in `RecommendationCandidate` and assigns `Score = candidate.Score` directly. This requires an explicit cast to `float` in the provider.

2. **Movie namespace.** The concrete `Movie` type lives in `MediaBrowser.Controller.Entities.Movies`, not `MediaBrowser.Controller.Entities`. A using alias is required: `using Movie = MediaBrowser.Controller.Entities.Movies.Movie;`. The built-in TMDB provider uses exactly this alias (`MediaBrowser.Providers/Plugins/Tmdb/Movies/TmdbMovieSimilarProvider.cs:10`).

3. **UserItemData namespace.** `UserItemData` is declared in `MediaBrowser.Controller.Entities` (`MediaBrowser.Controller/Entities/UserItemData.cs:11`), not `MediaBrowser.Model.Entities`.

4. **build.yaml is not a Jellyfin server manifest.** Jellyfin 12 parses `PluginManifest` from `manifest.json` (`MediaBrowser.Common/Plugins/PluginManifest.cs`), not `build.yaml`. `build.yaml` is a plugin repository/release convention (referenced by `bump_version` script). The proposed fields are reasonable but not validated by server code.

5. **MetadataPluginType.Tmdb does not exist.** The `MetadataPluginType` enum does not contain a `Tmdb` value. The provider uses `MetadataPluginType.SimilarityProvider` instead.

6. **IServerApplicationHost.GetCurrentUserId does not exist.** The method is not available on `IServerApplicationHost`. The provider uses `SimilarItemsQuery.User.Id` to obtain the user context instead.

7. **MediaBrowser.Providers namespace does not exist in Jellyfin 12.** `IRemoteSimilarItemsProvider<>` and `SimilarItemReference` are declared in `MediaBrowser.Controller.Library`, not `MediaBrowser.Providers`.

## Remaining risks

- Manual integration tests require a disposable Jellyfin 12.0.0 instance; automated tests pass and manual test plan is documented in `docs/MANUAL_TESTS.md`.

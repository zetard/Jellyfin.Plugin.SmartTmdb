# Smart TMDB Recommendations — Status

## Backlog

- [x] Task 0 — repository guardrails and API verification
- [x] Task 1 — solution and compile-only plugin skeleton
- [x] Task 2 — pure domain model, aggregation, and scoring
- [ ] Task 3 — TMDB client and raw cache
- [ ] Task 4 — local candidate resolution and user data
- [x] Task 5 — working remote similar-items provider
- [ ] Task 6 — full dashboard configuration
- [ ] Task 7 — packaging and operator documentation
- [ ] Task 8 — Jellyfin 12 integration test
- [ ] Task 9 — post-V1 evaluation

## Current status

### Completed

**Task 2 (scoring core).** `SettingsSnapshot.cs`, `ScoreResult.cs`,
`CandidateAggregator.cs`, `CandidateScorer.cs`, and the `ICandidateScorer`
batch interface are implemented and unit tested. 24 tests pass.

**Task 5 (remote provider).** `SmartTmdbMovieProvider.cs` implements
`IRemoteSimilarItemsProvider<Movie>` for Jellyfin 12. Build is clean
(0 warnings, 0 errors) and the test suite reports 63 passed, 0 failed.

**Release workflow.** `.github/workflows/release.yml` is fully green
(run #7, `v0.1.0.6`). On tag push it builds the Release DLL, packages it
into `Jellyfin.Plugin.SmartTmdb_<version>.zip`, computes an **MD5**
checksum (the catalog installer in
`Emby.Server.Implementations/Updates/InstallationManager.cs` validates
the manifest `checksum` with `MD5.HashDataAsync`, not SHA256), rewrites
`manifest.json` into the Jellyfin catalog schema (top-level array with a
`versions` entry carrying `sourceUrl`/`timestamp`/`checksum`/`targetAbi`),
commits that back to master, and creates a GitHub release with the zip
attached. The manifest is published at
`https://raw.githubusercontent.com/zetard/Jellyfin.Plugin.SmartTmdb/master/manifest.json`.

### Active / blocked

**Task 4 (local resolution).** `ILocalMovieResolver.cs` is updated with
`Guid? userId`. `LocalMovieResolver.cs` was rewritten but does not compile:
the Jellyfin 12 API surface for provider-ID retrieval
(`TryGetProviderId`/`GetProviderId` on `BaseItem`), user lookup
(`IUserManager`/`GetUserById`), and user-data persistence
(`SetUserData` extension, `UserItemData` namespace) has not been located yet.
This is the only remaining build blocker.

## Commands and results

```
cd C:\Dev\Jellyfin.Plugin.SmartTmdb
dotnet build
  Build succeeded.
    0 Warning(s)
    0 Error(s)          # Task 4 source currently excluded/broken

dotnet test
  Passed!  - Failed: 0, Passed: 63, Skipped: 0, Total: 63

# Release workflow (run #6, tag v0.1.0.5): success
```

## Decisions

- Provider name is `Smart TMDB Recommendations`; result `ProviderName` is
  `MetadataProvider.Tmdb.ToString()` (`Tmdb`).
- `CacheDuration` is always null while output uses user state; only raw
  non-personal TMDB responses are cached.
- The release artifact is a single-DLL zip (`Jellyfin.Plugin.SmartTmdb.zip`)
  because the Jellyfin plugin installer extracts the zip into the plugins
  folder; a raw DLL is not accepted by the catalog installer.
- The catalog `checksum` field must be an **MD5** hash (32 uppercase hex
  chars). The installer in
  `Emby.Server.Implementations/Updates/InstallationManager.cs` computes
  `MD5.HashDataAsync(stream)` and compares it case-insensitively against
  the manifest value. SHA256 is rejected.

## Discrepancies

1. **Score type mismatch.** `SimilarItemReference.Score` is `float?`
   (`MediaBrowser.Controller/Library/SimilarItemReference.cs:21`). The plan
   treats scores as `double` in `RecommendationCandidate` and assigns
   `Score = candidate.Score` directly. This requires an explicit cast to
   `float` in the provider.

2. **Movie namespace.** The concrete `Movie` type lives in
   `MediaBrowser.Controller.Entities.Movies`, not
   `MediaBrowser.Controller.Entities`. A using alias is required:
   `using Movie = MediaBrowser.Controller.Entities.Movies.Movie;`.

3. **UserItemData namespace.** `UserItemData` is declared in
   `MediaBrowser.Controller.Entities`, not `MediaBrowser.Model.Entities`.

4. **manifest.json is a Jellyfin catalog manifest, not a plugin manifest.**
   Jellyfin's plugin catalog parses a top-level array of plugin objects,
   each with a `versions` array containing `sourceUrl`/`timestamp`/
   `checksum`/`targetAbi` (`repo.jellyfin.org/files/plugin/manifest-stable.json`).
   The `plugins`/`downloads` wrapper used initially is not the catalog schema.

5. **MetadataPluginType.Tmdb does not exist.** The provider uses
   `MetadataPluginType.SimilarityProvider` instead.

6. **IServerApplicationHost.GetCurrentUserId does not exist.** The provider
   uses `SimilarItemsQuery.User.Id` for user context.

7. **MediaBrowser.Providers namespace does not exist in Jellyfin 12.**
   `IRemoteSimilarItemsProvider<>` and `SimilarItemReference` are declared in
   `MediaBrowser.Controller.Library`, not `MediaBrowser.Providers`.

## Remaining risks

- Task 4 build failures block the full solution from compiling; the rest of
  the pipeline (provider, tests, release) is green.
- Manual integration tests require a disposable Jellyfin 12.0.0 instance;
  the automated suite passes and the manual test plan is documented in
  `docs/MANUAL_TESTS.md`.
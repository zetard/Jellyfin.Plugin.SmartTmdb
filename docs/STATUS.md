# Smart TMDB Recommendations — Status

## Backlog

- [x] Task 0 — repository guardrails and API verification
- [x] Task 1 — solution and compile-only plugin skeleton
- [x] Task 2 — pure domain model, aggregation, and scoring
- [x] Task 3 — TMDB client and raw cache
- [x] Task 4 — local candidate resolution and user data
- [x] Task 5 — working remote similar-items provider
- [ ] Task 6 — full dashboard configuration
- [ ] Task 7 — packaging and operator documentation
- [ ] Task 8 — Jellyfin 12 integration test
- [ ] Task 9 — post-V1 evaluation

## Current status

### Completed

**Task 2 (scoring core).** `SettingsSnapshot.cs`, `ScoreResult.cs`,
`CandidateAggregator.cs`, `CandidateScorer.cs`, and the `ICandidateScorer`
batch interface are implemented and unit tested.

**Task 3 (TMDB client and raw cache).** `TmdbClient.cs` uses Bearer
authentication, `IHttpClientFactory`, bounded retry/timeout, raw-cache
coalescing, and redacted structured logging. `MemoryRawTmdbCache` provides
bounded in-memory TTL storage. Fixture tests cover valid results, missing
optional fields, empty pages, malformed JSON, 401, 404, 429 with
`Retry-After`, 500, timeout, and cancellation; the token never appears in
URIs, cache keys, or exception messages.

**Task 4 (local resolution).** `LocalMovieResolver.cs` resolves TMDB IDs to
local movies in a single `ILibraryManager.GetItemList` query, attaches batch
`IUserDataManager` data when a user is present, and forwards
`query.ExcludeItemIds` onto the library query. Null user disables watched
personalization; only local movie items are returned to the provider layer.

**Task 5 (remote provider).** `SmartTmdbMovieProvider.cs` implements
`IRemoteSimilarItemsProvider<Movie>` for Jellyfin 12. The candidate pool is
built larger than `query.Limit` before local resolution, results are capped at
`query.Limit ?? 50`, scores are clamped to `[0, 0.95]`, and `CacheDuration`
remains null.

**Release workflow.** `.github/workflows/release.yml` is fully green
(run #9, `v0.1.0.9`). On tag push it builds the Release DLL, packages it
into `Jellyfin.Plugin.SmartTmdb_<version>.zip`, computes an **MD5**
checksum (the catalog installer in
`Emby.Server.Implementations/Updates/InstallationManager.cs` validates
the manifest `checksum` with `MD5.HashDataAsync`, not SHA256), rewrites
`manifest.json` into the Jellyfin catalog schema (top-level array with a
`versions` entry carrying `sourceUrl`/`timestamp`/`checksum`/`targetAbi`),
commits that back to master, and creates a GitHub release with the zip
attached. The manifest is published at
`https://raw.githubusercontent.com/zetard/Jellyfin.Plugin.SmartTmdb/master/manifest.json`.

**Release procedure (do not hand-edit the manifest).** To cut a release:
`dotnet build --configuration Release` locally to verify, then
`git tag -a vX.Y.Z -m "Release vX.Y.Z"` and `git push origin vX.Y.Z`. The
workflow builds the Release DLL, packages the zip, computes the MD5,
**rewrites `manifest.json` itself and commits it back to master**, and
creates the GitHub release with the zip attached. Do not stage or commit
`manifest.json` manually — it is redundant and produces a divergent commit
that `git push origin master` then rejects as a non-fast-forward. Only commit
source/docs changes; the workflow owns the manifest. Verify afterward with
the GitHub Contents API (not `raw.githubusercontent.com`, which serves a
stale CDN cache of the previous blob) that the new `version` entry is present
on `master`.

### Active / blocked

**Configuration save returns HTTP 500.** `POST /Plugins/{id}/Configuration`
fails because `BasePluginOfT.SaveConfiguration` persists the plugin
configuration with `System.Xml.XmlSerializer`, which requires every
persisted type to have a public parameterless constructor. `ScoringWeights`
was a C# `record` with positional parameters — it has none — so any save
that included a populated `CustomWeights` threw
`InvalidOperationException: ScoringWeights cannot be serialized because it
does not have a parameterless constructor`, surfacing as a 500 with a
`text/plain` body. Fixed by converting `ScoringWeights` from a `record` to a
plain class with a public parameterless constructor and get/set properties
(`ScoringWeights.cs`); `SettingsSnapshot` was updated from `with` expressions
to explicit `new ScoringWeights(...)` construction. Added regression tests in
`ConfigurationSerializationTests.cs` that round-trip `ScoringWeights` and
`PluginConfiguration` through `System.Xml.Serialization.XmlSerializer`. The
test project now also references `System.Xml.XmlSerializer` so the gate can
exercise the same serializer the server uses.

## Commands and results

```
cd C:\Dev\Jellyfin.Plugin.SmartTmdb
dotnet build
  Build succeeded.
    0 Warning(s)
    0 Error(s)

dotnet format --verify-no-changes
  exit 0 (no formatting or analyzer changes needed)

dotnet test
  Passed!  - Failed: 0, Passed: 74, Skipped: 0, Total: 74

# Release workflow (run #9, tag v0.1.0.9): success
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
- `TreatWarningsAsErrors` is enabled on both the production and test
  projects; the test project previously silently swallowed xUnit analyzer
  warnings.

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

8. **`InternalItemsQuery` is not nested under `ILibraryManager`.** It lives
    in `MediaBrowser.Controller.Entities`. Its `ExcludeItemIds` is a settable
    `Guid[]`, while `SimilarItemsQuery.ExcludeItemIds` is
    `IReadOnlyList<Guid>`.

9. **`BasePlugin<T>.Configuration` has a protected setter.** Tests that need
    to seed saved config must set it via reflection
    (`GetProperty("Configuration")!.SetValue(plugin, value)`).

10. **`dotnet format` cannot auto-fix xUnit analyzer warnings.** xUnit1031
    (blocking `.Result`) and xUnit1051 (`CancellationToken`) are reported but
    have no associated code fix, so the gate is cleared by fixing them by
    hand rather than by excluding them.

11. **`ScoringWeights` was a `record` with positional parameters.**
    `System.Xml.XmlSerializer` (used by `BasePluginOfT.SaveConfiguration` on
    save) requires a public parameterless constructor, so any save that
    included a populated `CustomWeights` threw an `InvalidOperationException`
    that surfaced as an HTTP 500 on `POST /Plugins/{id}/Configuration`. It is
    now a plain class with a parameterless constructor and get/set
    properties; `SettingsSnapshot` was updated from `with` expressions to
    explicit `new ScoringWeights(...)` construction. The test project now
    references `System.Xml.XmlSerializer` so the gate can exercise the same
    serializer the server uses.

## Remaining risks

- Manual integration tests require a disposable Jellyfin 12.0.0 instance;
  the automated suite passes and the manual test plan is documented in
  `docs/MANUAL_TESTS.md`.
- Dashboard configuration UI (Task 6) still needs end-to-end verification
  that every visible setting maps to documented behavior and that saving
  a blank password field preserves an existing token unless Clear is
  chosen.
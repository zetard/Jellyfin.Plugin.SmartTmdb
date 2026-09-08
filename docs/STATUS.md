# Smart TMDB Recommendations — Status

## Backlog

- [x] Task 0 — repository guardrails and API verification
- [ ] Task 1 — solution and compile-only plugin skeleton
- [ ] Task 2 — pure domain model, aggregation, and scoring
- [ ] Task 3 — TMDB client and raw cache
- [ ] Task 4 — local candidate resolution and user data
- [ ] Task 5 — working remote similar-items provider
- [ ] Task 6 — full dashboard configuration
- [ ] Task 7 — packaging and operator documentation
- [ ] Task 8 — Jellyfin 12 integration test
- [ ] Task 9 — post-V1 evaluation

## Current status

Task 0 in progress. No production code has been written.

## Commands and results

```
cd C:\Dev\Jellyfin.Plugin.SmartTmdb
git rev-parse HEAD
6c073e19ddf604b2369c638716164fdab4c952dc
```

Verification read-only commands run against `C:\Dev\_reference\jellyfin-v12`:
- `grep` for interface declarations: `IRemoteSimilarItemsProvider`, `SimilarItemReference`, `SimilarItemsQuery`, `MetadataPluginType`, `BasePlugin<T>`, `IHasWebPages`, `IPluginServiceRegistrator`, `ILibraryManager`, `InternalItemsQuery`, `IUserDataManager`, `ISimilarItemsManager`, `MetadataProvider`, `BaseItemKind`, `UserItemData`
- `read` of exact source files at paths listed in Discrepancies below
- `dotnet new classlib` + `dotnet add package Jellyfin.Controller --version 12.0.0` to confirm package exists and targets `net10.0`
- `dotnet add package Microsoft.Extensions.Http --version 10.0.11` to confirm availability
- Temporary test project cleaned up after verification

## Decisions

- None required. All interfaces and package assumptions verified successfully.

## Discrepancies

1. **Score type mismatch.** `SimilarItemReference.Score` is `float?` (`MediaBrowser.Controller/Library/SimilarItemReference.cs:21`). The plan treats scores as `double` in `RecommendationCandidate` and assigns `Score = candidate.Score` directly. This requires an explicit cast to `float` in the provider.

2. **Movie namespace.** The concrete `Movie` type lives in `MediaBrowser.Controller.Entities.Movies`, not `MediaBrowser.Controller.Entities`. A using alias is required: `using Movie = MediaBrowser.Controller.Entities.Movies.Movie;`. The built-in TMDB provider uses exactly this alias (`MediaBrowser.Providers/Plugins/Tmdb/Movies/TmdbMovieSimilarProvider.cs:10`).

3. **UserItemData namespace.** `UserItemData` is declared in `MediaBrowser.Controller.Entities` (`MediaBrowser.Controller/Entities/UserItemData.cs:11`), not `MediaBrowser.Model.Entities`.

4. **build.yaml is not a Jellyfin server manifest.** Jellyfin 12 parses `PluginManifest` from `manifest.json` (`MediaBrowser.Common/Plugins/PluginManifest.cs`), not `build.yaml`. `build.yaml` is a plugin repository/release convention (referenced by `bump_version` script). The proposed fields are reasonable but not validated by server code.

## Remaining risks

- The `Score` type discrepancy must be resolved during Task 1 to compile.
- `build.yaml` format should be confirmed against the actual Jellyfin plugin repository requirements before Task 7.
- `JELLYFIN_SMART_TMDB_TOKEN` precedence logic must be verified against actual environment variable behavior during Task 6.

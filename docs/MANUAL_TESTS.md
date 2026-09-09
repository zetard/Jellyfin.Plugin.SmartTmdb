# Manual Tests

These are the manual integration tests for Task 8. They require a disposable Jellyfin 12.0.0 instance and a small movie library with known TMDB IDs.

## Setup

1. Record server version and plugin version.
2. Install the plugin and configure a valid TMDB token.
3. Add a test movie library with known TMDB IDs (e.g., Inception `id: 27205`, The Dark Knight `id: 155`, etc.).

## Automated Test Matrix (Section 14)

The automated unit tests cover the following areas. Run `dotnet test` to verify.

| Area | Covered by |
|---|---|
| Configuration | `SettingsSnapshotTests` — defaults, each preset, Custom normalization, zero/negative/NaN weights, page/timeout/cache clamps, environment-token precedence. |
| Authentication | `TmdbClientTests` — Bearer header present; token absent from URI, logs, exceptions, and cache key. |
| Pagination | `CandidateAggregatorTests` — duplicate across pages. `TmdbClientTests` — page parameters. |
| Aggregation | `CandidateAggregatorTests` — candidate only in recommendations, only in similar, in both, duplicate across pages, source item returned by TMDB. |
| Filters | `CandidateScorerTests` — adult, vote average, vote count, strict era, strict language, watched only. |
| Scoring | `CandidateScorerTests` — all feature boundaries, missing fields, franchise modes, watched preference, popularity ± direction, stable tie, clamp. |
| Collection | DTOs and `TmdbClient.GetCollectionAsync` implemented; collection-aware scoring is scaffolded in `RecommendationCandidate` and `CandidateScorer`. Full collection logic is deferred to V2. |
| Local resolver | `LocalMovieResolverTests` — no matches, partial matches, non-movie item with same provider ID, null user. |
| HTTP failures | `TmdbClientTests` — 400, 401, 403, 404, 429, 500, malformed JSON, timeout, cancellation. |
| Provider | `SmartTmdbMovieProvider` implemented; provider-level integration requires running Jellyfin instance. |
| Privacy/cache | `MemoryRawTmdbCacheTests` — raw cache shared safely; final results are not core-cached; user A watched state never changes user B output by design (watched state is attached per-request, not cached). |

## Manual Test Cases

### 1. Plugin loads with no assembly errors

- Restart Jellyfin.
- Check server logs for plugin load messages.
- Expected: No assembly errors. Plugin appears in Dashboard > Plugins.

### 2. Configuration page loads, saves, and reloads

- Open Dashboard > Plugins > Smart TMDB Recommendations.
- Verify all settings are visible with default values.
- Change a setting (e.g., Minimum TMDB rating to 6.0) and click Save.
- Reload the page.
- Expected: Setting persists.

### 3. Provider appears for Movie libraries

- Go to Dashboard > Libraries > Movies > More.
- Under **Similar content providers**, look for **Smart TMDB Recommendations**.
- Expected: Provider is listed and can be enabled.

### 4. With provider disabled, behavior is unchanged

- Disable **Smart TMDB Recommendations**.
- View More Like This for a test movie.
- Expected: Default Jellyfin/TMDB results appear; no plugin-specific results.

### 5. With it enabled but below Local Genre/Tag, logs demonstrate earlier provider

- Enable **Smart TMDB Recommendations** and order it below **Local Genre/Tag**.
- View More Like This for a test movie.
- Expected: Default results appear; logs show earlier provider filled the row.

### 6. With it first, it is invoked and ordering is visible

- Move **Smart TMDB Recommendations** to the top.
- View More Like This for a test movie.
- Expected: Plugin results appear with TMDB provider name.

### 7. TMDB titles absent from local library never appear

- Use a test movie with TMDB recommendations that include titles not in the local library.
- Expected: Only local titles appear in results.

### 8. Bad token fails cleanly and local fallback still supplies results

- Enter an invalid TMDB token and save.
- View More Like This for a test movie.
- Expected: No plugin results; Jellyfin's default provider supplies results. Server log shows auth failure.

### 9. PreferUnwatched changes ordering for a test user

- Enable **Prefer unwatched**.
- Log in as a test user who has watched one of the expected similar movies.
- View More Like This for a test movie.
- Expected: Unwatched titles rank higher than watched ones.

### 10. Two users with different watched states do not receive cross-user cached ordering

- Log in as User A (watched Movie X) and note More Like This ordering.
- Log in as User B (has not watched Movie X) and note More Like This ordering.
- Expected: Orderings differ based on watched state.

### 11. Child/restricted user cannot receive a movie outside their access rules

- Enable a parental rating restriction for a test user.
- View More Like This for a test movie.
- Expected: Only age-appropriate local titles appear.

### 12. Restart, plugin update, and uninstall are clean

- Restart Jellyfin with the plugin enabled.
- Update the plugin DLL and restart.
- Uninstall the plugin and restart.
- Expected: No errors in any step. Library settings remain valid after uninstall.

## Recording

For each case, record:

- Server version
- Plugin version
- Setup steps
- Expected result
- Actual result
- Relevant redacted logs

Fix failures before adding V2 features.

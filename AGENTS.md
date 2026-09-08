# Smart TMDB Recommendations

## Mission

Build a Jellyfin 12 movie plugin that improves item-detail More Like This results
by aggregating and re-ranking TMDB candidates that already exist in the local library.

## Canonical plan

- Read `docs/IMPLEMENTATION_PLAN.md` before changing architecture or scope.
- Work only on the task explicitly requested by the user.
- Record completed work, commands, test results, decisions, and discrepancies in
  `docs/STATUS.md` after every task.
- Stop after the requested task; do not automatically start the next backlog item.

## Compatibility

- Target `net10.0` and Jellyfin `12.0.0`.
- Verify Jellyfin APIs against tag `v12.0`, commit
  `6c073e19ddf604b2369c638716164fdab4c952dc`.
- Do not use 10.11-era signatures without verification.
- Use only public plugin/package APIs. Do not reference Jellyfin internal TMDB
  implementation classes, use reflection to reach them, fork Jellyfin, or modify jellyfin-web.

## Architecture invariants

- V1 implements `IRemoteSimilarItemsProvider<Movie>` only.
- Provider name is `Smart TMDB Recommendations`.
- Result `ProviderName` is `MetadataProvider.Tmdb.ToString()` (`Tmdb`).
- Provider `CacheDuration` is always null while output uses user state.
- Cache raw non-personal TMDB responses only; never cache final personalized results by source alone.
- Results must be local movies. Batch-resolve TMDB IDs; never scan/query once per candidate.
- The scoring engine is pure, deterministic, finite, and unit tested.
- Propagate cancellation and bound pages, concurrency, timeouts, retries, and cache size.
- Failure returns no plugin results so Jellyfin can use its next provider.

## Security and privacy

- Never commit or log API tokens, Authorization headers, or user watch history.
- Prefer `JELLYFIN_SMART_TMDB_TOKEN`; plugin configuration is the fallback.
- Use Bearer authentication, not a token in the query string.
- Validate configuration server-side.

## Quality gate

- Before finishing a task, run `dotnet format --verify-no-changes`, `dotnet build`,
  and `dotnet test` when those commands are available/relevant.
- Do not say a check passed unless its output was observed.
- Add tests for behavior changes and HTTP failure paths.
- Treat warnings as errors and keep nullable annotations enabled.

## Scope limits

- Do not implement autoplay, dismissal UI, non-local results, scraping, MovieLens,
  embeddings, per-user preference pages, TV support, or home Suggestions rows in V1.
- Do not add a database or third-party TMDB wrapper without a written decision and approval.

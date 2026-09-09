# Smart TMDB Recommendations

Configurable TMDB-based movie recommendations for Jellyfin.

## Prerequisites

- Jellyfin 12.0.0 or later
- .NET 10 runtime (included with Jellyfin 12)
- TMDB API Read Access Token ([create one here](https://www.themoviedb.org/settings/api))

## First supported target

Jellyfin 12.0.0 / .NET 10.

## What it does

This plugin improves the **More Like This** row on movie item-detail pages by aggregating and re-ranking TMDB recommendation and similar candidates that already exist in your local library.

V1 is limited to **movie item-detail More Like This** only. It does not add Suggestions rows, TV support, or autoplay.

## Installation

### Option A: Plugin catalog (recommended)

If your Jellyfin instance has the plugin catalog enabled, the plugin is
available as **Smart TMDB Recommendations**:

1. Go to **Dashboard > Plugins > Catalog**.
2. Find **Smart TMDB Recommendations**.
3. Click **Install** and restart Jellyfin.

If the catalog does not show the plugin yet, refresh the catalog or use
Option B.

### Option B: Manual install

1. Download `Jellyfin.Plugin.SmartTmdb_<version>.zip` from the latest release.
2. Extract the DLL into your Jellyfin `plugins/` directory.
3. Restart Jellyfin.

To build from source:

```
dotnet build --configuration Release
```

## Configuration

1. Go to **Dashboard > Plugins > Smart TMDB Recommendations**.
2. Enter your TMDB API Read Access Token, or set the `JELLYFIN_SMART_TMDB_TOKEN` environment variable (it takes precedence).
3. Adjust recommendation style, watched/franchise/era/language modes, and thresholds as desired.
4. Click **Save**.

### Per-library provider enable/order

1. Go to **Dashboard > Libraries > Movies > More** (or **Edit metadata settings**).
2. Under **Similar content providers**, ensure **Smart TMDB Recommendations** is present.
3. Enable it and move it to the desired order. It appears as `Smart TMDB Recommendations` for Movie libraries.

## How it works

- The provider fetches TMDB recommendations and similar pages for the source movie.
- Candidates are filtered by vote average, vote count, adult flag, and era/language modes.
- Remaining candidates are scored using genre overlap, era closeness, Bayesian quality, popularity percentile, and watched/franchise adjustments.
- Only candidates that already exist in your local library are returned.
- If the provider is disabled or fails, Jellyfin falls back to its default similar-items behavior.

## Troubleshooting

- **No results**: Verify the TMDB token is valid and the source movie has a TMDB ID.
- **Token not persisting**: Ensure you are not using the `JELLYFIN_SMART_TMDB_TOKEN` environment variable, which disables the config field.
- **Results not changing**: Check that the provider is enabled and ordered above the default TMDB provider in library settings.
- **Adult content appearing**: Disable **Include adult candidates** in plugin settings.

## Attribution

This product uses the [TMDB](https://www.themoviedb.org/) API but is not endorsed or certified by TMDB.

## Limitations

- V1 returns only local movies. Non-local TMDB titles are never shown.
- Scoring is deterministic and global per request; per-user editable profiles are deferred to V2.
- Raw TMDB responses are cached in memory for up to 7 days. Personalized results are never cached.

## License

GPL-3.0-only. See [LICENSE](LICENSE).

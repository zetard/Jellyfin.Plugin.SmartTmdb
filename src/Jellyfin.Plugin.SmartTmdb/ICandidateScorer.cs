using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Candidate scorer.
/// </summary>
public interface ICandidateScorer
{
    /// <summary>
    /// Scores all candidates in a batch, preserving pool-relative features like popularity percentile.
    /// </summary>
    /// <param name="candidates">Candidates to score.</param>
    /// <param name="sourceGenreIds">Source movie genre IDs.</param>
    /// <param name="sourceReleaseDate">Source movie release date.</param>
    /// <param name="sourceOriginalLanguage">Source movie original language.</param>
    /// <param name="settings">Validated settings snapshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Score results in the same order as candidates.</returns>
    Task<IReadOnlyList<ScoreResult>> ScoreAllAsync(
        IReadOnlyList<RecommendationCandidate> candidates,
        IReadOnlySet<int> sourceGenreIds,
        DateOnly? sourceReleaseDate,
        string? sourceOriginalLanguage,
        SettingsSnapshot settings,
        CancellationToken cancellationToken);
}

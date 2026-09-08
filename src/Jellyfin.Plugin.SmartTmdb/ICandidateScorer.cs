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
    /// Scores a candidate.
    /// </summary>
    /// <param name="candidate">Candidate to score.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Score between 0.0 and 0.95.</returns>
    Task<float> ScoreAsync(RecommendationCandidate candidate, CancellationToken cancellationToken);
}

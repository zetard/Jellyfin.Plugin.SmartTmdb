using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Candidate scorer skeleton.
/// </summary>
public sealed class CandidateScorer : ICandidateScorer
{
    /// <inheritdoc/>
    public Task<float> ScoreAsync(RecommendationCandidate candidate, CancellationToken cancellationToken)
    {
        return Task.FromResult(0.0f);
    }
}

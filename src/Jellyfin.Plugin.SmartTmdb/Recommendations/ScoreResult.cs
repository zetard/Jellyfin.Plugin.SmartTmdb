using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.SmartTmdb;

/// <summary>
/// Candidate scorer result.
/// </summary>
/// <param name="Score">Final score between 0.0 and 0.95.</param>
/// <param name="IsFiltered">Whether the candidate was filtered out by a hard mode.</param>
/// <param name="Reasons">Score reason codes for debugging.</param>
public sealed record ScoreResult(float Score, bool IsFiltered, IReadOnlyList<string> Reasons);

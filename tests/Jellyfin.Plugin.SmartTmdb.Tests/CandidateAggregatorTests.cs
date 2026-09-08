using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SmartTmdb;
using Jellyfin.Plugin.SmartTmdb.Configuration;
using Jellyfin.Plugin.SmartTmdb.Tmdb;
using Xunit;

namespace Jellyfin.Plugin.SmartTmdb.Tests;

public class CandidateAggregatorTests
{
    private static SettingsSnapshot CreateSettings(bool includeAdult = true, double minVoteAverage = 0.0, int minVoteCount = 0)
    {
        return new SettingsSnapshot(new PluginConfiguration
        {
            IncludeAdult = includeAdult,
            MinimumVoteAverage = minVoteAverage,
            MinimumVoteCount = minVoteCount,
            UseRecommendations = true,
            UseSimilar = true,
        });
    }

    [Fact]
    public void Merge_CandidateOnlyInRecommendations_IncludesCandidate()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbRecommendationPageDto recPage = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Rec Only" },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { recPage },
            new List<TmdbSimilarPageDto>(),
            CreateSettings());

        Assert.Single(results);
        Assert.Equal(10, results[0].TmdbId);
        Assert.Equal(1, results[0].RecommendationRank);
    }

    [Fact]
    public void Merge_CandidateOnlyInSimilar_IncludesCandidate()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbSimilarPageDto simPage = new TmdbSimilarPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 20, Title = "Sim Only" },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto>(),
            new List<TmdbSimilarPageDto> { simPage },
            CreateSettings());

        Assert.Single(results);
        Assert.Equal(20, results[0].TmdbId);
        Assert.Equal(1, results[0].SimilarRank);
    }

    [Fact]
    public void Merge_CandidateInBoth_RetainsBestRanks()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbRecommendationPageDto recPage = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Both" },
            },
        };
        TmdbSimilarPageDto simPage = new TmdbSimilarPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Both" },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { recPage },
            new List<TmdbSimilarPageDto> { simPage },
            CreateSettings());

        Assert.Single(results);
        Assert.Equal(10, results[0].TmdbId);
        Assert.Equal(1, results[0].RecommendationRank);
        Assert.Equal(1, results[0].SimilarRank);
    }

    [Fact]
    public void Merge_SourceItemReturned_Excluded()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbRecommendationPageDto recPage = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 1, Title = "Source" },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { recPage },
            new List<TmdbSimilarPageDto>(),
            CreateSettings());

        Assert.Empty(results);
    }

    [Fact]
    public void Merge_DuplicateAcrossPages_RetainsBestRank()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbRecommendationPageDto page1 = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Dup" },
            },
        };
        TmdbRecommendationPageDto page2 = new TmdbRecommendationPageDto
        {
            Page = 2,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Dup" },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { page1, page2 },
            new List<TmdbSimilarPageDto>(),
            CreateSettings());

        Assert.Single(results);
        Assert.Equal(1, results[0].RecommendationRank);
    }

    [Fact]
    public void Merge_AdultFiltered_WhenIncludeAdultFalse()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbRecommendationPageDto recPage = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Adult", Adult = true },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { recPage },
            new List<TmdbSimilarPageDto>(),
            CreateSettings(includeAdult: false));

        Assert.Empty(results);
    }

    [Fact]
    public void Merge_VoteAverageFiltered_WhenBelowThreshold()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbRecommendationPageDto recPage = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Low", VoteAverage = 3.0, VoteCount = 1000 },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { recPage },
            new List<TmdbSimilarPageDto>(),
            CreateSettings(minVoteAverage: 5.5));

        Assert.Empty(results);
    }

    [Fact]
    public void Merge_VoteCountFiltered_WhenBelowThreshold()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        TmdbRecommendationPageDto recPage = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = new List<TmdbMovieResultDto>
            {
                new TmdbMovieResultDto { Id = 10, Title = "Low", VoteAverage = 7.0, VoteCount = 10 },
            },
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { recPage },
            new List<TmdbSimilarPageDto>(),
            CreateSettings(minVoteCount: 100));

        Assert.Empty(results);
    }

    [Fact]
    public void Merge_MaxTotalCandidates_RespectsCap()
    {
        TmdbMovieDetailsDto source = new TmdbMovieDetailsDto { Id = 1 };
        List<TmdbMovieResultDto> resultsList = new List<TmdbMovieResultDto>();
        for (int i = 2; i <= 250; i++)
        {
            resultsList.Add(new TmdbMovieResultDto { Id = i, Title = $"Movie {i}" });
        }

        TmdbRecommendationPageDto recPage = new TmdbRecommendationPageDto
        {
            Page = 1,
            Results = resultsList,
        };

        IReadOnlyList<RecommendationCandidate> results = CandidateAggregator.Merge(
            source,
            new List<TmdbRecommendationPageDto> { recPage },
            new List<TmdbSimilarPageDto>(),
            CreateSettings());

        Assert.Equal(200, results.Count);
    }
}

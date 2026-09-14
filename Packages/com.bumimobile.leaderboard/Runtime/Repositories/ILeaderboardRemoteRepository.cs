using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace BumiMobile.Leaderboard
{
    public interface ILeaderboardRemoteRepository
    {
        UniTask<bool> EnsureReadyAsync();
        string CurrentUserId { get; }
        UniTask SubmitScoreAsync(long absoluteScore, int pendingDelta, string displayName, string countryIso);
        UniTask<List<LeaderboardEntry>> GetGlobalLeaderboardAsync(int limit);
        UniTask<List<LeaderboardEntry>> GetRegionalLeaderboardAsync(string countryIso, int limit);
        UniTask<List<LeaderboardEntry>> GetCountryLeaderboardAsync(int limit);
        UniTask<LeaderboardEntry> GetPlayerEntryAsync(string userId);
    }
}

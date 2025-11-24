using System.Collections.Generic;
using BumiMobile;
using Cysharp.Threading.Tasks;

namespace BumiMobile.Leaderboard
{
    sealed class NoopLeaderboardRepository : ILeaderboardRemoteRepository
    {
        public string CurrentUserId => AuthService.User?.UserId;

        public UniTask<bool> EnsureReadyAsync() => UniTask.FromResult(true);

        public UniTask SubmitScoreAsync(long absoluteScore, int pendingDelta, string displayName, string countryIso)
            => UniTask.CompletedTask;

        public UniTask<List<LeaderboardEntry>> GetGlobalLeaderboardAsync(int limit)
            => UniTask.FromResult(new List<LeaderboardEntry>());

        public UniTask<List<LeaderboardEntry>> GetRegionalLeaderboardAsync(string countryIso, int limit)
            => UniTask.FromResult(new List<LeaderboardEntry>());

        public UniTask<List<LeaderboardEntry>> GetCountryLeaderboardAsync(int limit)
            => UniTask.FromResult(new List<LeaderboardEntry>());

        public UniTask<LeaderboardEntry> GetPlayerEntryAsync(string userId)
            => UniTask.FromResult<LeaderboardEntry>(null);
    }
}

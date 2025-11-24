using System;
using BumiMobile;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BumiMobile.Leaderboard
{
    public sealed class LeaderboardWarmupLoadingTask : LoadingTask
    {
        readonly int _limit;

        public LeaderboardWarmupLoadingTask(int topLimit = 10)
        {
            _limit = topLimit;
        }

        public override string TaskName => "Fetching Leaderboards";

        protected override async void OnTaskActivated()
        {
            try
            {
                await LeaderboardController.EnsureWarmup(_limit);
                CompleteTask(CompleteStatus.Completed);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] Warmup task failed or skipped: {e.Message}");
                CompleteTask(CompleteStatus.Skipped);
            }
        }
    }
}

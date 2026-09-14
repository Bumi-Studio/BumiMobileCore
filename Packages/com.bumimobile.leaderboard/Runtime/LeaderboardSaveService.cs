using System;
using UnityEngine;
using BumiMobile;

namespace BumiMobile.Leaderboard
{
    public static class LeaderboardSaveService
    {
        const string SAVE_KEY = "leaderboard_global_save";

        static LeaderboardSaveData cache;
        static bool migrationAttempted;

        static LeaderboardSaveData Data
        {
            get
            {
                cache ??= SaveController.GetSaveObject<LeaderboardSaveData>(SAVE_KEY);
                return cache;
            }
        }

        public static void EnsureInitialized()
        {
            var save = Data;
            if (save == null || save.LegacyScoreMigrated || migrationAttempted)
                return;

            migrationAttempted = true;
            try
            {
                var legacyScore = SaveController.GetSaveObject<SimpleIntSave>("score");
                if (legacyScore != null)
                {
                    save.TotalScore = Mathf.Max(save.TotalScore, legacyScore.Value);
                }

                save.PendingScore = Mathf.Max(save.PendingScore, 0);
                save.LastSubmittedScore = Mathf.Clamp(save.LastSubmittedScore, 0, save.TotalScore);
                save.LegacyScoreMigrated = true;
                SaveController.MarkAsSaveIsRequired();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] Legacy migration failed: {e.Message}");
            }
        }

        public static int TotalScore => Data.TotalScore;
        public static int PendingScore => Data.PendingScore;
        public static bool HasPending => Data.PendingScore != 0;

        public static void AddScore(int delta)
        {
            if (delta == 0) return;
            var save = Data;
            save.TotalScore = Mathf.Max(0, save.TotalScore + delta);
            save.PendingScore = Mathf.Max(0, save.PendingScore + delta);
            SaveController.MarkAsSaveIsRequired();
        }

        public static void OverrideScore(int absoluteValue, bool markAsPending)
        {
            var save = Data;
            int clamped = Mathf.Max(0, absoluteValue);
            int delta = clamped - save.TotalScore;
            save.TotalScore = clamped;
            if (markAsPending)
            {
                save.PendingScore = Mathf.Max(0, save.PendingScore + delta);
            }
            else
            {
                save.PendingScore = Mathf.Clamp(save.PendingScore, 0, save.TotalScore);
            }
            SaveController.MarkAsSaveIsRequired();
        }

        public static void MarkSubmissionSuccess(string playerName, string countryIso)
        {
            var save = Data;
            save.PendingScore = 0;
            save.LastSubmittedScore = save.TotalScore;
            save.LastSubmitUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!string.IsNullOrWhiteSpace(playerName))
                save.LastSubmittedName = playerName.Trim();
            save.LastSubmittedCountryISO = NormalizeIso(countryIso);
            SaveController.MarkAsSaveIsRequired();
        }

        public static void ResetScore()
        {
            var save = Data;
            save.TotalScore = 0;
            save.PendingScore = 0;
            save.LastSubmittedScore = 0;
            save.LastSubmitUnixTime = 0;
            save.LastSubmittedName = null;
            save.LastSubmittedCountryISO = "ZZ";
            SaveController.MarkAsSaveIsRequired();
        }

        static string NormalizeIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return "ZZ";
            iso = iso.Trim();
            return iso.Length == 2 ? iso.ToUpperInvariant() : "ZZ";
        }
    }
}

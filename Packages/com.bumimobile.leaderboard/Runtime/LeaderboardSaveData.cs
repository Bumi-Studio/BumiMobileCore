using System;
using BumiMobile;

namespace BumiMobile.Leaderboard
{
    [Serializable]
    public class LeaderboardSaveData : ISaveObject
    {
        public int TotalScore;
        public int PendingScore;
        public int LastSubmittedScore;
        public long LastSubmitUnixTime;
        public string LastSubmittedName;
        public string LastSubmittedCountryISO = "ZZ";
        public bool LegacyScoreMigrated;

        public void Flush()
        {
        }
    }
}

using System;

namespace BumiMobile.Leaderboard
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string userId;
        public string name;
        public int score;
        public string country;
        public DateTime updatedAt;
    }

    public enum LeaderboardType
    {
        Global,
        Regional,
        Country
    }
}

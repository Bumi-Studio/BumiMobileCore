using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BumiMobile.Leaderboard
{
    [CreateAssetMenu(menuName = "BumiMobile/Leaderboard/Leaderboard Init Module", fileName = "LeaderboardInitModule")]
    [RegisterModule("Leaderboard", core: false, order: 10)]
    public class LeaderboardInitModule : InitModule
    {
        [Header("Warmup Settings")]
        [SerializeField] private bool enableWarmup = true;
        [SerializeField] private int warmupTopLimit = 10;

        public override string ModuleName => "Leaderboard";

        public override void CreateComponent()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
#if BUMI_AUTH_HAS_FIREBASE
            // Only initialize leaderboard if authentication succeeded
            if (!AuthenticatedInitModule.IsAuthenticated)
            {
                Debug.Log("[Leaderboard] Skipping initialization - user not authenticated.");
                return;
            }

            Debug.Log("[Leaderboard] Starting initialization...");
            LeaderboardController.Init();

            if (enableWarmup)
            {
                Debug.Log($"[Leaderboard] Running warmup (top {warmupTopLimit})...");
                await LeaderboardController.EnsureWarmup(warmupTopLimit);
                Debug.Log("[Leaderboard] Warmup complete.");
            }
            else
            {
                Debug.Log("[Leaderboard] Warmup disabled.");
            }
#else
            Debug.LogWarning("[Leaderboard] Firebase not available. Define BUMI_AUTH_HAS_FIREBASE to enable leaderboard.");
#endif
        }
    }
}

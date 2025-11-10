using UnityEngine;

#if UNITY_IOS && MODULE_IDFA
using Unity.Advertisement.IosSupport;
#endif

namespace BumiMobile
{
    public sealed class IDFALoadingTask : LoadingTask
    {
        private MonetizationSettings settings;
        private TweenCase checkTweenCase;

        public IDFALoadingTask(MonetizationSettings settings) : base()
        {
            this.settings = settings;
        }

        public override string TaskName => "IDFA";

        public override void OnTaskActivated()
        {
            if (!settings.AdsSettings.IsIDFAEnabled)
            {
                CompleteTask(CompleteStatus.Skipped);

                return;
            }

#if UNITY_IOS && MODULE_IDFA
            if (AdsManager.IsIDFADetermined())
            {
                CompleteTask(CompleteStatus.Completed);
            }

            if (Monetization.VerboseLogging)
                Debug.Log("[Ads Manager]: Requesting IDFA..");

            ATTrackingStatusBinding.RequestAuthorizationTracking();

            CheckStatus();
#else
            CompleteTask(CompleteStatus.Skipped);
#endif
        }

        private void CheckStatus()
        {
#if UNITY_IOS && MODULE_IDFA
            checkTweenCase.KillActive();

            ATTrackingStatusBinding.AuthorizationTrackingStatus status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

            if (Monetization.VerboseLogging)
                Debug.Log($"[Ads Manager]: IDFA status - {status}");

            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                checkTweenCase = Tween.DelayedCall(0.3f, CheckStatus, unscaledTime: true);
            }
            else
            {
                CompleteTask(CompleteStatus.Completed);
            }
#endif
        }
    }
}

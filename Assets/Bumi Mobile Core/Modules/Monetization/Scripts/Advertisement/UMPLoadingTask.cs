using UnityEngine;

#if MODULE_ADMOB
using GoogleMobileAds.Ump.Api;
#endif

namespace BumiMobile
{
    public sealed class UMPLoadingTask : LoadingTask
    {
        private MonetizationSettings settings;

        public UMPLoadingTask(MonetizationSettings settings) : base()
        {
            this.settings = settings;
        }

        public override string TaskName => "Ads";

        public override void OnTaskActivated()
        {
            if(!settings.AdsSettings.IsUMPEnabled)
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: UMP is disabled. Task is skipped.");

                CompleteTask(CompleteStatus.Skipped);

                return;
            }

#if MODULE_ADMOB
            if (ConsentInformation.CanRequestAds())
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: UMP is already completed, ad can be loaded.");

                CompleteTask(CompleteStatus.Completed);
            }

            ConsentRequestParameters requestParameters = GetRequestParameters();

            ConsentInformation.Update(requestParameters, (FormError updateError) =>
            {
                if (updateError != null)
                {
                    Debug.LogError("[AdsManager]: Failed to gather consent: " + updateError.Message);

                    CompleteTask(CompleteStatus.Failed);

                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
                {
                    if (showError != null)
                    {
                        Debug.LogError("[AdsManager]: Failed to show consent: " + showError.Message);

                        CompleteTask(CompleteStatus.Failed);

                        return;
                    }

                    if (ConsentInformation.CanRequestAds())
                    {
                        if (Monetization.VerboseLogging)
                            Debug.Log("[AdsManager]: UMP successfully completed, now ads can be loaded.");

                        CompleteTask(CompleteStatus.Completed);
                    }
                });
            });
#else
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: AdMob package can't be found. UMP task is skipped.");

            CompleteTask(CompleteStatus.Skipped);
#endif
        }

#if MODULE_ADMOB
        private ConsentRequestParameters GetRequestParameters()
        {
            ConsentRequestParameters requestParameters = new ConsentRequestParameters();

            if (settings.AdsSettings.UMPDebugMode)
            {
                requestParameters.ConsentDebugSettings = new ConsentDebugSettings()
                {
                    DebugGeography = (GoogleMobileAds.Ump.Api.DebugGeography)settings.AdsSettings.UMPDebugGeography,
                    TestDeviceHashedIds = settings.TestDevices
                };
            }

            return requestParameters;
        }
#endif
    }
}

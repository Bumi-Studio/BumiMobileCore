using UnityEngine;
using UnityEditor;

namespace BumiMobile
{
    public class EditorAdMobContainer : EditorAdsContainer
    {
        //App section
        private const string SETTINGS_FILE_PATH = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string TEST_APP_ID = "ca-app-pub-3940256099942544~3347511713";
        private const string ANDROID_APP_ID_PROPERTY_PATH = "adMobAndroidAppId";
        private const string OUR_ANDROID_APP_ID_PROPERTY_PATH = "androidAppId";
        private const string IOS_APP_ID_PROPERTY_PATH = "adMobIOSAppId";
        private const string OUR_IOS_APP_ID_PROPERTY_PATH = "iosAppId";

        private SerializedProperty androidAppIdProperty;
        private SerializedProperty iOSAppIdProperty;
        private SerializedProperty ourAndroidAppIdProperty;
        private SerializedProperty ourIOSAppIdProperty;
        private UnityEngine.Object settingsFile;
        private SerializedObject serializedObject;
        private bool fileLoaded;

        //Add Units section 
        private const string BANNER_TYPE_PROPERTY_PATH = "bannerType";
        private const string BANNER_POSITION_PROPERTY_PATH = "bannerPosition";
        private const string ANDROID_BANNER_ID_PROPERTY_PATH = "androidBannerID";
        private const string ANDROID_INTERSTITIAL_ID_PROPERTY_PATH = "androidInterstitialID";
        private const string ANDROID_REWARDED_VIDEO_ID_PROPERTY_PATH = "androidRewardedVideoID";
        private const string IOS_BANNER_ID_PROPERTY_PATH = "iOSBannerID";
        private const string IOS_INTERSTITIAL_ID_PROPERTY_PATH = "iOSInterstitialID";
        private const string IOS_REWARDED_VIDEO_ID_PROPERTY_PATH = "iOSRewardedVideoID";

        private SerializedProperty bannerTypeProperty;
        private SerializedProperty bannerPositionProperty;
        private SerializedProperty androidBannerIdProperty;
        private SerializedProperty androidInterstitialIdProperty;
        private SerializedProperty androidRewardedVideoIdProperty;
        private SerializedProperty iOSBannerIdProperty;
        private SerializedProperty iOSInterstitialIdProperty;
        private SerializedProperty iOSRewardedVideoIdProperty;

        private SerializedProperty useAppOpenAdProperty;
        private SerializedProperty androidAppOpenAdIDProperty;
        private SerializedProperty iosAppOpenAdIDProperty;

        private GUIContent testIdContent;
        private GUIStyle groupStyle;

        public EditorAdMobContainer(string containerName, string propertyName) : base(containerName, propertyName)
        {
        }

        public override void Init(SerializedObject serializedObject)
        {
            base.Init(serializedObject);

            //for add units section
            bannerTypeProperty = containerProperty.FindPropertyRelative(BANNER_TYPE_PROPERTY_PATH);
            bannerPositionProperty = containerProperty.FindPropertyRelative(BANNER_POSITION_PROPERTY_PATH);
            androidBannerIdProperty = containerProperty.FindPropertyRelative(ANDROID_BANNER_ID_PROPERTY_PATH);
            androidInterstitialIdProperty = containerProperty.FindPropertyRelative(ANDROID_INTERSTITIAL_ID_PROPERTY_PATH);
            androidRewardedVideoIdProperty = containerProperty.FindPropertyRelative(ANDROID_REWARDED_VIDEO_ID_PROPERTY_PATH);
            iOSBannerIdProperty = containerProperty.FindPropertyRelative(IOS_BANNER_ID_PROPERTY_PATH);
            iOSInterstitialIdProperty = containerProperty.FindPropertyRelative(IOS_INTERSTITIAL_ID_PROPERTY_PATH);
            iOSRewardedVideoIdProperty = containerProperty.FindPropertyRelative(IOS_REWARDED_VIDEO_ID_PROPERTY_PATH);

            useAppOpenAdProperty = containerProperty.FindPropertyRelative("useAppOpenAd");
            androidAppOpenAdIDProperty = containerProperty.FindPropertyRelative("androidAppOpenAdID");
            iosAppOpenAdIDProperty = containerProperty.FindPropertyRelative("iOSAppOpenAdID");

            // for app section
            fileLoaded = false;
            ourAndroidAppIdProperty = containerProperty.FindPropertyRelative(OUR_ANDROID_APP_ID_PROPERTY_PATH);
            ourIOSAppIdProperty = containerProperty.FindPropertyRelative(OUR_IOS_APP_ID_PROPERTY_PATH);

            // Prepare styles
            testIdContent = new GUIContent("", EditorCustomStyles.GetIcon("icon_warning"), "You are using test app id value.");
            groupStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            groupStyle.fontStyle = FontStyle.Bold;
            groupStyle.alignment = TextAnchor.LowerLeft;

            LoadFile();
        }

        private void LoadFile()
        {
            settingsFile = AssetDatabase.LoadMainAssetAtPath(SETTINGS_FILE_PATH);

            if (settingsFile != null)
            {
                serializedObject = new SerializedObject(settingsFile);
                androidAppIdProperty = serializedObject.FindProperty(ANDROID_APP_ID_PROPERTY_PATH);
                iOSAppIdProperty = serializedObject.FindProperty(IOS_APP_ID_PROPERTY_PATH);
                fileLoaded = true;
            }
            else
            {
#if MODULE_ADMOB
                GoogleMobileAds.Editor.GoogleMobileAdsSettingsEditor.OpenInspector(); // Creates admob settings file

                LoadFile();

                if (fileLoaded)
                {

                    if ((androidAppIdProperty.stringValue.Length == 0) && (ourAndroidAppIdProperty.stringValue.Length != 0))
                    {
                        androidAppIdProperty.stringValue = ourAndroidAppIdProperty.stringValue;
                    }
                    else if ((androidAppIdProperty.stringValue.Length != 0) && (ourAndroidAppIdProperty.stringValue.Length == 0))
                    {
                        ourAndroidAppIdProperty.stringValue = androidAppIdProperty.stringValue;
                    }

                    if ((iOSAppIdProperty.stringValue.Length == 0) && (ourIOSAppIdProperty.stringValue.Length != 0))
                    {
                        iOSAppIdProperty.stringValue = ourIOSAppIdProperty.stringValue;
                    }
                    else if ((iOSAppIdProperty.stringValue.Length != 0) && (ourIOSAppIdProperty.stringValue.Length == 0))
                    {
                        ourIOSAppIdProperty.stringValue = iOSAppIdProperty.stringValue;
                    }


                    serializedObject.ApplyModifiedProperties();
                    ourAndroidAppIdProperty.serializedObject.ApplyModifiedProperties();
                }
#endif
            }
        }

        public override void DrawContainer()
        {
            containerProperty.isExpanded = EditorGUILayoutCustom.BeginExpandBoxGroup(containerName, containerProperty.isExpanded);

            if (containerProperty.isExpanded)
            {
                DrawAppSection();
                DrawAddUnitsSection();

                DrawUsefulSection();

                containerProperty.serializedObject.ApplyModifiedProperties();

                if (fileLoaded)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }

        private void DrawAppSection()
        {
            EditorGUILayout.LabelField("Application ID", groupStyle);

            DrawIdProperty(ourAndroidAppIdProperty, TEST_APP_ID);
            DrawIdProperty(ourIOSAppIdProperty, TEST_APP_ID);

            if (fileLoaded)
            {
                androidAppIdProperty.stringValue = ourAndroidAppIdProperty.stringValue;
                iOSAppIdProperty.stringValue = ourIOSAppIdProperty.stringValue;
            }
        }

        private void DrawIdProperty(SerializedProperty property, string testValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property);

            if (property.stringValue.Equals(testValue))
            {
                EditorGUILayout.LabelField(testIdContent, GUILayout.MaxWidth(24));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddUnitsSection()
        {
            EditorGUILayout.LabelField("Banner ID", groupStyle);
            DrawIdProperty(androidBannerIdProperty, AdMobContainer.ANDROID_BANNER_TEST_ID);
            DrawIdProperty(iOSBannerIdProperty, AdMobContainer.IOS_BANNER_TEST_ID);

            EditorGUILayout.PropertyField(bannerTypeProperty);
            EditorGUILayout.PropertyField(bannerPositionProperty);

            EditorGUILayout.LabelField("Interstitial ID", groupStyle);
            DrawIdProperty(androidInterstitialIdProperty, AdMobContainer.ANDROID_INTERSTITIAL_TEST_ID);
            DrawIdProperty(iOSInterstitialIdProperty, AdMobContainer.IOS_INTERSTITIAL_TEST_ID);

            EditorGUILayout.LabelField("Rewarded Video ID", groupStyle);
            DrawIdProperty(androidRewardedVideoIdProperty, AdMobContainer.ANDROID_REWARDED_VIDEO_TEST_ID);
            DrawIdProperty(iOSRewardedVideoIdProperty, AdMobContainer.IOS_REWARDED_VIDEO_TEST_ID);

            EditorGUILayout.LabelField("App Open AD", groupStyle);
            EditorGUILayout.PropertyField(useAppOpenAdProperty);
            if (useAppOpenAdProperty.boolValue)
            {
                DrawIdProperty(androidAppOpenAdIDProperty, AdMobContainer.ANDROID_OPEN_TEST_ID);
                DrawIdProperty(iosAppOpenAdIDProperty, AdMobContainer.IOS_OPEN_TEST_ID);
            }

            EditorGUILayout.LabelField("Debug", EditorCustomStyles.labelMediumBold);

            if (GUILayout.Button("Set test app id", EditorCustomStyles.button))
            {
                ourAndroidAppIdProperty.stringValue = TEST_APP_ID;
                ourIOSAppIdProperty.stringValue = TEST_APP_ID;
                containerProperty.serializedObject.ApplyModifiedProperties();

                if (fileLoaded)
                {
                    androidAppIdProperty.stringValue = ourAndroidAppIdProperty.stringValue;
                    iOSAppIdProperty.stringValue = ourIOSAppIdProperty.stringValue;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            if (GUILayout.Button("Set test ids", EditorCustomStyles.button))
            {
                androidBannerIdProperty.stringValue = AdMobContainer.ANDROID_BANNER_TEST_ID;
                iOSBannerIdProperty.stringValue = AdMobContainer.IOS_BANNER_TEST_ID;

                androidInterstitialIdProperty.stringValue = AdMobContainer.ANDROID_INTERSTITIAL_TEST_ID;
                iOSInterstitialIdProperty.stringValue = AdMobContainer.IOS_INTERSTITIAL_TEST_ID;

                androidRewardedVideoIdProperty.stringValue = AdMobContainer.ANDROID_REWARDED_VIDEO_TEST_ID;
                iOSRewardedVideoIdProperty.stringValue = AdMobContainer.IOS_REWARDED_VIDEO_TEST_ID;

                androidAppOpenAdIDProperty.stringValue = AdMobContainer.ANDROID_OPEN_TEST_ID;
                iosAppOpenAdIDProperty.stringValue = AdMobContainer.IOS_OPEN_TEST_ID;
            }
        }

        private void DrawUsefulSection()
        {
            EditorGUILayout.LabelField("Useful", EditorCustomStyles.labelMediumBold);

            if (GUILayout.Button("Download AdMob plugin", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://github.com/googleads/googleads-mobile-unity/releases");
            }

            if (GUILayout.Button("AdMob Dashboard", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://apps.admob.com/v2/home");
            }

            if (GUILayout.Button("AdMob Quick Start Guide", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://developers.google.com/admob/unity/start");
            }

            GUILayout.Space(8);

            EditorGUILayout.HelpBox("Tested with AdMob Plugin v9.2.1", MessageType.Info);
        }

        protected override void SpecialButtons()
        {
        }
    }
}
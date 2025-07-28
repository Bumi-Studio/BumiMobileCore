using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BumiMobile
{

    public class AboutWindow : EditorWindow
    {
        private static readonly Vector2 WINDOW_SIZE = new Vector2(500, 155);

        private const string WINDOW_TITLE = "About";

        private const string SITE_URL = @"http://bumistudio.co.id/";

        private const string MAIL_URL = @"https://bumistudio.co.id/";
        private const string DISCORD_URL = @"https://discord.gg";

        private const string DEFAULT_VALUE = "[unknown]";
        private const string DEFAULT_DOCUMENTATION_URL = @"https://bumistudio.atlassian.net/wiki/spaces/PM1/overview?homepageId=6390037";
        private static readonly string PROJECT_DESCRIPTION = @"Thank you for using {0}.\nBefore you start working with project, read the documentation.";

        private GUIStyle descriptionStyle;
        private GUIStyle setupButtonStyle;
        private GUIStyle gameButtonStyle;
        private GUIStyle textGamesStyle;
        private GUIStyle logoStyle;
        private GUIStyle projectStyle;
        private GUIStyle boxStyle;

        private GUIContent logoContent;
        private GUIContent mailButtonContent;
        private GUIContent discordButtonContent;
        private GUIContent documentationButtonContent;

        private string description;
        
        private string coreVersion;
        private string projectVersion;
        private string documentationUrl;
        private float defaultLength;

        [MenuItem("Window/Bumi Mobile Core/About", priority = 10000)]
        static void ShowWindow()
        {
            AboutWindow tempWindow = (AboutWindow)GetWindow(typeof(AboutWindow), true, WINDOW_TITLE);
            tempWindow.minSize = WINDOW_SIZE;
            tempWindow.maxSize = WINDOW_SIZE;
            tempWindow.titleContent = new GUIContent(WINDOW_TITLE, EditorCustomStyles.GetIcon("icon_title"));
        }

        protected void OnEnable()
        {
            EditorCustomStyles.CheckStyles();
                        
            TextAsset coreChangelogText = EditorUtils.GetAsset<TextAsset>("Core Changelog");
            if(coreChangelogText != null && !string.IsNullOrEmpty(coreChangelogText.text))
            {
                string[] lines = coreChangelogText.text.Split('\n');
                if (lines.Length > 0)
                {
                    coreVersion = lines[0];
                }
                else
                {
                    coreVersion = DEFAULT_VALUE;
                }
            }
            else
            {
                coreVersion = DEFAULT_VALUE;
            }

            TextAsset templateChangelogText = EditorUtils.GetAsset<TextAsset>("Template Changelog");
            if (templateChangelogText != null && !string.IsNullOrEmpty(templateChangelogText.text))
            {
                string[] lines = templateChangelogText.text.Split('\n');
                if(lines.Length > 0)
                {
                    projectVersion = lines[0];
                }
                else
                {
                    projectVersion = DEFAULT_VALUE;
                }
            }
            else
            {
                projectVersion = DEFAULT_VALUE;
            }

            TextAsset documentationText = EditorUtils.GetAsset<TextAsset>("DOCUMENTATION");
            if (documentationText != null && !string.IsNullOrEmpty(documentationText.text))
            {
                string[] lines = documentationText.text.Split('\n');
                if(lines.Length > 0)
                {
                    documentationUrl = lines[^1];
                }
                else
                {
                    documentationUrl = DEFAULT_DOCUMENTATION_URL;
                }
            }
            else
            {
                documentationUrl = DEFAULT_DOCUMENTATION_URL;
            }

            boxStyle = new GUIStyle(EditorCustomStyles.Skin.box);
            boxStyle.margin = new RectOffset(5, 5, 5, 5);
            boxStyle.overflow = new RectOffset(0, 0, 0, 0);
            boxStyle.padding = new RectOffset(5, 5, 5, 5);

            description = string.Format(PROJECT_DESCRIPTION, Application.productName).Replace("\\n", "\n");

            logoContent = new GUIContent(EditorCustomStyles.GetIcon("logo", EditorGUIUtility.isProSkin ? new Color(1.0f, 1.0f, 1.0f) : new Color(0.2f, 0.2f, 0.2f)), SITE_URL);

            textGamesStyle = EditorCustomStyles.labelSmall.GetAligmentStyle(TextAnchor.MiddleCenter);
            textGamesStyle.alignment = TextAnchor.MiddleCenter;
            textGamesStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            gameButtonStyle = EditorCustomStyles.button.GetPaddingStyle(new RectOffset(2, 2, 2, 2));

            descriptionStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            descriptionStyle.wordWrap = true;
            descriptionStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            setupButtonStyle = new GUIStyle(EditorCustomStyles.button);
            setupButtonStyle.imagePosition = ImagePosition.ImageAbove;
            setupButtonStyle.padding = new RectOffset(8, 8, 8, 8);

            mailButtonContent = new GUIContent(EditorCustomStyles.GetIcon("icon_mail"));
            discordButtonContent = new GUIContent(EditorCustomStyles.GetIcon("icon_discord"));
            documentationButtonContent = new GUIContent(EditorCustomStyles.ICON_SPACE + "Documentation", EditorCustomStyles.GetIcon("icon_documentation"));

            logoStyle = new GUIStyle(GUIStyle.none);
            logoStyle.alignment = TextAnchor.MiddleCenter;
            logoStyle.padding = new RectOffset(10, 10, 10, 10);

            projectStyle = new GUIStyle(EditorCustomStyles.Skin.label);
            projectStyle.alignment = TextAnchor.MiddleCenter;
            projectStyle.wordWrap = false;
            projectStyle.clipping = TextClipping.Overflow;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(logoContent, logoStyle, GUILayout.Width(80), GUILayout.Height(80)))
            {
                Application.OpenURL(SITE_URL);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(EditorCustomStyles.padding05, GUILayout.Height(21), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("GREETINGS!", EditorCustomStyles.labelBold, GUILayout.ExpandHeight(true), GUILayout.Width(110));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(discordButtonContent, EditorCustomStyles.button, GUILayout.Width(22), GUILayout.Height(22)))
            {
                Application.OpenURL(DISCORD_URL);
            }

            if (GUILayout.Button(mailButtonContent, EditorCustomStyles.button, GUILayout.Width(22), GUILayout.Height(22)))
            {
                Application.OpenURL(MAIL_URL);
            }

            if (GUILayout.Button(documentationButtonContent, EditorCustomStyles.button, GUILayout.Height(22)))
            {
                Application.OpenURL(documentationUrl);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(description, descriptionStyle);

            defaultLength = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 115;

            EditorGUILayout.LabelField("Project version", projectVersion);
            EditorGUILayout.LabelField("Core version", coreVersion);

            EditorGUIUtility.labelWidth = defaultLength;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }
    }
}
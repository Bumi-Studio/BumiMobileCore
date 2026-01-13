#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class SecretKeystoreWindow : EditorWindow
{
    private enum Tab { Unlock, Secrets, Create }
    private Tab currentTab = Tab.Unlock;

    private SecretKeystore keystore;
    private string password;
    private string confirmPassword;
    private string newAlias;
    private string newValue;
    private string errorMessage;
    private Vector2 scrollPos;

    [MenuItem("Tools/🔐 Secret Keystore")]
    public static void ShowWindow()
    {
        var window = GetWindow<SecretKeystoreWindow>("Secret Keystore");
        window.minSize = new Vector2(450, 400);
    }

    private void OnEnable()
    {
        currentTab = SecretKeystore.Exists() ? Tab.Unlock : Tab.Create;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        // Header - like Unity's keystore
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("🔐 Secret Keystore", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (keystore != null && keystore.IsUnlocked)
        {
            GUI.color = Color.green;
            GUILayout.Label("● Unlocked");
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label("● Locked");
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Show error if any
        if (!string.IsNullOrEmpty(errorMessage))
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            GUILayout.Space(5);
        }

        // Main content
        if (!SecretKeystore.Exists())
        {
            DrawCreateTab();
        }
        else if (keystore == null || !keystore.IsUnlocked)
        {
            DrawUnlockTab();
        }
        else
        {
            DrawSecretsTab();
        }
    }

    private void DrawCreateTab()
    {
        GUILayout.Label("Create New Keystore", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "No keystore found. Create one to securely store your API keys and secrets.",
            MessageType.Info
        );

        GUILayout.Space(15);

        GUILayout.Label("Keystore Password:");
        password = EditorGUILayout.PasswordField(password);

        GUILayout.Label("Confirm Password:");
        confirmPassword = EditorGUILayout.PasswordField(confirmPassword);

        GUILayout.Space(10);

        // Validation
        bool valid = !string.IsNullOrEmpty(password)
            && password == confirmPassword
            && password.Length >= 6;

        if (!string.IsNullOrEmpty(password) && password.Length < 6)
        {
            EditorGUILayout.HelpBox("Password must be at least 6 characters", MessageType.Warning);
        }
        else if (!string.IsNullOrEmpty(confirmPassword) && password != confirmPassword)
        {
            EditorGUILayout.HelpBox("Passwords don't match", MessageType.Warning);
        }

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(!valid);
        if (GUILayout.Button("Create Keystore", GUILayout.Height(35)))
        {
            try
            {
                keystore = SecretKeystore.Create(password);
                keystore.Save(password);
                errorMessage = "";
                ClearFields();
                Debug.Log("✅ Keystore created successfully!");
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    private void DrawUnlockTab()
    {
        GUILayout.Label("Unlock Keystore", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Visual keystore icon area (like Unity's)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("🔒", new GUIStyle(EditorStyles.label) { fontSize = 48 });
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("secrets.keystore.bytes", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(20);
        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        // Generate or Regenerate KeystorePassword.Local.cs
        bool keystorePasswordExists = System.IO.File.Exists("Assets/Resources/KeystorePassword.Local.cs");

        if (!keystorePasswordExists)
        {
            EditorGUILayout.HelpBox("KeystorePassword.Local.cs not found. Generate it to store the keystore password.", MessageType.Warning);
        }

        GUI.color = keystorePasswordExists ? Color.yellow : Color.cyan;
        string buttonText = keystorePasswordExists ? "🔄 Regenerate KeystorePassword.Local.cs" : "🔑 Generate KeystorePassword.Local.cs";
        if (GUILayout.Button(buttonText, GUILayout.Height(30)))
        {
            if (keystorePasswordExists)
            {
                if (EditorUtility.DisplayDialog("Regenerate Password?",
                    "This will generate a new random password and overwrite the existing KeystorePassword.Local.cs file.\n\nMake sure to update any references to the old password!",
                    "Regenerate", "Cancel"))
                {
                    GenerateKeystorePasswordScript();
                }
            }
            else
            {
                GenerateKeystorePasswordScript();
            }
        }
        GUI.color = Color.white;
        GUILayout.Space(10);

        GUILayout.Label("Password:");
        GUI.SetNextControlName("PasswordField");
        password = EditorGUILayout.PasswordField(password);

        // Allow Enter key to unlock
        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return
            && GUI.GetNameOfFocusedControl() == "PasswordField")
        {
            TryUnlock();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("🔓 Unlock", GUILayout.Height(35)))
        {
            TryUnlock();
        }

        GUILayout.Space(10);

        GUI.color = Color.gray;
        if (GUILayout.Button("Delete Keystore & Create New"))
        {
            if (EditorUtility.DisplayDialog("Delete Keystore? ",
                "This will permanently delete your keystore and all stored secrets.  This cannot be undone! ",
                "Delete", "Cancel"))
            {
                System.IO.File.Delete("Assets/Resources/secrets.keystore.bytes");
                AssetDatabase.Refresh();
                currentTab = Tab.Create;
            }
        }
        GUI.color = Color.white;
    }

    private void TryUnlock()
    {
        try
        {
            keystore = SecretKeystore.Load(password);
            errorMessage = "";
            ClearFields();
            Debug.Log("✅ Keystore unlocked!");
        }
        catch (Exception e)
        {
            errorMessage = "Invalid password! ";
            Debug.LogWarning("❌ " + e.Message);
        }
    }

    private void DrawSecretsTab()
    {
        // Toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Stored Secrets", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("🔒 Lock", EditorStyles.toolbarButton))
        {
            keystore.Lock();
            keystore = null;
            ClearFields();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // List of secrets
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        string[] aliases = keystore.Aliases;
        if (aliases.Length == 0)
        {
            EditorGUILayout.HelpBox("No secrets stored yet.  Add one below!", MessageType.Info);
        }
        else
        {
            foreach (string alias in aliases)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                GUILayout.Label("🔑", GUILayout.Width(20));
                GUILayout.Label(alias, EditorStyles.boldLabel, GUILayout.Width(150));

                string value = keystore.GetSecret(alias);
                EditorGUILayout.SelectableLabel(MaskValue(value), GUILayout.Height(18));

                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = value;
                    Debug.Log($"✅ Copied '{alias}' to clipboard");
                }

                if (GUILayout.Button("👁", GUILayout.Width(25)))
                {
                    EditorUtility.DisplayDialog(alias, value, "OK");
                }

                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Secret? ",
                        $"Delete '{alias}'?", "Delete", "Cancel"))
                    {
                        keystore.RemoveSecret(alias);
                        keystore.Save(password);
                    }
                }
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();

        // Add new secret
        GUILayout.Space(15);
        GUILayout.Label("Add New Secret", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Alias:", GUILayout.Width(80));
        newAlias = EditorGUILayout.TextField(newAlias);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Value:", GUILayout.Width(80));
        newValue = EditorGUILayout.TextField(newValue);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newAlias) || string.IsNullOrEmpty(newValue));
        if (GUILayout.Button("➕ Add Secret", GUILayout.Height(30)))
        {
            // Need password to save
            string savePassword = EditorInputDialog.Show("Enter Keystore Password", "Password:", true);
            if (!string.IsNullOrEmpty(savePassword))
            {
                try
                {
                    keystore.SetSecret(newAlias, newValue);
                    keystore.Save(savePassword);
                    newAlias = "";
                    newValue = "";
                    Debug.Log("✅ Secret added!");
                }
                catch
                {
                    EditorUtility.DisplayDialog("Error", "Invalid password!", "OK");
                }
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(5);
        EditorGUILayout.EndVertical();
    }

    private string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 4)
            return "••••••••";
        return value.Substring(0, 4) + "••••••••";
    }

    private void ClearFields()
    {
        password = "";
        confirmPassword = "";
        newAlias = "";
        newValue = "";
    }

    private void GenerateKeystorePasswordScript()
    {
        string password = GenerateRandomPassword(16);
        string scriptPath = "Assets/Resources/KeystorePassword.Local.cs";

        string scriptContent =
$@"// This file is gitignored and contains the actual keystore password.
// DO NOT COMMIT THIS FILE TO VERSION CONTROL.

namespace BumiMobile
{{
    public static partial class KeystorePassword
    {{
        static partial void SetPassword()
        {{
            _password = ""{password}"";
        }}
    }}
}}";

        try
        {
            System.IO.File.WriteAllText(scriptPath, scriptContent);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"KeystorePassword.Local.cs generated successfully!\n\nPassword: {password}\n\nThis file is gitignored and will not be committed.",
                "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to generate KeystorePassword.Local.cs:\n{e.Message}", "OK");
        }
    }

    private string GenerateRandomPassword(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
        var random = new System.Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private void OnDestroy()
    {
        // Security: clear everything when closing
        keystore?.Lock();
        ClearFields();
    }
}

// Simple input dialog helper
public class EditorInputDialog : EditorWindow
{
    private string value = "";
    private string message;
    private bool isPassword;
    private bool didConfirm;
    private static EditorInputDialog window;

    public static string Show(string title, string message, bool isPassword = false)
    {
        window = GetWindow<EditorInputDialog>(true, title, true);
        window.message = message;
        window.isPassword = isPassword;
        window.minSize = new Vector2(300, 100);
        window.maxSize = new Vector2(300, 100);
        window.ShowModalUtility();
        return window.didConfirm ? window.value : null;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label(message);

        GUI.SetNextControlName("InputField");
        value = isPassword ? EditorGUILayout.PasswordField(value) : EditorGUILayout.TextField(value);
        EditorGUI.FocusTextInControl("InputField");

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel"))
        {
            didConfirm = false;
            Close();
        }
        if (GUILayout.Button("OK"))
        {
            didConfirm = true;
            Close();
        }
        EditorGUILayout.EndHorizontal();

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
        {
            didConfirm = true;
            Close();
        }
    }
}
#endif
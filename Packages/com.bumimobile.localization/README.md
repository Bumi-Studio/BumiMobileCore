# Bumi Mobile Localization

A comprehensive localization system for Unity projects, providing multi-language support with CSV-based translation management and runtime language switching.

## Features

- **Multi-Language Support**: Built-in support for 22 languages including English, Indonesian, Chinese (Simplified & Traditional), Japanese, Korean, Arabic, and more
- **CSV-Based Translation Management**: Easy translation management using CSV spreadsheets
- **Runtime Language Switching**: Change languages at runtime with event notifications
- **Arabic Text Support**: Integrated Arabic text shaping and rendering support
- **Unity UI Integration**: Components for automatic text localization in Unity UI and TextMeshPro
- **Editor Tools**: Custom editors and utilities for streamlined workflow
- **Fallback System**: Automatic fallback to English when translations are missing

## Installation

This package is designed to be installed via Unity Package Manager as a local package or from a Git repository.

### Dependencies

- `com.bumimobile.core` (v0.1.1 or higher)
- `com.unity.textmeshpro` (v3.0.9)
- `com.unity.editorcoroutines` (v1.0.0)
- `com.unity.nuget.newtonsoft-json` (v3.2.1)

### Requirements

- Unity 2021.3 or higher

## Quick Start

### 1. Create Localization Settings

Create a `LocalizationSettings` asset in your project:
1. Right-click in the Project window
2. Navigate to the creation menu for localization settings
3. Configure your CSV spreadsheet references

### 2. Prepare Translation Spreadsheets

Create CSV files with the following format:

```csv
Key,English,Indonesian,Japanese,...
welcome_message,Welcome!,Selamat datang!,ようこそ！,...
start_button,Start,Mulai,スタート,...
```

- First column: Unique translation keys
- First row: Language names (must match `LanguageType` enum values)
- Subsequent rows: Translations for each key

### 3. Initialize Localization

```csharp
using BumiMobile;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private LocalizationSettings localizationSettings;
    
    void Start()
    {
        LocalizationController.Init(localizationSettings);
        LocalizationController.Read();
    }
}
```

### 4. Use Localization in Code

```csharp
// Simple localization
string welcomeText = LocalizationController.Localize("welcome_message");

// Localization with parameters
string greetingText = LocalizationController.Localize("greeting", userName);

// Change language at runtime
LocalizationController.Language = LanguageType.Indonesian;

// Listen to language changes
LocalizationController.OnLocalizationChanged += OnLanguageChanged;

void OnLanguageChanged()
{
    Debug.Log("Language changed to: " + LocalizationController.Language);
}
```

### 5. Use UI Components

#### For Unity UI Text
Add the `LocalizedText` component to a Text GameObject and set the localization key.

#### For TextMeshPro
Add the `LocalizedTextMeshPro` component to a TextMeshProUGUI GameObject and set the localization key.

#### For Dropdowns
Use `LocalizedDropdown` or `LocalizeDropdownTextMeshPro` components for localized dropdown menus.

## Supported Languages

The following languages are supported through the `LanguageType` enum:

| Language | Enum Value | Code |
|----------|-----------|------|
| English | `LanguageType.English` | 0 |
| Indonesian | `LanguageType.Indonesian` | 1 |
| Mandarin (Simplified) | `LanguageType.MandarinSimplified` | 2 |
| Mandarin (Traditional) | `LanguageType.MandarinTraditional` | 3 |
| Japanese | `LanguageType.Japanese` | 4 |
| Korean | `LanguageType.Korean` | 5 |
| Italian | `LanguageType.Italian` | 6 |
| Spanish | `LanguageType.Spanish` | 7 |
| French | `LanguageType.French` | 8 |
| German | `LanguageType.German` | 9 |
| Vietnamese | `LanguageType.Vietnamese` | 10 |
| Portuguese | `LanguageType.Portuguese` | 11 |
| Filipino | `LanguageType.Filipino` | 12 |
| Arab | `LanguageType.Arab` | 13 |
| Tamil | `LanguageType.Tamil` | 14 |
| Russian | `LanguageType.Russian` | 15 |
| Thai | `LanguageType.Thai` | 16 |
| Dutch | `LanguageType.Dutch` | 17 |
| Hindi | `LanguageType.Hindi` | 18 |
| Turkish | `LanguageType.Turkish` | 19 |
| Persian | `LanguageType.Persian` | 20 |
| Kurdish | `LanguageType.Kurdish` | 21 |

## Advanced Features

### Arabic Text Support

The package includes built-in Arabic text shaping support via the `ArabicSupport.cs` utility. Arabic text is automatically processed when the language is set to `LanguageType.Arab`:

```csharp
// Automatically applies Arabic text shaping when language is Arab
string arabicText = LocalizationController.Localize("arabic_key");
```

### Checking Key Existence

```csharp
if (LocalizationController.HasKey("some_key"))
{
    string text = LocalizationController.Localize("some_key");
}
```

### Localization Sync

Use the `LocalizationSync` component to synchronize localization data with external sources or services.

## API Reference

### LocalizationController

Main static class for managing localization.

#### Properties
- `Language` - Get or set the current language
- `Dictionary` - Access to the full translation dictionary
- `OnLocalizationChanged` - Event fired when language changes

#### Methods
- `Init(LocalizationSettings settings)` - Initialize with settings
- `Read()` - Load translation data from CSV files
- `Localize(string key)` - Get localized string for a key
- `Localize(string key, params object[] args)` - Get formatted localized string
- `HasKey(string key)` - Check if translation key exists
- `AutoLanguage()` - Set language to default (English)
- `FixIfArabic(string text)` - Apply Arabic text shaping if needed

### Components

- **LocalizedText** - Localizes Unity UI Text components
- **LocalizedTextMeshPro** - Localizes TextMeshPro components
- **LocalizedDropdown** - Localizes Unity UI Dropdown components
- **LocalizeDropdownTextMeshPro** - Localizes TextMeshPro Dropdown components
- **LocalizationSync** - Synchronizes localization data
- **LocalizationInitModule** - Initialization module for automatic setup

## Best Practices

1. **Use Consistent Key Naming**: Use descriptive, hierarchical keys (e.g., `ui.menu.start_button`)
2. **Always Provide English**: English is used as the fallback language
3. **Avoid Empty Translations**: Empty cells in CSV will trigger fallback to English
4. **Test Language Switching**: Always test your UI with different languages to ensure proper layout
5. **Handle Missing Keys**: Use `HasKey()` to check for key existence before localizing
6. **Parameter Formatting**: Use standard C# string formatting syntax for parameterized translations

## Troubleshooting

### "Translation not found" Warning
- Verify the key exists in your CSV file
- Check that the CSV file is properly referenced in LocalizationSettings
- Ensure the CSV format is correct (no duplicate keys)

### "Duplicated key" Error
- Each translation key must be unique across all CSV files
- Check for duplicate entries in your spreadsheets

### "Language not found" Error
- Ensure language name in CSV header matches `LanguageType` enum values
- Check for typos in language names

### Arabic Text Not Displaying Correctly
- Verify that TextMeshPro is properly configured for Arabic fonts
- Ensure the font asset supports Arabic glyphs
- The package automatically applies Arabic text shaping when language is set to Arab

## Changelog

See [CHANGELOG.md](./CHANGELOG.md) for version history and updates.

## License

This package is part of the Bumi Mobile framework.

## Support

For issues, questions, or feature requests, please contact the Bumi Studio development team.

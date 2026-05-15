# Bumi Mobile Feedback

User feedback, rating prompts, and survey orchestration for Bumi Mobile projects. This package provides a unified API for collecting user feedback, prompting app store ratings, and managing in-app surveys so individual games only wire the high-level settings.

## Features

- **Feedback prompt orchestration**: Configurable prompts for collecting user sentiment and suggestions within the app.
- **App store rating integration**: Native rating dialog triggers gated behind configurable conditions (session count, time elapsed, positive sentiment threshold).
- **Survey management**: Plugin-based survey providers with support for in-app questionnaires and external survey links.
- **Initializer module** (`FeedbackInitModule`) that plugs into the Core startup pipeline for automatic bootstrapping.
- **Persistent state**: Rating request cooldowns and prompt timing tracked across sessions via the Save module when `MODULE_SAVE` is present.

## Requirements

- Unity **2021.3** or newer.
- `com.bumimobile.core` 0.1.1+ for the initializer, module registry, and helper utilities.

## Getting Started

1. **Create Feedback Settings**

   - `Assets ▸ Create ▸ Data ▸ Core ▸ Feedback Settings` generates the main ScriptableObject configuration. Configure rating conditions, survey providers, and feedback prompt text.

2. **Wire the initializer**

   - Add a `FeedbackInitModule` asset to your `ProjectInitSettings` (via the Core initializer UI). Assign the Feedback Settings asset.
   - During startup the module initializes feedback providers and restores persistent state.

3. **Optional: manual bootstrap**

```csharp
[SerializeField] private FeedbackSettings feedbackSettings;

IEnumerator Start()
{
    Feedback.Init(feedbackSettings);
    yield return null;
}
```

## Runtime Usage

- **Rating prompts**

```csharp
// Show the native rating dialog (gated by configured conditions)
Feedback.RequestRating();

// Check if conditions are met before prompting
if (Feedback.CanRequestRating)
{
    Feedback.RequestRating();
}

// Subscribe to rating events
Feedback.RatingRequested += OnRatingRequested;
Feedback.RatingCompleted += (accepted) => Debug.Log($"User rated: {accepted}");
```

- **Surveys**

```csharp
// Launch an in-app survey by identifier
Feedback.ShowSurvey("onboarding_survey");

// Open an external survey URL
Feedback.OpenSurveyUrl("https://forms.example.com/survey");

// Subscribe to survey events
Feedback.SurveyCompleted += (surveyId) => Debug.Log($"Survey {surveyId} completed");
```

- **General feedback**

```csharp
// Open a configurable feedback form
Feedback.ShowFeedbackForm();

// Submit feedback programmatically
Feedback.SubmitFeedback("Love the game!", FeedbackSentiment.Positive);
```

## Troubleshooting

- If rating prompts never appear, verify the configured conditions (session count, time elapsed, sentiment threshold) are being met at runtime.
- For native rating dialogs on iOS, ensure the app is distributed through the App Store—the SKStoreReviewController API does not function in development builds without special configuration.
- Use the verbose logging flag on the Feedback Settings asset to surface detailed prompt evaluation events in the console.

## License

See the root `LICENSE` file for licensing details.

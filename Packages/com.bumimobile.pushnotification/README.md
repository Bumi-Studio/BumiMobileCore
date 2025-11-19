# Bumi Mobile Push Notification

This package provides a simple, data-driven push notification solution for Bumi Mobile projects. It pairs ScriptableObject templates with a flexible scheduler so designers can configure messaging without touching code.

## Features

- Predefined notification templates for retention, re-engagement, event announcements, and more.
- Standalone `NotificationTemplate` ScriptableObjects that encapsulate message copy, media, targeting rules, and triggers per notification.
- `NotificationTemplateCatalog` ScriptableObject that references the templates you create and exposes them to the scheduler.
- `NotificationSettings` ScriptableObject that binds a catalog to the initialization pipeline and optionally auto-schedules.
- `NotificationManager` runtime scheduler that enforces cooldowns, trigger constraints, and platform-specific delivery.

## Requirements

- Unity 2021.3 or newer.
- `com.bumimobile.core` 0.1.1 or later.
- Unity "Mobile Notifications" package 2.4.1 or later.

## Getting Started

1. **Create notification templates**

   - Choose `Create ▸ BumiMobile ▸ Push Notifications ▸ Notification Template` for each push you want to configure.
   - Save the assets anywhere in your project (for example, under `Assets/Notifications/Templates`).
   - Customize every template's message, media, rules, and trigger settings.

2. **Create a notification catalog**

   - In the Project view, choose `Create ▸ BumiMobile ▸ Push Notifications ▸ Notification Catalog`.
   - Save the asset inside a `Resources` folder (for example, `Assets/Resources/NotificationTemplateCatalog.asset`).
   - Add the templates you created to the catalog list. Remove null entries to keep the list tidy.
   - If no catalog is present (or it is empty), the runtime falls back to in-memory defaults that mirror the sample templates.

3. **Create notification settings**

   - Choose `Create ▸ BumiMobile ▸ Push Notifications ▸ Notification Settings`.
   - Assign the catalog asset to the `Template Catalog` field.
   - Enable `Schedule On Init` if you want automatic scheduling after permissions are granted. Optionally enable `Schedule In Editor` to allow scheduling when running in the editor.

4. **Hook into the init pipeline**

   - Open your project initialization settings (typically under `Assets/Resources/Core/ProjectInitSettings` or via the Init Module inspector).
   - Locate the `Notification Module` entry and assign the `NotificationSettings` asset to the module's `Settings` field.

5. **Verify Android channel registration**

   - On Android, `NotificationInitModule` registers a default notification channel (`default_channel`) after permissions are granted. Adjust the channel data inside `NotificationInitModule.cs` if you need a different channel ID, name, or importance.

6. **Optional: Programmatic scheduling**
   - You can call `NotificationManager.ScheduleAllNotifications(new NotificationScheduleContext { ... })` manually if you need to run scheduling at custom times or with a populated context.
   - Use `NotificationManager.RegisterCatalog(customCatalog)` if you prefer to load the catalog from a non-Resources location at runtime.

## Extending Templates

- **Add new notification types:** Extend the `NotificationType` enum and update `NotificationTemplateDefaults.CreateRuntimeTemplates` inside `NotificationTemplate.cs` if you need new fallback defaults.
- **Custom triggers:** Introduce new values in `NotificationTriggerType` and adjust `NotificationManager.DetermineSchedule` with the scheduling logic.
- **Per-project customization:** Duplicate the built-in defaults by creating new `NotificationTemplate` assets (rules, messages, media) and adding them to your catalog.

## Troubleshooting

- **No catalog assigned:** If `NotificationSettings` has an empty catalog reference, the package falls back to an in-memory default catalog and logs a warning when `Log Missing Catalog Warning` is enabled.
- **Notifications not scheduling:** Ensure `Schedule On Init` is enabled or call `NotificationManager.ScheduleAllNotifications` manually. Double-check that the `NotificationScheduleContext` used has the necessary properties populated (e.g., `LastLogin`, `LevelStuckSince`).
- **Android channel conflicts:** Update the channel ID in `NotificationInitModule.RequestNotificationPermission` to avoid collisions with other packages.

## License

This package is licensed under the terms described in the repository's `LICENSE` file.

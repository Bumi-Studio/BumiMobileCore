# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

## [0.3.0] - 2026-06-03

### Added

- **Bumi Core Notification Rules v2.0**: Full rewrite of scheduling logic to enforce all 20 global rules.
  - Max 1 push per player per day
  - 4-hour cooldown between any notifications
  - Skip if player is currently active
  - Player local time with server fallback
  - Priority-based filtering (highest priority wins when multiple qualify)
  - Copy variation deduplication (random pick, skip rest)
  - Idempotency key system to prevent duplicate sends
  - Per-category scheduling: H+1/H+2, H+3/H+7, 10:00/11:00/12:00/15:00/20:00 delivery slots
- **NotificationPriority enum**: 1=Feature Announcement (highest) → 6=Daily Retention (lowest)
- **Identity & Tracking fields**: `copyVariationId` and `relatedEntityId` for copy variation handling and idempotency
- **New trigger fields**: `requiresCompletedLevel`, `gameOpenBeforeHour`, `gameOpenBeforeMinute`, `loginStreakDays`
- **Expanded NotificationScheduleContext**: `PlayerId`, `PlayerLocalTime`, `HasEverCompletedLevel`, `LastGameOpenTime`, `IsPlayerActive`, `LevelsCompletedToday`, `LevelsStartedToday`, `LoginStreakDays`, `UnclaimedRewards`, `FeatureReleaseDate`, `LeaderboardRankBeforeReset`, `LeaderboardResetTime`
- **Localization support**: `titleKey`, `bodyKey`, `callToActionKey` on `NotificationMessage` with runtime resolution via `BumiMobile.LocalizationController`
- **Custom Editor (`NotificationTemplateEditor`)**:
  - Colored type header (blue, orange, green, yellow, purple, pink)
  - Dynamic field visibility — only shows trigger fields relevant to the selected notification type
  - "Show All Fields" toggle for advanced editing
  - Contextual help boxes per type summarizing the rules
  - Auto-applies built-in defaults when the notification type is changed
  - Organized sections: Identity & Tracking, Rules, Trigger Settings, Message, Media Assets
  - Hour and minute fields use `IntSlider` (0–23 for hours, 0–59 for minutes)
- **Editor assembly definition**: `BumiMobile.PushNotification.Editor.asmdef`

### Changed

- Updated all 6 built-in template defaults to match the new Bumi Core notification rules
- `NotificationTemplateDefaults` is now `public` (was `internal`) for editor access
- `BumiMobile.PushNotification` runtime asmdef now references `BumiMobile.Localization`
- `DetermineSchedule` now uses `template.type` instead of `triggerType` for routing
- `DeduplicateNotifications` now merges copy variations instead of rescheduling them

### Fixed

- `HasAnyNotificationToday` was comparing `GetPlayerDate(context) == today` inside the lambda instead of the history item's date, causing it to always return `true` after the first notification was ever sent. This blocked all future notifications permanently. Fixed to compare `h.Date == today.Date`.

### Removed

- `RegisterLocalizationResolver(Func<string, string>)` — localization now uses `BumiMobile.LocalizationController` directly

## [0.2.3] - 2025-12-08

### Added

- **Notification Deduplication System**: Automatically handles multiple notifications of the same type scheduled at the same time by randomly selecting one to display and rescheduling others for future days.
  - Notifications within 5 minutes are considered duplicates
  - Notifications 4+ hours apart are not deduplicated
  - Duplicates are rescheduled with count-based day offsets (+1, +2, +3 days, etc.)
  - Random selection ensures fair distribution across app sessions
- New `enableDeduplication` configuration option in `NotificationSettings` to toggle the feature
- Public API methods `SetDeduplicationEnabled()` and `IsDeduplicationEnabled()` for runtime control
- Detailed logging of deduplication actions for debugging

### Changed

- `ScheduleAllNotifications` now collects all candidates before scheduling to enable deduplication processing


## [0.2.2] - 2025-11-20

### Added

- Templates created in the editor now auto-fill with the built-in defaults for their selected notification type and include an inspector action to reapply those defaults on demand.

## [0.2.1] - 2025-11-19

### Changed

- Scheduler now clears existing platform notifications before queuing new ones to prevent duplicate deliveries.

## [0.2.0] - 2025-11-19

### Added

- Notification template catalog ScriptableObject populated with predefined engagement, retention, and announcement push templates.
- Notification settings asset that assigns a catalog, controls auto-scheduling, and warns when defaults are used.
- Standalone notification template ScriptableObject for authoring copy, rules, triggers, and media per notification type.

### Changed

- Notification manager now loads templates from the catalog and schedules notifications using rule-driven contexts instead of hard-coded examples.
- Catalogs now reference external notification template assets, with runtime fallbacks generating defaults when no catalog (or entries) are present.
- Removed the `templateLabel` field; asset names now drive display labels for templates.

## [0.1.0] - 2025-11-14

### Added

- Initial release of the PushNotification package.

# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

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

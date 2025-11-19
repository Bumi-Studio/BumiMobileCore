# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

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

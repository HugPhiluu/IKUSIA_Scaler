# Changelog

All notable changes to this package are documented in this file.

## [0.0.4] - 2026-08-10

### Fixed
- Fixed a regression where some auto-converted dropped outfits could be processed twice and snap armature scale back unexpectedly (for example, back to 1.0).

## [0.0.3] - 2026-08-10

### Added
- Updated armature conversion scaling to use the destination avatar armature scale as a baseline before applying profile multipliers, so resized avatars convert outfits more accurately.

### Fixed
- Improved auto-detection for dropped outfit prefabs by restricting source-avatar matching to the prefab's immediate containing folder.

## [0.0.2] - 2026-08-06

### Added
- Automatic conversion detection flow for dropped outfit prefabs, including source/target avatar matching and profile lookup.
- New settings window options for Automatic Conversion and auto-detection Console trace logs.
- First-time auto-conversion discovery prompt with one-click enable action.
- Reset All Settings action to clear IKUSIA Scaler preferences and onboarding state.

### Changed
- Reworked language/settings UX from popup-style flow to a dedicated settings window layout.
- Improved avatar detection candidates by including prefab source path segments and file names.
- Conversion execution now supports non-dialog auto mode, with safer preflight behavior for automatic runs.

## [0.0.1] - 2026-08-05

### Added
- Initial release of IKUSIA Scaler
- Right-click context menu under GameObject/IKUSIA Scaler
- Conversion support for Mizuki, Rurune, and Kaguya outfit scaling
- Unity Undo support and basic safety warnings

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.7-experimental.4] - 2025-12-23

### Added

- PlayStation button icon set and selector (Controls tab).
- New UI sprite variants: `UI.Xbox.png` and `UI.PlayStation.png`.
- Button icon set config option with a restart notice.

### Changed

- Button icon selection now swaps the UI sprite sheet on disk (restart required).
- Updated attributions for PlayStation button icons.

---

## [1.1.6] - 2025-12-22

### Summary

- Consolidated release with all changes from prior CustomisationPlus builds.

### Added

- Separate thumbstick navigation options for the item wheel and mod wheel.
- Added a "Both" thumbstick option for wheel navigation.
- Backward-compatible binding so older config layouts still show a navigation selector.
- Customization controls for item name/description size and visibility.
- Sliders for vertical and horizontal radial menu position.
- Toggle and size slider for quick action menus.
- Scrollable config menu pages with visible scrollbars.

### Changed

- Reduced radial menu size and lowered on-screen position for a less intrusive UI.
- Reduced quick action menu sizes to match main radial menus.
- Removed full-screen background fade when radial menus are opened.
- Improved radial menu preview layout and centering.
- Sharper item name rendering at small scales.
- Updated default configuration values to match the new UI tuning.
- Config menu now centers on most tabs and slides left on the Style tab to make room for the preview.
- Config menu slide animation smoothed for better readability.
- Preserves vanilla health/stamina/time HUD while hiding only the toolbar.
- Instant actions now support held tool use for continuous action.
- Improved controller cursor behavior after closing a wheel (suppresses flicker and unwanted movement).
- Reset mouse position to the player when opening/closing the wheel.
- Refined config menu layout to fit better on smaller screens.

### Fixed

- In-game config menu scaling on smaller screens (incl. 1280x800).
- Preview rendering at sub-1.0 text scale.
- Truncated item names in the radial menu center label.
- Slingshot (and modded bows) assignment to quick actions.
- Right-stick scrolling now works across all config tabs without cursor snapping.
- Cursor stays hidden when opening/closing radial menus to prevent pop-in.
- Hot reload source sync no longer crashes when source paths are unavailable.
- GMCM keybinding sync now skips cleanly when the extension DLL is missing.
- Config menu position flicker when opening the menu.
- Fixed radial menu preview not showing properly in the Style tab of in-game config menu.

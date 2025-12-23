# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.6-prerelease] - 2025-12-22

### Changed

- Instant actions now support held tool use for continuous action

### Fixed

- Right-stick scrolling in the config menu now works across all tabs without cursor snapping
- Cursor stays hidden while opening/closing radial menus to prevent pop-in
- GMCM keybinding sync now skips cleanly when the extension DLL is missing
- Hot reload source sync no longer crashes when source paths are unavailable

---

## [1.1.5] - 2025-12-22

### Changed

- Slowed and smoothed the config menu slide animation when entering/leaving the Style tab

---

## [1.1.4] - 2025-12-22

### Changed

- Config menu now centers on most tabs and slides left on the Style tab to make room for the preview

### Fixed

- Config menu position flicker when opening the menu

---

## [1.1.3] - 2025-12-22

### Added

- Separate thumbstick navigation options for the item wheel and mod wheel
- Added a "Both" thumbstick option for wheel navigation
- Backward-compatible binding so older config layouts still show a navigation selector

### Changed

- Improved controller cursor behavior after closing a wheel (suppresses flicker and unwanted movement)
- Reset mouse position to the player when opening/closing the wheel

---

## [1.1.2] - 2025-12-22

### Added

- Customization controls for item name/description size and visibility
- Sliders for vertical and horizontal radial menu position
- Toggle and size slider for quick action menus
- Visible scrollbars in the config menu pages

### Changed

- Improved radial menu preview layout and centering
- Sharper item name rendering at small scales
- Updated default configuration values to match the new UI tuning
- Refined config menu layout to fit better on smaller screens

### Fixed

- Preview rendering at sub-1.0 text scale
- Truncated item names in the radial menu center label
- Vanilla HUD display to keep health, stamina, and top-right UI visible while only hiding the toolbar

---

## [1.1.1] - 2025-12-21

### Added

- Added scrollable menus for config menu on smaller screens

### Fixed

- Fixed in-game config menu scaling issues on smaller screens (should now display correctly on 1280 x 800 displays)
- Fixed radial menu preview not showing properly in the Style tab of in-game config menu

---
## [1.1.0] - 2025-12-21

### Changed

- Reduced radial menu size for a less intrusive UI
- Lowered position of radial menus on screen
- Reduced quick action menu sizes to match main radial menus
- Removed full-screen background fade when radial menus are opened

### Fixed

- Fixed slingshot and modded bow quick action assignment that was present in original mod

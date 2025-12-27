# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.10-prerelease.8] - 2025-12-25

### Fixed

- Defaults now keeps the wheel at the bottom after reset.

---

## [1.1.10-prerelease.7] - 2025-12-25

### Fixed

- Defaults now keep the wheel at the expected vertical position after the offset inversion.

---

## [1.1.10-prerelease.6] - 2025-12-25

### Changed

- Removed cursor repositioning (kept cursor hiding behavior).

---

## [1.1.10-prerelease.5] - 2025-12-25

### Added

- Experimental: optional mouse/trackpad navigation for radial wheels (selection + click activation).
- Mouse wheel rotation feel refined to reduce direction “bounce.”
- Added hybrid mouse mode: small moves rotate, larger moves snap to direction.
- Increased mouse responsiveness for both rotate and snap modes.
- Switched to absolute-direction mouse selection for more direct swipes.
- Increased small-trackpad sensitivity for tiny circular motions.
- Pushed absolute-direction sensitivity even lower for ultra-small trackpads.
- Vertical position slider range increased to ±50% and direction inverted.
- Added default Mod Menu items (Main Menu, Map, Journal, Mailbox, Crafting).
- Added one-time migration to preserve vertical offset positions after direction flip.
- Fixed vertical position label showing “-0%” at zero.

### Changed

- Reposition overlay: heading moved to the top, help text moved below Save/Cancel, and crosshair lines dimmed.

---

## [1.1.9-hotfix] - 2025-12-25

### Changed

- Hotfix: rebuilt against Stardew Valley 1.6.15 assemblies so the mod loads on the stable game version.

---

## [1.1.9] - 2025-12-24

### Changed

- Improved compatibility with Stardew Valley 1.6.16 alpha by avoiding a removed scythe check.
- After closing a wheel, your tools now follow your facing direction instead of an old mouse target.
- Mouse recentering now aims at the tile in front of your character to reduce accidental turn‑and‑swing.

---

## [1.1.8] - 2025-12-23

### Changed

- Removed legacy cursor patches that no longer exist in current game versions (less log spam).

---

## [1.1.7] - 2025-12-23

### Added

- PlayStation button prompt set, with a selector on the Controls tab.
- An Instant Actions entry in the mod wheel, plus a visibility toggle.
- PlayStation prompt sprite sheet + mapping data.

### Changed

- Instant Actions (remapping) menu fits smaller screens better; the middle list now scrolls with a visible bar.
- Switching prompt sets reloads UI sprites (close and reopen the menu if icons don’t update).
- Control prompts and the mod‑menu keybind editor follow the selected prompt set.
- Quick action slots/icons/prompts are larger while the menu size stays the same.

### Fixed

- Mouse/trackpad now re‑shows the cursor correctly after closing a wheel.
- Defaults/Cancel/Save tooltips removed to prevent overlap on small screens.

---

## [1.1.6] - 2025-12-22

### Summary

- Consolidated release with all CustomisationPlus changes to date.

### Added

- Separate thumbstick options for the item wheel vs. mod wheel.
- A “Both” thumbstick option for wheel navigation.
- Backward‑compatible binding so older configs still show a navigation selector.
- Item name/description visibility toggles + size sliders.
- Vertical + horizontal radial menu position sliders.
- Quick action menu visibility toggle + size slider.
- Scrollable config pages with visible scrollbars.

### Changed

- Radial menus are smaller and lower on screen so they’re less intrusive.
- Quick action menus are resized to match the main wheel.
- Full‑screen background fade removed when wheels open.
- Radial menu preview layout and centering improved.
- Small‑scale item name text looks sharper.
- Default config values updated to match the new UI tuning.
- Config menu centers on most tabs and slides left on the Style tab to make room for the preview.
- Config menu slide animation smoothed for readability.
- Health/stamina/time HUD stays visible; only the toolbar hides.
- Instant Actions now support holding the button for continuous use.
- Controller cursor behavior improved after closing a wheel (less flicker/movement).
- Mouse position resets to the player when opening/closing a wheel.
- Config menu layout refined to fit smaller screens.

### Fixed

- Config menu fits on smaller screens (incl. 1280×800).
- Preview renders correctly at text scales below 1.0.
- Center‑label item names no longer get cut off.
- Slingshot (and modded bows) can be assigned to quick actions.
- Right‑stick scrolling works across all config tabs without snapping to tabs.
- Cursor stays hidden when opening/closing wheels to prevent pop‑in.
- Hot‑reload source sync no longer crashes when source paths are unavailable.
- GMCM keybinding sync now skips cleanly when the extension DLL is missing.
- Config menu position flicker on open.
- Radial menu preview now shows properly on the Style tab.

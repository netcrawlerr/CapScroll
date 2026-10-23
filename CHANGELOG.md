# Changelog

All notable changes to CapScroll are documented in this file.

## [1.3.0] - 2026-10-23

### Added

- **Native XDG Desktop Portal Integration:**
  - Added direct D-Bus communication with `org.freedesktop.portal.Screenshot` using `Tmds.DBus`.
  - Added portal-based region and scrolling screenshot capture
- **Wayland Remote Desktop Input:**
  - Added integration with `org.freedesktop.portal.RemoteDesktop` for automated mouse-wheel scrolling input synthesis.

### Changed

- **Wayland Screenshot Pipeline:**
  - Replaced desktop-specific screenshot command integrations with native C# XDG Desktop Portal communication.
  - Removed reliance on `gnome-screenshot`, `spectacle`, `cosmic-screenshot`, and `grim` from the new portal-based screenshot pipeline.
  - Standardized screenshot capture around the interfaces provided by the active desktop portal backend.
- **Automated Scrolling:**
  - Updated Wayland input handling to use the RemoteDesktop portal when the required capabilities and permissions are available.
  - Added runtime scroll-step adjustments to accommodate differences in scrolling behavior between desktop environments, including KDE Plasma and GNOME.
  

## [1.2.1] - 2026-10-09

### Added

- **Pop!_OS (COSMIC) Wayland Support:**
  - Added support for screenshot capture on Pop!_OS running COSMIC on Wayland.
- **GNOME Desktop Detection:**
  - Added `PlatformDetector.IsGnome()` to identify GNOME desktop environments across X11 and Wayland sessions.

### Changed

- **Wayland Input Synthesis:**
  - Replaced `ydotool` button press emulation (`click 0xC5`) with mouse-wheel axis movements (`mousemove --wheel -y -1`).
  - Prevented text highlighting, element dragging, and accidental UI button selection during Wayland scrolling loops.
- **Shutter Audio Handling:**
  - Prevented duplicate audio triggers caused by GNOME's native capture server.

## [1.2.0] - 2026-09-25

### Added

- **Multi-Monitor Support:**
  - Added support for multi-display desktop setups.
  - Added primary monitor layout detection so the instruction text overlay (`"Drag to select an area • Esc to cancel"`) automatically centers at the top of the **primary screen**, regardless of virtual display coordinates.

### Changed

- Updated overlay window initialization logic to query `Screens.All` and span across all connected screens.

---

## [1.1.0] - 2026-09-17

### Added

- Wayland screenshot capture support.
- GNOME Wayland support.
- KDE Plasma Wayland support.
- Automatic desktop-environment detection for Wayland capture.
- GNOME screenshot capture through GNOME Shell D-Bus.
- GNOME screenshot fallback through `gnome-screenshot`.
- KDE Plasma screenshot capture through `spectacle`.
- Generic Wayland screenshot fallback through `grim`.
- XDG Desktop Portal screenshot fallback.
- Wayland input support through `ydotool`.
- Wayland scrolling screenshot capture with manual scrolling.
- Wayland frame capture, bottom-of-content detection, and frame stitching.
- Platform-specific capture handling for X11 and Wayland.
- Wayland capture backend availability detection.
- Automatic `ydotoold` startup when the ydotool socket is unavailable.
- Capture shutter sound support using the freedesktop sound event with `paplay` fallback.

### Changed

- Refactored scrolling capture to support multiple Linux display backends.
- Updated `CapScrollEngine` to handle both X11 and Wayland capture workflows.
- Selected screenshot capture methods according to the active Linux display environment.
- Preserved existing X11 automatic scrolling behavior.
- Improved platform-specific handling of capture and input simulation.
- Updated capture timing for Wayland applications to allow content to finish rendering after scrolling.
- Updated documentation and requirements to reflect Wayland support.

### Wayland Scrolling

Wayland scrolling capture works differently from X11.

- CapScroll captures the selected region before scrolling.
- The user manually scrolls the selected content.
- CapScroll captures subsequent frames while scrolling.
- CapScroll detects when the content stops changing.
- Captured frames are

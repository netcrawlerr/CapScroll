# Changelog

All notable changes to CapScroll are documented in this file.

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
- `CapScrollEngine` now handles both X11 and Wayland capture workflows.
- Screenshot capture is now selected according to the active Linux display environment.
- X11 automatic scrolling behavior remains unchanged.
- Improved platform-specific handling of capture and input simulation.
- Updated capture timing for Wayland applications to allow content to finish rendering after scrolling.
- Updated documentation and requirements to reflect Wayland support.

### Wayland Scrolling

Wayland scrolling capture works differently from X11.

- CapScroll captures the selected region before scrolling.
- The user manually scrolls the selected content.
- CapScroll captures subsequent frames while scrolling.
- CapScroll detects when the content stops changing.
- Captured frames are automatically aligned and stitched together.

Automatic mouse-wheel scrolling remains available on X11.

### Supported Desktop Environments

| Desktop Environment | X11       | Wayland   |
| ------------------- | --------- | --------- |
| GNOME               | Supported | Supported |
| KDE Plasma          | Supported | Supported |
| XFCE                | Supported | —         |

### Known Limitations

- Wayland scrolling capture requires the user to manually scroll the selected content.
- Wayland input automation is not currently used for scrolling capture.
- Wayland screenshot behavior depends on the screenshot facilities available in the desktop environment.
- Scrolling results can vary depending on application rendering behavior and scrolling characteristics.
- Windows and macOS are not currently supported.

## [1.0.0] - 2026-08-28

### Added

- Region screenshot capture.
- Scrolling screenshot capture.
- Automatic scrolling using X11 mouse wheel events.
- Automatic detection of the end of scrollable content.
- Dynamic overlap detection between consecutive captured frames.
- Vertical image stitching into a single continuous screenshot.
- Capture cancellation support.
- Stitching progress reporting.
- Linux/X11 platform detection.
- Native X11 screen capture through `libX11`.
- X11 input simulation through `libXtst`.
- Avalonia-based desktop user interface.
- Debian package build and installation scripts.

### Known Limitations

- Scrolling capture requires an X11 session.
- Wayland support was not yet implemented.
- Windows and macOS are not currently supported.
- Stitching quality can vary depending on application rendering behavior and scrolling characteristics.

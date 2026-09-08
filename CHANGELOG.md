# Changelog

All notable changes to CapScroll are documented in this file.

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

- Scrolling capture currently requires an X11 session.
- Wayland support is **PLANNED** yet not currently implemented.
- Windows and macOS are not currently supported.
- Stitching quality can vary depending on application rendering behavior and scrolling characteristics.

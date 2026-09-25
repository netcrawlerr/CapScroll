<div align="center">
<img src="src/CapScroll.Desktop/Assets/Images/logo.png" width="64" height="64" alt="CapScroll Logo" />
<h1>CapScroll</h1>
<p>A Linux scrolling screenshot tool built with .NET and Avalonia UI</p>

<p>
<a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET" /></a>
<a href="https://learn.microsoft.com/dotnet/csharp/"><img src="https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white" alt="C#" /></a>
<a href="https://avaloniaui.net/"><img src="https://img.shields.io/badge/Avalonia-12-8A2BE2" alt="Avalonia" /></a>
<a href="https://www.linux.org/"><img src="https://img.shields.io/badge/Linux-FCC624?logo=linux&logoColor=black" alt="Linux" /></a>
<a href="https://www.x.org/"><img src="https://img.shields.io/badge/X11-supported-blue" alt="X11" /></a>
<img src="https://img.shields.io/badge/Wayland-supported-blue" alt="Wayland" />
<a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="License" /></a>
</p>
</div>

CapScroll is a screenshot tool for Linux that makes it easy to capture screen regions and create long screenshots from browsers, terminals, documents, and other scrollable content.

CapScroll supports both **X11 and Wayland** sessions, as well as **multi-monitor desktop setups**.

> [!NOTE]
> Scrolling capture works differently depending on the display server. X11 supports automatic scrolling, while Wayland currently requires manual scrolling.

## Overview

CapScroll supports both regular screenshots and scrolling screenshots.

For scrolling captures, CapScroll:

- Spans the virtual bounding desktop across all connected monitors.
- Captures the selected region.
- Scrolls or waits for the selected content to move.
- Captures subsequent frames.
- Detects when the end of the content is reached.
- Detects the overlap between frames.
- Stitches the frames into a single image.

The goal is to make long screenshots as simple as selecting an area and letting CapScroll handle the rest.

## Demo

> [!NOTE]
> Watch CapScroll in action with a scrolling screenshot demonstration.

[**▶ Watch Demo**](https://drive.google.com/file/d/1aLgQzf764prA_aX1Y7y1hyhJgYGu5rjU/view?usp=sharing)

## Screenshots

<table>
<tr>
<td width="100%">
<img src="screenshots/1.png" alt="CapScroll main interface" style="width: 100%;" />
</td>
</tr>
<tr>
<td width="100%">
<img src="screenshots/2.png" alt="CapScroll Gallery" style="width: 100%;" />
</td>
</tr>
<tr>
<td width="100%">
<img src="screenshots/3.png" alt="CapScroll stitching" style="width: 100%;" />
</td>
</tr>
</table>

## Features

- Region capture
- **Multi-monitor support** 
- Scrolling capture
- X11 automatic scrolling
- Wayland manual scrolling
- Automatic bottom detection
- Automatic frame stitching
- Dynamic frame alignment
- Capture cancellation
- Capture progress
- Capture preview
- Open captured image
- Open capture folder
- PNG output
- Debian package support
- X11 and Wayland support

## Supported Content

CapScroll can be useful for capturing:

- Web pages
- PDF documents
- Terminal output
- Long application views
- Documents
- Chat conversations
- Any Other scrollable content

> [!NOTE]
> Results may vary depending on how the target application handles scrolling and renders its content.
## Display Server & Monitor Support

| Display Server | Region Capture | Scrolling Capture | Multi-Monitor | Scrolling Method |
| -------------- | -------------- | ----------------- | ------------- | ---------------- |
| X11            | Supported      | Supported         | Supported     | Automatic        |
| Wayland        | Supported      | Supported         | Supported     | Manual           |

### Desktop Environment Support

| Desktop Environment | X11       | Wayland   | Multi-Monitor |
| ------------------- | --------- | --------- | ------------- |
| GNOME               | Supported | Supported | Supported     |
| KDE Plasma          | Supported | Supported | Supported     |
| XFCE                | Supported | —         | Supported     |

## Requirements

### General

- Linux
- .NET 10
- Avalonia 12

### X11

X11 scrolling capture requires:

- X11 session
- `libX11`
- `libXtst`

### Wayland

Wayland screenshot capture uses the screenshot facilities available on the desktop environment.

Depending on the environment, CapScroll can use:

- GNOME Shell screenshot D-Bus API
- `gnome-screenshot`
- KDE `spectacle`
- `grim`
- XDG Desktop Portal

Wayland scrolling capture additionally uses:

- `ydotool`
- `ydotoold`

> [!NOTE]
> Wayland scrolling capture currently requires manually scrolling the selected content. `ydotool` is used for Wayland input support and infrastructure, but CapScroll does not currently perform automatic scrolling on Wayland.

## Installing Requirements

### Debian / Ubuntu / Linux Mint

Install the general dependencies:

```bash
sudo apt update

sudo apt install \
    dotnet-sdk-10.0 \
    libx11-6 \
    libx11-dev \
    libxtst6 \
    libxtst-dev
```

### Wayland Dependencies

For Wayland scrolling capture:

```bash
sudo apt install ydotool
```

For GNOME:

```bash
sudo apt install gnome-screenshot
```

For KDE Plasma:

```bash
sudo apt install spectacle
```

For the generic Wayland fallback:

```bash
sudo apt install grim
```

For the XDG Desktop Portal fallback:

```bash
sudo apt install \
    xdg-desktop-portal \
    xdg-desktop-portal-gtk
```

> [!TIP]
> You do not necessarily need every Wayland screenshot utility. CapScroll detects the desktop environment and attempts the appropriate screenshot method before falling back to other available methods.

### Install Everything

For a typical GNOME/KDE system where you want the available fallback methods installed:

```bash
sudo apt update

sudo apt install \
    dotnet-sdk-10.0 \
    libx11-6 \
    libx11-dev \
    libxtst6 \
    libxtst-dev \
    ydotool \
    gnome-screenshot \
    spectacle \
    grim \
    xdg-desktop-portal \
    xdg-desktop-portal-gtk
```

> [!NOTE]
> Some packages may already be installed on your system. `apt` will simply keep the existing packages.

## Currently Tested On

- Kali Linux
- Linux Mint
- Ubuntu
- GNOME
- KDE Plasma
- XFCE

Display server testing includes:

- X11 (GNOME, KDE, XFCE)
- GNOME Wayland
- KDE Plasma Wayland

## Installation

### Debian Package

Download the `.deb` package from the project's releases and install it with:

```bash
sudo apt install ./capscroll_<VERSION_NO>_amd64.deb
```

### Build From Source

Clone the repository:

```bash
git clone https://github.com/netcrawlerr/CapScroll.git
cd CapScroll
```

Run the build and installation scripts:

```bash
scripts/build.sh
scripts/install.sh
```

## How to Use

### Normal Capture

1. Start CapScroll.
2. Select **Capture Region**.
3. Select the area to capture.
4. CapScroll captures the selected region.
5. The image is saved automatically.

### Scrolling Capture — X11

1. Select **Scrolling Capture**.
2. Select the scrollable area.
3. CapScroll captures the initial view.
4. CapScroll automatically scrolls the content.
5. Additional frames are captured.
6. CapScroll detects when the bottom of the content is reached.
7. The frames are aligned and stitched together.
8. The completed long screenshot is displayed.

### Scrolling Capture — Wayland

1. Select **Scrolling Capture**.
2. Select the scrollable area.
3. CapScroll captures the initial view.
4. Scroll the selected content manually.
5. Continue scrolling at a relatively consistent speed.
6. CapScroll captures subsequent frames.
7. CapScroll detects when the content stops changing.
8. The frames are automatically aligned and stitched together.

> [!TIP]
> On Wayland, keep the scrolling speed reasonably consistent for better stitching results.

> [!NOTE]
> Wayland scrolling capture does not control the mouse wheel automatically. The user must scroll the selected content manually.

## Stop Scrolling Capture

Press:

```text
Ctrl + Shift + Esc
```

or move the mouse pointer outside the selected capture area.

> [!NOTE]
> Moving the pointer outside the selected capture area stops the scrolling capture because CapScroll uses the selected region and pointer position as part of its capture workflow.

## Output

Captured images are saved as PNG files.

By default, captures are stored in:

```text
~/Pictures/CapScroll/
```

## Current Limitations

- Wayland scrolling requires manual scrolling.
- Wayland screenshot behavior depends on the available desktop screenshot utilities.
- Scrolling quality can vary depending on application rendering behavior.
- Dynamically changing content may produce imperfect stitching.
- Windows is not supported.
- macOS is not supported.

## License

CapScroll is licensed under the [MIT License](LICENSE).

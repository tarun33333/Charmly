# Charmly ✨

**Handcrafted interactive desktop charms hanging from a physical cord on your Windows desktop.** Push, flick, or drag charms and watch them swing with the realistic weight, inertia, and settle of a real ornament.

Built and engineered for Windows on **.NET 9**, **WinUI 3**, **Win2D GPU Rendering**, and **SkiaSharp** by **Tarun**.

[![Release](https://img.shields.io/github/v/release/tarun33333/Charmly?style=flat-square&color=blue)](https://github.com/tarun33333/Charmly/releases)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011%20(x64)-informational?style=flat-square)](https://github.com/tarun33333/Charmly/releases)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)

[**⬇️ Download Charmly for Windows (v0.9.2)**](https://github.com/tarun33333/Charmly/releases)

- **`CharmlySetup.exe`** (Recommended standalone installer wizard with auto-startup)
- **`Charmly-v0.9.2-Windows-x64.zip`** (Portable zero-install package)

---

## 🚀 Installation

### Option 1: Standalone Installer (Recommended)
1. Download **`CharmlySetup.exe`** from [GitHub Releases](https://github.com/tarun33333/Charmly/releases/latest).
2. Run `CharmlySetup.exe` to open the setup wizard.
3. Review and accept the legal disclaimer and MIT license.
4. Keep **"Start Charmly automatically when Windows boots"** checked (selected by default).
5. Click **Install** — Charmly will install to `%LOCALAPPDATA%\Programs\Charmly`, create Desktop and Start Menu shortcuts, and register auto-start.
6. Click **Finish & Launch**!

### Option 2: Portable ZIP
1. Download **`Charmly-v0.9.2-Windows-x64.zip`** from [GitHub Releases](https://github.com/tarun33333/Charmly/releases/latest).
2. Extract the archive to any folder on your PC.
3. Double-click `Charmly.exe` to run.

---

## ✨ Features

- **161 Copyright-Free Charms Across 21 Collections**:
  - *Protection*: Nazar Evil Eye (🧿), Hamsa Hand (🪬), Nimbu-Mirchi (🍋🌶️), Dreamcatcher, Scarab, and more.
  - *Spirituality*: Sacred Om (🕉️), Lotus Bloom (🪷), Trishul, Ganesha, Buddha, and sacred emblems.
  - *Luck & Fortune*: Beckoning Cat (🐱), Four-Leaf Clover (🍀), Shrine Bell (🔔), Horseshoe, Coins.
  - *Superheroes & Vigilantes*: Web Slinger (🕷️), Star Shield (⭐), Arc Helmet, Thunder Mallet, Shadow Bat Crest, Solar Shield.
  - *Pocket Monsters*: Spark Mouse (⚡), Flame Lizard, Aqua Turtle, Capture Sphere.
  - *Wizarding Magic*: The Chosen Wizard, Winged Seeker (⚡), Triangle Relic.
  - *Shinobi & Grand Voyage*: Orange Shinobi, Straw Hat Captain, Three-Sword Master.
  - *Retro Kicks*: Classic Court High-Tops and street sneaker classics.
  - *Colossal Titans, Galactic Watch, Pop Idols, & Classics*: Wings of Freedom, Chrono-Dial, Music Legends, Diamond, Camera, and more.
- **🍋🌶️ Special Indian Charm: Customizable Nimbu-Mirchi Drying & Refresh Lifecycle**:
  - A traditional Indian protective amulet featuring a fresh lemon and 7 green chilies.
  - **User-Customizable Drying Lifespan (New in v0.9.1)**:
    - Configure exactly how long your Nimbu-Mirchi takes to dry via a smooth slider (from **1 hour** up to **168 hours / 7 days**).
    - One-click presets: `1h`, `12h`, `1 Day`, `2 Days` (default), `3 Days`, `1 Week`.
  - **Dynamic Visual Stages**:
    - **Fresh (0% – 50% lifespan)**: Vibrant bright green chilies and fresh sunny yellow lemon.
    - **Wilting (50% – 100% lifespan)**: Chilies turn olive and yellow-tinged, lemon dulls to ochre.
    - **Dried (100%+ lifespan)**: Chilies turn into wrinkled deep red *Lal Mirch* and the lemon shrivels to amber-brown.
  - **1-Click Refresh**:
    - Hang a fresh Nimbu-Mirchi anytime with 1 click from the System Tray or Customize Library.
    - System Tray menu dynamically shows current status and remaining time (e.g. `Refresh Nimbu-Mirchi (Fresh — Dries in 42h)`).
- **System Tray Controls & Quick Exit (New in v0.9.1)**:
  - Clean tray context menu with direct **Exit Charmly** option to close the app completely anytime.
  - Charmly appears cleanly named in Windows Task Manager.
- **🔄 Native In-App Update Engine (New in v0.9.1)**:
  - Check for updates directly inside the app with 1 click from the About page.
  - Automatically queries GitHub Releases, displays release notes, and streams the update with an animated progress bar.
  - Silent setup update engine seamlessly applies the update in the background and restarts Charmly without manual re-installation.
  - Supports both installed and portable copies.
- **Up to 3 Charms on One Cord**:
  - Hang a single statement charm or stack up to three charms in sequence down the cord. Each charm adds physical mass, rotational inertia, and collision constraints.
- **6 Physical Rope Styles**:
  - *Obsidian Cord*: Clean, sleek dark weave.
  - *Gold Chain*: Alternating open oval links with metallic highlights and high momentum.
  - *Silver Links*: Steel/silver interlocking chain.
  - *Crimson Ribbon*: Soft satin ribbon with vibrant red gradients.
  - *Braided Leather*: Warm rustic leather with cross-stitch markers.
  - *Beaded Thread*: Delicate thread dotted with spherical accent beads.
- **Verlet Physics Engine & Dynamic Audio**:
  - Multi-node particle chain integrated with Verlet dynamics at 240 Hz.
  - Real-time drag, throw, reach-clamp, atmospheric breeze, and collision relaxation passes.
  - Tactile audio feedback when charms collide, tap, or settle.
- **Transparent Desktop Overlay**:
  - GPU-accelerated borderless WinUI 3 layered window with per-pixel alpha transparency.
  - Full click-through support outside charm bounding bounds (`WS_EX_TRANSPARENT`).
  - Drops to **0% CPU** when charms settle to sleep.
- **Clean Experience**:
  - No developer support/donation prompts.
  - Clean, copyright-safe, and trademark-free.
  - Developed and maintained by **Tarun**.

---

## 🏗️ Project Architecture

```
Charmly/
├── src/
│   ├── Hangly.Core/                    # Pure domain models, Verlet physics, audio, & NimbuMirchi lifecycle
│   │   ├── Audio/                      # Sound synthesis and audio playback
│   │   ├── Models/                     # Charms, collections, anchors, rope styles, NimbuMirchiLifecycle
│   │   ├── Physics/                    # Verlet particle simulation, constraints, wind & mouse impulses
│   │   └── Settings/                   # AppSettings, Hanging definitions, local storage
│   │
│   └── Hangly.App/                     # WinUI 3 Windows desktop application
│       ├── Assets/                     # 161+ Charm SVGs (including fresh, wilting, and dried Nimbu-Mirchi)
│       ├── Branding/                   # Application branding and credits (Tarun)
│       ├── Customize/                  # Customization window, charm library, and lifecycle controls
│       ├── Overlay/                    # WinUI 3 transparent desktop overlay window & Win2D renderer
│       ├── Services/                   # App lifecycle, tray integration, single instance
│       └── Program.cs                  # Main entry point
│
└── tests/
    └── Hangly.Core.Tests/              # Comprehensive xUnit test suite (physics, ropes, lifecycle)
```

---

## 🚀 Building & Running

### Prerequisites
- Windows 10 (Build 1809+) or Windows 11 (x64)
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Windows App SDK

### Launching the Application

Use the included PowerShell runner:

```powershell
# Run the application in Release mode
.\run.ps1 -Configuration Release

# Or run in Debug mode
.\run.ps1 -Configuration Debug
```

---

## 📜 Credits & License

- **Developer**: Tarun
  - 🌐 Website: [itstarun.me](https://itstarun.me)
  - 🐙 GitHub: [@tarun33333](https://github.com/tarun33333)
- **License**: MIT

---

## ⚖️ Legal Disclaimer

*Charmly* is an independent, non-commercial open-source desktop personalization utility developed and maintained by Tarun under the MIT License. All charms, amulets, and symbols included in this software are original, generic, or parody artistic interpretations. Any references to cultural, mythological, or pop-culture motifs are strictly for non-commercial fan appreciation and parody under fair-use principles, and do not imply any affiliation, sponsorship, or endorsement by any trademark or copyright holders.

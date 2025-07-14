# PGodot – Procedural Generation Tool for Godot

<img width="644" height="229" alt="Proyecto nuevo (74) (1)" src="https://github.com/user-attachments/assets/a00a8d6b-84df-4a58-bcaa-f25e0b14f229" />

**PGodot** is a free and open-source procedural generation plugin for the Godot Engine. It enables developers to create dynamic 3D environments using noise-based techniques, directly from the editor or at runtime. This tool was developed with indie studios and solo devs in mind, offering procedural power without the complexity or price of commercial alternatives.

Built as part of a bachelor thesis at the CITM (UPC), PGodot is intended to streamline world-building and empower creators to generate replayable, diverse levels effortlessly. The long-term vision is to grow this plugin with community support and have it integrated natively into future versions of Godot.

<img width="1080" height="527" alt="image" src="https://github.com/user-attachments/assets/ab62faed-2ec0-4b13-b169-6430488afa01" />

---

## ✨ Features

### Terrain Generation
- Real-time mesh terrain using **Perlin Noise**
- Adjustable frequency, amplitude, scale, seed, and octaves
- Color mapping and visual shaders for elevation layers
- Seed-based generation ensures reproducibility

### Editor Plugin
- Custom dock integrated directly into the Godot editor
- Intuitive interface for editing noise parameters
- "Live Preview" toggle for instant feedback
- Export generated terrain as standalone scene files

### Extensibility
- Modular code structure prepared for:
  - Wave Function Collapse (WFC)
  - Cellular Automata
  - Room-based generation
- Future support planned for 2D TileMap procedural generation

---

## 🖥 Requirements

To use this plugin, **C# support must be enabled in Godot**. Please ensure the following are properly set up:

- Godot Engine version **4.2+**
- **Mono version of Godot** (C# support)
- **Visual Studio 2022 or newer** installed
- C# configured in Godot's editor settings:
  - Go to `Editor > Editor Settings > Mono`
  - Set the path to your Visual Studio's `MSBuild.exe`

If you don't link Visual Studio correctly, the plugin will not compile or run.

---

## Installation

1. Clone or download this repository into your Godot project’s `addons` folder:

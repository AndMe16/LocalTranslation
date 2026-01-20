# Local Translation

A quality-of-life editor mod for [**Zeepkist**](https://store.steampowered.com/app/1440670/Zeepkist/) that enables block translation and placement using **local axes** instead of fixed global axes.

This mod improves precision and workflow when building rotated or non-axis-aligned track sections.

---

## 🎮 About the Game

**Zeepkist** is a Unity-based racing game featuring a powerful in-game track editor that allows players to design and share custom tracks.

Local Translation extends the editor by making transformations orientation-aware.

---

## ✨ Features

- 📐 **Local-axis translation**
  - Move selected blocks along the local axis of a reference block.

- 🧭 **Orientation-aware placement**
  - When placing new blocks, the placement grid automatically rotates to match a reference block.

- 🎯 **Reference block system**
  - Use an existing block as a spatial reference for transformations.

- ⚡ **Fast workflow**
  - Designed for quick toggling during active building sessions.

---

## 🧩 How It Works

### Reference Block

The mod uses a **reference block** to define the local coordinate system.

- Select a block and press **NUMPAD 2** to set it as the reference.
- The block’s local axes are used for translation and placement.
- Press **NUMPAD 2** again to clear the reference when:
  - No block is selected, or
  - The same reference block is already active.

### Placement Behavior

When a reference block is set:
- The editor’s placement grid rotates to match the reference block’s orientation.
- New blocks align naturally with angled or rotated structures.

---

## 🛠️ Technical Overview

- **Language:** C#
- **Engine:** Unity
- **Type:** Editor Utility Mod
- **Distribution:** mod.io
- **License:** MIT

The mod operates entirely within the editor layer and does not modify core gameplay logic.

---

## 📦 Installation

The mod is distributed via [**mod.io**](https://mod.io/g/zeepkist/m/local-translation#description).

1. Install the mod through [ModkistRevamped](https://github.com/donderjoekel/ModkistRevamped) or mod.io.
2. Launch the game.
3. Open the track editor.
4. Use **NUMPAD 2** to set or clear a reference block.
5. Translate and place blocks using local axes.

---

## 🤝 Contributing

Contributions and suggestions are welcome.

Feel free to open an issue or pull request.

---

## 📄 License

This project is licensed under the **MIT License**.

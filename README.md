<div align="center">

# 🛩️ Hold The Plane

**A tiny endless flyer. Hold to climb, let go to dive, don't touch the spikes.**

[![Play now](https://img.shields.io/badge/▶_PLAY_NOW-in_your_browser-F2C94C?style=for-the-badge)](https://emresner.github.io/HoldThePlane/)

![Unity](https://img.shields.io/badge/Unity-6.3_LTS-000000?logo=unity&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-WebGL-990000?logo=webgl&logoColor=white)
![Assets](https://img.shields.io/badge/Assets-CC0_by_Kenney-2EA44F)
![Built with](https://img.shields.io/badge/Built_with-Claude_Code-D97757?logo=anthropic&logoColor=white)

[**▶ Play**](https://emresner.github.io/HoldThePlane/) · [**🎮 Controls**](#-controls) · [**✨ Features**](#-features) · [**🤖 How it was made**](#-how-it-was-made) · [**🙏 Credits**](#-credits)

</div>

<!-- Add a screenshot or GIF here, e.g. ![Gameplay](docs/gameplay.gif) -->

## 🎯 The Game

You're a small biplane in an endless cave run. Press and hold to rise, release to fall, and thread through spikes that hang from the ceiling and grow from the ground. Grab stars for bonus points, chase a new best score, and watch the world speed up the longer you survive. 🌥️

> [!TIP]
> Runs in any modern browser, desktop or phone (landscape). No install, no account.

## 🎮 Controls

| Input | Fly | Pause | Menu |
|---|---|---|---|
| 🖱️ Mouse / 👆 Touch | Hold to rise | Pause button | Tap |
| ⌨️ Keyboard | Hold `Space` | `Esc` / `P` | `←` `→` or `A` `D` to pick a plane |

## ✨ Features

- 🟦🟧🟩 **Three biomes** (ice, desert, grassland) that swap as you fly, each with its own level pieces and scenery
- 🧩 **21 level sections**, built with a custom Editor tool and checked by a flight simulation so every one is beatable at top speed
- 📈 **Dynamic difficulty:** the section mix and world speed ramp up over time (5.5 → 17.5 units/s in 75 seconds)
- ⭐ **Stars and medals:** distance score plus star bonuses, bronze / silver / gold medals
- ✈️ **Four plane colours**, a best score and settings that persist in your browser
- 🔊 **Sound effects** with a volume slider and mute, 💥 crash and pickup VFX with camera shake
- ⏯️ **Pause with a 3-2-1 resume countdown**
- 📱 **Responsive:** plays on any screen from 4:3 to 20:9

## 🤖 How it was made

This is a **learning project**. It was my first time building a game with **Claude Code**, so the real goal was to learn that workflow end to end, with a finished, playable game as the proof.

- 🧠 **Claude Code** wrote and refactored the C#, and drove the Unity Editor.
- 🔌 **Unity's official Claude plugin** (with its skills), the **Unity CLI** and the **Unity Pipeline** package let it inspect and edit scenes, prefabs and settings, run builds and read the Console. No third-party MCP servers.
- 🪜 **Stage by stage:** every feature followed the same loop: plan → my approval → implement → I playtest → write up what was done and why → commit. I made the design calls and judged how it *felt*; Claude did the heavy typing.
- 🛠️ Along the way we built custom Editor tools: a section generator, a beatability validator and a star placer, under `Tools → Level`.
- 📝 I kept detailed dev notes while building. They live outside this repo.

> [!NOTE]
> The game was developed in a **local Git repository** with dozens of small commits, one or more per stage. For this public repo I exported a single clean snapshot, so the history here is one commit. The built game is on the `gh-pages` branch.

## 🧰 Tech

| | |
|---|---|
| Engine | Unity **6.3 LTS** (`6000.3.28f1`), Universal Render Pipeline (2D) |
| Input | Unity Input System (mouse, touch, keyboard) |
| UI | uGUI + TextMeshPro, sprite-based score digits |
| Audio | AudioMixer, pooled `AudioSource`s, a `ScriptableObject` sound catalog |
| Web | WebGL, custom HTML template, hosted on GitHub Pages |
| Language | C# |

<details>
<summary><b>📁 Project layout</b></summary>

```text
Assets/
├─ Scripts/        game code (Player, Camera, World, Game, UI, Audio) + Editor tools
├─ Prefabs/        Plane, UI panels, VFX, level sections (7 per biome)
├─ Scenes/         Menu, Game
├─ Graphics/       Kenney art packs (CC0)
├─ Audio/          mixer + Kenney sound packs (CC0)
└─ WebGLTemplates/ the page the web build is wrapped in
```

</details>

## 🚀 Run the project

1. Install **Unity 6000.3.28f1** with Unity Hub.
2. Clone this repo and open the folder in Unity Hub. The first open imports assets and downloads packages from Unity's registry, so give it a few minutes.
3. Open `Assets/Scenes/Menu.unity` and press **Play**.

## 🙏 Credits

Huge thanks to **[Kenney](https://kenney.nl)** 💛 for releasing his art and audio as **[CC0](https://creativecommons.org/publicdomain/zero/1.0/)**. Without these packs this game wouldn't look or sound like anything.

The UI font is Kenney Future; TextMeshPro's default font is Liberation Sans (SIL OFL).

## 📜 License

No license has been chosen for the game code yet, so all rights are reserved. The Kenney assets keep their own **CC0** license (a `License.txt` sits in each pack folder).

<div align="center">

Made for fun and for learning.

</div>

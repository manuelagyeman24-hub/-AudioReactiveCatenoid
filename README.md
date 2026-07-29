# Catenoid Holographic Studio

An interactive, audio-reactive hologram of a **catenoid** — the minimal surface you get when you spin a
catenary around an axis — continuously morphing into its isometric partner, the **helicoid**.
Written in C# with WPF 3D and NAudio.

<img width="1536" alt="Catenoid Holographic Studio" src="https://github.com/user-attachments/assets/21c9438d-3e85-4ba4-b087-fa534fd78d1c" />

## Features

- **Holographic rendering** — translucent cyan→magenta surface with an emissive scan-line texture, a
  glowing wireframe shell, a projector plate with rotating rings and a volumetric light cone, plus
  scanlines, a light sweep and a flicker pass over the whole viewport.
- **Full 3D control** — orbit, pan, zoom and spin the surface with the mouse or the keyboard.
- **Live parameter panel** — neck radius, height, tessellation density, morph and spin speed, ripple,
  glow, hue drift, background intensity and audio sensitivity, all adjustable while it runs.
- **Audio reactive** — Windows loopback capture (WASAPI) drives the ripple, glow, spin rate and scale
  from whatever is playing on your machine. It degrades gracefully when no loopback device exists.
- **Animated background** — drifting nebulae, a parallax star field, a perspective data grid and a vignette.
- **Tested geometry core** — all surface math lives in `CatenoidCore`, a platform-independent library
  covered by 34 unit tests that run on any OS.

## The math

The catenoid, parameterised by angle `θ` and height `z` with neck radius `a`:

```
x = a·cosh(z/a)·cos θ      y = a·cosh(z/a)·sin θ      z = z
```

The helicoid it bends into without stretching:

```
x = z·cos θ                y = z·sin θ                z = a·θ·0.2
```

The renderer linearly blends the two (`morph` ∈ [0, 1]) and adds a radial ripple
`sin(6θ + 2φ)·cos(3z − φ)` scaled by the current audio level. See
[`CatenoidCore/CatenoidMath.cs`](CatenoidCore/CatenoidMath.cs).

## Requirements

- Windows 10/11 (WPF and WASAPI loopback are Windows-only)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run it

```bash
git clone https://github.com/manuelagyeman24-hub/-AudioReactiveCatenoid.git
cd -AudioReactiveCatenoid
dotnet run --project CatenoidDemo.csproj
```

Or build a standalone executable:

```bash
dotnet publish CatenoidDemo.csproj -c Release -r win-x64 --self-contained true -o publish
./publish/CatenoidDemo.exe
```

Every push also publishes a ready-to-run `win-x64` build as a CI artifact — see the
[Actions tab](../../actions).

## Controls

| Input | Action |
| --- | --- |
| Drag left mouse | Rotate (yaw / pitch) |
| Drag right mouse, `WASD`, arrows | Move the hologram |
| Scroll wheel, `+` / `-` | Zoom |
| `Space` | Toggle auto-spin |
| `X` | Cycle spin axis (Z → Y → X) |
| `[` / `]` | Spin speed |
| `Q` / `E` | Nudge yaw |
| `M` | Hold the morph (then use the panel slider to pick a shape) |
| `H` | Wireframe shell |
| `G` | Background grid |
| `Tab` | Show/hide the parameter panel |
| `F11` | Fullscreen |
| `P` | Save a PNG to `Pictures/CatenoidHologram` |
| `R` | Reset everything |

## Project layout

| Path | What it is |
| --- | --- |
| `CatenoidCore/` | Platform-independent surface math and mesh generation (`net8.0`) |
| `CatenoidCore.Tests/` | xUnit tests for the core (`dotnet test`, runs anywhere) |
| `MainWindow.xaml` | Scene graph, background layers, HUD and parameter panel |
| `MainWindow.xaml.cs` | Animation loop, camera/transform control, input handling, audio capture |
| `HoloSurface.cs` | Live mesh: builds topology once, rewrites positions in place each frame |
| `HoloSettings.cs` | Two-way bindable scene parameters shared by the panel and the shortcuts |
| `HoloGeometry.cs` | Core-mesh → WPF conversion, holographic brushes, HSV helpers |

## Development

```bash
dotnet test CatenoidCore.Tests/CatenoidCore.Tests.csproj   # geometry tests, any OS
dotnet build CatenoidDemo.sln                              # full solution, Windows
```

On a non-Windows machine the WPF project can still be compiled with
`dotnet build -p:EnableWindowsTargeting=true` (it cannot be run there).

Rendering notes: triangle indices and texture coordinates are computed once per resolution change and
the per-frame cost is a single pass writing `Point3D` values into the existing `Point3DCollection`;
materials and brushes are created once and mutated. The default 80×60 grid is ~9,600 triangles at 60 Hz.

## License

MIT — see [LICENSE](LICENSE).

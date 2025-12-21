 
 # 🎧 Audio‑Reactive 3D Catenoid Visualizer  
*A real‑time morphing minimal‑surface animation built with C#, WPF, and NAudio.*

## 🌐 Overview
This project renders a 3D catenoid–helicoid morphing surface that reacts dynamically to system audio. The animation uses real‑time loopback audio capture to drive:

- Surface morphing  
- Rotation speed  
- Glow intensity  
- Specular highlights  
- Vertical motion  
- Subtle color drift  

The result is a smooth, hypnotic, audio‑responsive visualizer built entirely with WPF’s 3D engine.

## ✨ Features
- Real‑time audio reactivity using NAudio  
- Morphing minimal surface (catenoid → helicoid blend)  
- Dynamic lighting with diffuse, emissive, and specular materials  
- Hue‑shifting rainbow gradient  
- Smooth camera‑like motion via rotation and translation transforms  
- Beat‑smoothed animation for stable visual response  
- GPU‑accelerated WPF 3D rendering  

## 🛠️ Technologies Used
- C# / .NET  
- WPF (Windows Presentation Foundation)  
- NAudio for audio capture  
- MeshGeometry3D for procedural surface generation  
- DispatcherTimer for real‑time animation  

## 📸 How It Works
The surface is generated procedurally each frame using the catenoid radius function:

r(z) = a · cosh(z / a)

This is blended with a helicoid parameterization to create a smooth morphing effect.

Audio RMS amplitude is captured from system output and smoothed to produce a stable beat signal. This beat drives:

- Morph parameter  
- Scale pulsing  
- Rotation acceleration  
- Glow intensity  

The animation behaves like a living mathematical sculpture that pulses with your music.

## ▶️ Running the Project
1. Clone the repository  
2. Open the solution in Visual Studio  
3. Restore NuGet packages (NAudio)  
4. Build and run  

Loopback audio capture works automatically on Windows 10/11.

## 📂 Project Structure

# 🎢 Roller Coaster Simulation – Unity 6

An interactive 3D roller coaster simulation built using **Unity 6**, featuring a custom-designed track, animated coaster cart, immersive camera views, and ESP32-based hardware communication.

## Overview

This project recreates a roller coaster ride in a 3D environment. The coaster cart follows a custom track through different turns, slopes, and elevation changes, creating an interactive ride experience.

The simulation also communicates with an ESP32 over Wi-Fi using UDP packets, allowing the coaster's motion data to be transmitted to an external hardware setup.

## Features

- **Custom Roller Coaster Track** – A 3D track with slopes, turns, drops, and elevation changes.
- **Animated Coaster Cart** – A cart that moves along the track with dynamic speed and motion.
- **First-Person View** – Experience the ride from the front seat of the coaster.
- **Overview Camera** – Switch to an external view to observe the entire ride.
- **3D Environment** – Includes terrain, vegetation, scenery, and supporting structures.
- **ESP32 Communication** – Sends coaster motion data to an ESP32 using UDP communication.
- **Motion Control** – Transmits pitch, roll, and yaw values based on the cart's position along the track.

## Controls

| Key | Function |
|---|---|
| C | Toggle between first-person and overview camera |
| R | Toggle ESP32 home mode |

## Technologies Used

- Unity 6
- C#
- ESP32
- UDP Communication
- Unity Input System

## Project Structure

```text
ARVR_Project/
│
├── Assets/
│   ├── Materials/
│   ├── Pure Poly/
│   ├── Scenes/
│   │   └── RollerCoasterScene.unity
│   └── Scripts/
│       ├── RollerCoaster/
│       │   ├── RollerCoasterBuilder.cs
│       │   ├── RollerCoasterCameraController.cs
│       │   ├── RollerCoasterCartController.cs
│       │   └── RollerCoasterEnvironment.cs
│       ├── CubeController.cs
│       └── ESP32UDPSender.cs
│
├── Packages/
├── ProjectSettings/
└── README.md
```

## Getting Started

1. Clone or download this repository.
2. Install **Unity 6** using Unity Hub.
3. Open the project through Unity Hub.
4. Navigate to `Assets/Scenes/RollerCoasterScene.unity`.
5. Open the scene and press **Play** to start the simulation.

## ESP32 Communication

The Unity application communicates with an ESP32 over a Wi-Fi network using UDP.

During the simulation, the application calculates the coaster's motion angles and transmits pitch, roll, and yaw values to the ESP32.

These values can be used by an external hardware setup to reproduce the corresponding movements.

## Demonstration

Screenshots and project demonstration videos can be added here.

## Repository

**GitHub:** [ARVR_Project](https://github.com/VivanJ7/ARVR_Project)

---

*Built with Unity 6 and C#.*

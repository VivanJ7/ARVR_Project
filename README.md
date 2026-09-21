# AR/VR Project – Unity-Based Interactive Simulation

## Overview

This project is an academic AR/VR implementation developed using Unity 6 and C#. The project focuses on creating an interactive 3D environment in Unity and demonstrating real-time control of object orientation.

The Unity application communicates with an ESP32 over a network connection using UDP communication. The received orientation data can be used to control the physical prototype.

## Objectives

- Develop an interactive 3D simulation using Unity.
- Implement real-time rotation along the X, Y, and Z axes.
- Establish communication between Unity and an ESP32.
- Demonstrate UDP-based communication between the software and hardware.
- Integrate the Unity simulation with a physical prototype.

## Technologies Used

- Unity 6
- C#
- ESP32
- UDP Communication
- Visual Studio Code

## Project Features

- Interactive 3D object simulation.
- Rotation control along three axes:
  - X-axis
  - Y-axis
  - Z-axis
- Reset functionality to return the object to its original orientation.
- Unity-to-ESP32 communication using UDP.
- Hardware control through the ESP32.
- Demonstration of software-to-hardware interaction.

## Project Structure

```text
ARVR_Project_Ini/
│
├── Assets/
│   ├── Scenes/
│   ├── Scripts/
│   ├── Settings/
│   └── ESP32UDPSender.cs
│
├── Packages/
│
├── ProjectSettings/
│
├── Documentation/
│   ├── Screenshots/
│   └── Videos/
│
└── README.md
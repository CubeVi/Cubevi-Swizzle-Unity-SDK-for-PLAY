<div align="center">
  <img src="doc/src/512x512.png" alt="Logo" width="80" height="80">
  <img src="doc/src/cube-logo-512x512.png" alt="Logo" width="80" height="80">
  <h3 align="center">CubeVi Swizzle Unity SDK for PLAY hardware</h3>

  <h3 align="center">CubeVi Swizzle SDK Unity</h3>

  <p align="center">
    A Unity Demo for light field rendering and naked-eye 3D display.
    <br />
    <a href="./README_zh.md">中文文档</a> | <a href="https://cubevi.com/pages/cubevi-play-launch-2">Website</a> | <a href="https://discord.com/invite/ZzEhKNJE8g">Discord</a>
  </p>
</div>

## Introduction
CubeVi Swizzle  Unity SDK is a software development kit designed for light field rendering and naked-eye 3D display in Unity. It integrates eye-tracking capabilities to provide a realistic and immersive 3D experience on supported devices [PLAY](https://cubevi.com/blogs/news/cubevi-play-developer-program-application).

The core functionality includes:
- **BatchCameraManager**: Manages multiple virtual cameras to capture different perspectives of the scene.
- **Interlacing Algorithm**: Merges these views into a single interlaced image suitable for the lenticular lens display.
- **Eye Tracking Integration**: Adjusts the rendering in real-time based on the user's eye position for optimal viewing angles.

## Dependencies

1.  **Unity Version**: Unity 2019 to 2022 (LTS versions recommended).
2.  **Packages**:
    - `Newtonsoft.Json`: Required for parsing configuration files. You can add it via the Package Manager using the name `com.unity.nuget.newtonsoft-json`.
3.  **Hardware**:
    - Supported device: [PLAY (27-inch display)](https://cubevi.com/pages/cubevi-play-launch)

## Quick Start

Follow these steps to quickly run the demo and start developing:

1.  **Open Demo Scene**:
    - Navigate to the `Assets/Scenes` folder (or relevant path based on project structure) and open the demo scene.

    ![Initial UI (Display 1)](doc/img/1.jpg)

    - **UI Controls**:
        - `OpenSBSVideo`: Opens a full-width SBS (side-by-side) stereo video.
        - `OpenMultiView`: Opens a pre-rendered 60-view (60-angle) multiview video.
        - `Start EyeTrack`: Starts eye tracking (requires `eye_tracking_x0.onnx/json` to exist in `Assets/StreamingAssets`) for better 3D effect.
        - `Toggle Eye Roll`: Enables eye-movement tracking after eye tracking is started (only for real-time rendering; not for video playback).
        - Grating parameters can be adjusted smoothly in the UI (e.g. `x0` / `interval` / `slope`) to tune the 3D effect.

2.  **Check Scene Setup**:
    - Ensure all UI elements in the scene are displayed correctly.
    - Verify that the `EventSystem` has the default Input Module script attached and is active.

3.  **Configure Displays**:
    - In the Unity Editor, go to the **Game** tab.
    - Create two "Game" windows:
        - **Window 1**: Set to **Display 1** (Main control/UI interface).
        - **Window 2**: Set to **Display 2** (This window simulates the naked-eye 3D content output).

    ![Running result (Display 1 and Display 2)](doc/img/2.jpg)

4.  **Verify Eye Tracking Resources**:
    - Ensure `eye_tracking_x0.onnx` and `eye_tracking_x0.json` are present in the `Assets/StreamingAssets` directory.
    - These files are required by `EyeTrackingManager.cs` and `DeviceDataManager.cs` for initializing the eye-tracking model and device parameters.

5.  **Run the Scene**:
    - Press **Play**.
    - You can interact with the buttons in the main view to test different functionalities.
    - Observe the 3D rendering output on Display 2.

6.  **Configuration Parameters**:
    - Configuration files are located in the `StreamingAssets` folder.
    - Key file: `eye_tracking_x0.json`
    - The demo `eye_tracking_x0.json` is for demonstration only. For real devices, use CubeStage (English) or OpenstageAI (Chinese) to connect the display and locate the device-specific config under:
        - `C:\Users\cubevi\AppData\Roaming\OpenstageAI\configs`
        - `C:\Users\cubevi\AppData\Roaming\Cubestage\configs`
      Then copy the correct `eye_tracking_x0.json` into `Assets/StreamingAssets`.
    - **Key Parameters**:
        - `grating_params`: Contains calibration data for the lenticular lens.
            - `x0`: Initial offset.
            - `interval`: Pitch of the lenticular lens.
            - `slope`: Slant angle of the lens.
    - These parameters are critical for the correct 3D effect and are usually pre-calibrated for the specific device. Developers can tweak these for debugging purposes if necessary.

    ![eye_tracking_x0.json location](doc/img/3.jpg)

## License

This project is licensed under the Apache License 2.0. See the [LICENSE.txt](LICENSE.txt) file for details.

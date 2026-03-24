<div align="center">
  <img src="doc/src/512x512.png" alt="Logo" width="80" height="80">
  <img src="doc/src/cube-logo-512x512.png" alt="Logo" width="80" height="80">
  <h3 align="center">CubeVi Swizzle Unity SDK for PLAY hardware</h3>

  <p align="center">
    Unity 环境下开发的裸眼 3D 渲染和光场显示 DEMO
    <br />
    <a href="./README.md">English Document</a> | <a href="https://cubevi.com/">官网</a> | <a href="https://discord.com/invite/ZzEhKNJE8g">Discord</a>
  </p>
</div>

## 项目介绍
CubeVi Swizzle  Unity SDK 是一个专为 Unity 开发的裸眼 3D 渲染和光场显示 SDK。它集成了眼球追踪功能，能够在支持的设备[PLAY](https://cubevi.com/blogs/news/cubevi-play-developer-program-application)上提供逼真且沉浸的 3D 视觉体验。

核心功能包括：
- **BatchCameraManager**：管理多个虚拟相机，用于捕捉场景的不同视角。
- **交织算法（Interlacing Algorithm）**：将这些视角合并为适合柱状透镜显示器显示的交织图像。
- **眼球追踪集成**：根据用户的眼球位置实时调整渲染，以提供最佳的观看视角。

## 项目依赖

1.  **Unity 版本**：建议使用 Unity 2019 至 2022 (推荐 LTS 版本)。
2.  **软件包**：
    - `Newtonsoft.Json`：用于解析配置文件。您可以在 Package Manager 中通过名称 `com.unity.nuget.newtonsoft-json` 添加。
3.  **硬件支持**：
    - 支持的 [“PLAY” （27英寸） 设备](https://cubevi.com/pages/cubevi-play-launch)。

## 快速使用

请按照以下步骤快速运行演示场景并开始开发：

1.  **打开 Demo 场景**：
    - 导航至 `Assets/Scenes` 文件夹（或相应的项目路径）并打开演示场景。

    ![运行初始界面（Display 1）](doc/img/1.jpg)

    - **UI 说明**：
        - `OpenSBSVideo`：打开全幅 SBS（左右格式）立体视频。
        - `OpenMultiView`：打开特制的 60 视角（60-angle）多视角视频。
        - `Start EyeTrack`：启动追瞳（需要确保 `Assets/StreamingAssets` 下存在 `eye_tracking_x0.onnx/json`，开启后即可获得正常 3D 效果）。
        - `Toggle Eye Roll`：在追瞳正确启动后，开启眼动追踪（仅支持实时渲染，不支持视频播放）。
        - 可以在 UI 上流畅调节光栅参数（如 `x0` / `interval` / `slope`）以优化 3D 效果。

2.  **检查场景设置**：
    - 确保场景中的所有 UI 元素正常显示。
    - 检查 `EventSystem` 上是否挂载了默认的 Input Module 脚本且工作正常。

3.  **配置显示窗口**：
    - 在 Unity 编辑器中，找到 **Game** 选项卡。
    - 创建两个 "Game" 窗口：
        - **窗口 1**：设置为 **Display 1**（用于主控/UI 界面）。
        - **窗口 2**：设置为 **Display 2**（此窗口用于模拟显示裸眼 3D 内容）。

    ![运行成功画面（同时展示 Display 1 与 Display 2）](doc/img/2.jpg)

4.  **验证眼控资源文件**：
    - 确保 `Assets/StreamingAssets` 目录下包含 `eye_tracking_x0.onnx` 和 `eye_tracking_x0.json` 文件。
    - 这些是 `EyeTrackingManager.cs` 和 `DeviceDataManager.cs` 初始化眼球追踪模型和设备参数所必须的文件。

5.  **运行场景**：
    - 点击 **Play** 运行。
    - 您可以使用主视图中的按钮测试各种功能。
    - 在 Display 2 上观察 3D 渲染输出。

6.  **参数配置**：
    - 配置文件位于 `StreamingAssets` 文件夹中。
    - 核心文件：`eye_tracking_x0.json`
    - 注意：Demo 项目中的 `eye_tracking_x0.json` 参数仅用于示例。实际设备的正确参数需要使用 Cubestage（英文版）或 OpenstageAI（中文版）连接屏幕后，到对应目录中查找设备配置，例如：
        - `C:\Users\cubevi\AppData\Roaming\OpenstageAI\configs`
        - `C:\Users\cubevi\AppData\Roaming\Cubestage\configs`
      然后将正确的 `eye_tracking_x0.json` 拷贝到 `Assets/StreamingAssets` 下使用。
    - **关键参数说明**：
        - `grating_params`：包含柱状透镜的校准数据。
            - `x0`：初始偏移量。
            - `interval`：光栅间距。
            - `slope`：光栅倾斜角度。
    - 这些参数对于呈现正确的 3D 效果至关重要，通常针对特定设备进行了预校准。如果需要调试，开发者可以微调这些参数。

    ![eye_tracking_x0.json 参数文件位置](doc/img/3.jpg)

## 许可证 (License)

本项目采用 Apache License 2.0 许可证。详情请参阅 [LICENSE.txt](LICENSE.txt) 文件。

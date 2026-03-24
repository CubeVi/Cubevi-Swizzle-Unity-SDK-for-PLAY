using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cubevi_Swizzle
{
    public class DebugCapture : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Key to trigger debug capture")]
        public KeyCode captureKey = KeyCode.F6;

        [Tooltip("Whether to output debug info to console")]
        public bool logDebugInfo = true;

        [Header("Component References")]
        public BatchCameraManager batchCameraManager;
        public DeviceDataManager deviceDataManager;

        private void Start()
        {
            if (batchCameraManager == null)
            {
                SwizzleLog.LogError("BatchCameraManager not found, SwizzleDebugCapture will not work properly");
            }

            if (deviceDataManager == null)
            {
                SwizzleLog.LogError("DeviceDataManager not found, SwizzleDebugCapture will not work properly");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(captureKey))
            {
                CaptureDebugData();
            }
        }

        /// <summary>
        /// Capture debug data
        /// </summary>
        public void CaptureDebugData()
        {
            if (batchCameraManager == null || deviceDataManager == null)
            {
                SwizzleLog.LogError("Missing necessary components, cannot capture debug data");
                return;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folderName = "debug_" + timestamp;
                string folderPath = Path.Combine(Application.persistentDataPath, folderName);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Save current displayCamera image
                SaveDisplayCameraImage(folderPath);

                // Save current device parameter config
                SaveDeviceParameterConfig(folderPath);

                if (logDebugInfo)
                {
                    SwizzleLog.LogInfo($"Debug data saved to: {folderPath}");
                }
            }
            catch (Exception ex)
            {
                SwizzleLog.LogError($"Error capturing debug data: {ex.Message}");
            }
        }

        /// <summary>
        /// Save current displayCamera image
        /// </summary>
        private void SaveDisplayCameraImage(string folderPath)
        {
            if (batchCameraManager.displayCamera == null)
            {
                SwizzleLog.LogError("displayCamera not found, cannot capture image");
                return;
            }

            try
            {
                int width = (int)batchCameraManager._device.output_size_X;
                int height = (int)batchCameraManager._device.output_size_Y;

                RenderTexture tempRT = new RenderTexture(width, height, 24);
                RenderTexture originalRT = batchCameraManager.displayCamera.targetTexture;

                batchCameraManager.displayCamera.targetTexture = tempRT;

                batchCameraManager.displayCamera.Render();

                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);

                RenderTexture.active = tempRT;

                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();

                batchCameraManager.displayCamera.targetTexture = originalRT;
                RenderTexture.active = null;

                byte[] bytes = screenshot.EncodeToPNG();
                string filePath = Path.Combine(folderPath, "batch.png");
                File.WriteAllBytes(filePath, bytes);

                Destroy(tempRT);
                Destroy(screenshot);

                if (logDebugInfo)
                {
                    SwizzleLog.LogInfo($"Camera image saved: {filePath}");
                }
            }
            catch (Exception ex)
            {
                SwizzleLog.LogError($"Error saving camera image: {ex.Message}");
            }
        }

        /// <summary>
        /// Save current device parameter config
        /// </summary>
        private void SaveDeviceParameterConfig(string folderPath)
        {
            try
            {
                string deviceName = deviceDataManager.selectedDeviceType.ToString();

                var deviceConfigs = typeof(DeviceDataManager)
                    .GetField("deviceConfigs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(deviceDataManager) as Dictionary<string, DeviceParameterConfig>;

                if (deviceConfigs != null && deviceConfigs.TryGetValue(deviceName, out DeviceParameterConfig config))
                {
                    string json = JsonUtility.ToJson(config, true);
                    string filePath = Path.Combine(folderPath, "device_config.json");
                    File.WriteAllText(filePath, json);

                    if (logDebugInfo)
                    {
                        SwizzleLog.LogInfo($"Device config saved: {filePath}");
                    }

                    if (batchCameraManager._device != null)
                    {
                        string deviceDataJson = JsonUtility.ToJson(batchCameraManager._device, true);
                        string deviceDataPath = Path.Combine(folderPath, "device_data.json");
                        File.WriteAllText(deviceDataPath, deviceDataJson);

                        if (logDebugInfo)
                        {
                            SwizzleLog.LogInfo($"Device data saved: {deviceDataPath}");
                        }
                    }
                }
                else
                {
                    SwizzleLog.LogError($"Device config data not found for {deviceName}");
                }
            }
            catch (Exception ex)
            {
                SwizzleLog.LogError($"Error saving device config: {ex.Message}");
            }
        }
    }
}
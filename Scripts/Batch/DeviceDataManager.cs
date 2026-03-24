using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cubevi_Swizzle
{
    /// <summary>
    /// Interlace related parameters
    /// </summary>
    [System.Serializable]
    public class DeviceData
    {
        // Screen name tag
        public string name;
        public string screen_name;

        // Number of viewpoints created
        public int imgs_count_x;
        public int imgs_count_y;
        public int total_viewnum;
        public float total_fov;

        // Screen size (Unit: mm)
        public float oc_size_x;

        // Sub-image and interlaced image size (Unit: pixel)
        public float output_size_X;
        public float output_size_Y;
        public float subimg_width;
        public float subimg_height;

        // Interlace variables
        public float interval_fov;     // Real fov of grating
        public float sbs_fov = 3.783f; // Angle between eyes (usually not modified)
        public float f_cam;
        public float tan_alpha_2;
        public float mul_offset;       // Base offset of grating screen
        public string pixel_order;     // RGB order of sub-pixels in interlaced rendering

        // Eye tracking variables
        public float cam_fov_x;  // Camera horizontal fov, unit: degree
        public float delta_pos;  // IPD related value calibrated at 500mm
        public float delta_700_pos; // IPD related value calibrated at 700mm

        // Multi-view interlace parameters
        public float x0;
        public float interval;
        public float slope;

        public bool is27Layout = false;
        // 27-inch specific parameters (for MultiView_sbs_27)
        public float rowNum = 10f;
        public float colNum = 6f;
        public string x0TexFileName = "undefined";
    }

    /// <summary>
    /// Device type group enum
    /// </summary>
    public enum DeviceGroup
    {
        US,
        China,
        All
    }

    public enum DeviceType
    {
        [DeviceGroupAttribute(DeviceGroup.China)]
        Type_15p6_only,

        [DeviceGroupAttribute(DeviceGroup.China)]
        Type_27_test,
    }

    // Device group attribute
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class DeviceGroupAttribute : System.Attribute
    {
        public DeviceGroup Group { get; private set; }

        public DeviceGroupAttribute(DeviceGroup group)
        {
            Group = group;
        }
    }

    public class DeviceConfig
    {
        public string type { get; set; }
        public OA_devicedata config { get; set; }
        public string hardwareSN { get; set; }
    }

    public class LableListConfig
    {
        public string type { get; set; }
        public List<string> lablelist { get; set; }
    }

    public class OA_devicedata
    {
        public float lineNumber { get; set; }
        public float obliquity { get; set; }
        public float deviation { get; set; }

        public string deviceId { get; set; }
        public float originDeviation { get; set; }
        public string deviceNumber { get; set; }
    }

    [System.Serializable]
    public class DeviceParameterConfig
    {
        public string name; // Corresponding DeviceType

        // Interlace parameters
        public float x0;
        public float interval;
        public float slope;

        // Other adjustable parameters
        public float interval_fov;
        public float delta_pos;
        public float delta_700_pos;
        public float cam_fov_x;

        // 27-inch specific parameters
        public bool is27Layout;
    }

    public class DeviceDataManager : MonoBehaviour
    {
        [Header("Select Rendering Mode")]
        public DeviceType selectedDeviceType = DeviceType.Type_27_test;

        [Header("Device Group Settings")]
        public DeviceGroup activeDeviceGroup = DeviceGroup.US;

        // Config file folder path
        private string configFolderPath;
        private Dictionary<string, DeviceParameterConfig> deviceConfigs = new Dictionary<string, DeviceParameterConfig>();

        private static DeviceDataManager _instance;
        public static DeviceDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    SwizzleLog.LogError("Cannot find DeviceData instance");
                    return _instance;
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                InitializeConfigSystem(this._Batch);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        // Temporary reference to BatchCameraManager, used only for initialization
        private BatchCameraManager _Batch;

        public void SetBatchCameraManager(BatchCameraManager batch)
        {
            _Batch = batch;
            // If set after Awake, may need to manually trigger parameter application
             if (_deviceDataInitialized && _Batch != null)
            {
                 ApplyDeviceConfig(_Batch, selectedDeviceType.ToString());
            }
        }

        private bool _deviceDataInitialized = false;

        private void InitializeConfigSystem(BatchCameraManager batch = null)
        {
            // Set config file folder path
            configFolderPath = Path.Combine(Application.persistentDataPath, "device_parameter_v2_new");

            // Ensure directory exists
            if (!Directory.Exists(configFolderPath))
            {
                Directory.CreateDirectory(configFolderPath);
            }

            // Read parameters from eye_tracking_x0.json on initialization
            LoadParamsFromJson();
            
            // If batch instance is provided, apply delta_700_pos immediately
            if (batch != null && batch._device != null)
            {
                batch._device.delta_700_pos = defaultDelta700Pos;
            }

            LoadAllDeviceConfigs();
            _deviceDataInitialized = true;
        }

        private float defaultDelta700Pos = 0.0715f;
        private float defaultX0 = 18.0f;
        private float defaultInterval = 7.53800f;
        private float defaultSlope = -0.133632f;
        private bool hasJsonParams = false;

        private void LoadParamsFromJson()
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "eye_tracking_x0.json");
            if (!File.Exists(jsonPath))
            {
                SwizzleLog.LogWarning("eye_tracking_x0.json not found, using default parameters");
                return;
            }

            try
            {
                string jsonText = File.ReadAllText(jsonPath);
                JObject root = JObject.Parse(jsonText);

                if (root["grating_params"] is JObject gratingParams)
                {
                    if (gratingParams["x0"] != null) defaultX0 = (float)gratingParams["x0"];
                    if (gratingParams["interval"] != null) defaultInterval = (float)gratingParams["interval"];
                    if (gratingParams["slope"] != null) defaultSlope = (float)gratingParams["slope"];
                    hasJsonParams = true;
                }

                if (root["cam_para"] is JObject camParams && camParams["delta_x_700"] != null)
                {
                    defaultDelta700Pos = (float)camParams["delta_x_700"];
                }

                SwizzleLog.LogInfo($"Parameters loaded from eye_tracking_x0.json: x0={defaultX0}, interval={defaultInterval}, slope={defaultSlope}, delta_700={defaultDelta700Pos}");
            }
            catch (Exception e)
            {
                SwizzleLog.LogError($"Failed to read eye_tracking_x0.json: {e.Message}");
            }
        }

        private void LoadAllDeviceConfigs()
        {
            foreach (DeviceType deviceType in Enum.GetValues(typeof(DeviceType)))
            {
                string deviceName = deviceType.ToString();
                LoadDeviceConfig(deviceName);
            }
        }

        private void LoadDeviceConfig(string deviceName)
        {
            try
            {
                string configFilePath = Path.Combine(configFolderPath, $"{deviceName}.json");
                if (File.Exists(configFilePath))
                {
                    string jsonContent = File.ReadAllText(configFilePath);
                    DeviceParameterConfig config = JsonUtility.FromJson<DeviceParameterConfig>(jsonContent);

                    deviceConfigs[deviceName] = config;
                    SwizzleLog.LogInfo($"Device config loaded: {deviceName}");
                }
                else
                {
                    CreateDefaultConfig(deviceName);
                }
            }
            catch (Exception ex)
            {
                SwizzleLog.LogError($"Failed to load device config: {deviceName}, Error: {ex.Message}");
                CreateDefaultConfig(deviceName);
            }
        }

        private void CreateDefaultConfig(string deviceName)
        {
            // Create temporary batch camera manager to get default parameters
            BatchCameraManager tempBatch = new BatchCameraManager();
            DeviceType deviceType = (DeviceType)Enum.Parse(typeof(DeviceType), deviceName);

            // Save currently selected device type
            DeviceType currentType = selectedDeviceType;

            // Temporarily switch device type
            selectedDeviceType = deviceType;

            // Initialize device data
            InitDeviceData(tempBatch);

            // If json parameters exist, overwrite hardcoded default values
            if (hasJsonParams)
            {
                tempBatch._device.x0 = defaultX0;
                tempBatch._device.interval = defaultInterval;
                tempBatch._device.slope = defaultSlope;
            }

            // Create config
            DeviceParameterConfig config = new DeviceParameterConfig
            {
                name = deviceName,
                x0 = tempBatch._device.x0,
                interval = tempBatch._device.interval,
                slope = tempBatch._device.slope,

                interval_fov = tempBatch._device.interval_fov,
                delta_pos = tempBatch._device.delta_pos,
                delta_700_pos = tempBatch._device.delta_700_pos,
                cam_fov_x = tempBatch._device.cam_fov_x,

                // 27-inch specific parameters
                is27Layout = tempBatch._device.is27Layout
            };

            // Save config
            deviceConfigs[deviceName] = config;
            SaveDeviceConfig(config);

            // Restore original device type
            selectedDeviceType = currentType;

            SwizzleLog.LogInfo($"Default device config created: {deviceName}");
        }

        public void SaveDeviceConfig(DeviceParameterConfig config)
        {
            try
            {
                string jsonContent = JsonUtility.ToJson(config, true);
                string configFilePath = Path.Combine(configFolderPath, $"{config.name}.json");
                File.WriteAllText(configFilePath, jsonContent);
                SwizzleLog.LogInfo($"Device config saved: {config.name}");

                string eyeTrackingJsonPath = Path.Combine(Application.streamingAssetsPath, "eye_tracking_x0.json");
                if (File.Exists(eyeTrackingJsonPath))
                {
                    TryUpdateEyeTrackingJson(eyeTrackingJsonPath, config.x0, config.interval, config.slope, config.delta_700_pos);
                }
            }
            catch (Exception ex)
            {
                SwizzleLog.LogError($"Failed to save device config: {config.name}, Error: {ex.Message}");
            }
        }

        private static void TryUpdateEyeTrackingJson(string jsonPath, float x0, float interval, float slope, float delta_700_pos)
        {
            try
            {
                string jsonText = File.ReadAllText(jsonPath);
                JObject root = JObject.Parse(jsonText);
                JObject gratingParams = root["grating_params"] as JObject;
                if (gratingParams == null)
                {
                    gratingParams = new JObject();
                    root["grating_params"] = gratingParams;
                }

                gratingParams["x0"] = x0;
                gratingParams["interval"] = interval;
                gratingParams["slope"] = slope;

                JObject camParams = root["cam_para"] as JObject;
                if (camParams == null)
                {
                    camParams = new JObject();
                    root["cam_para"] = camParams;
                }
                camParams["delta_x_700"] = delta_700_pos;

                string updatedText = root.ToString(Formatting.Indented);
                File.WriteAllText(jsonPath, updatedText);
                SwizzleLog.LogInfo("Synced x0/interval/slope/delta_x_700 to eye_tracking_x0.json");
            }
            catch (Exception e)
            {
                SwizzleLog.LogError($"Failed to sync eye_tracking_x0.json: {e.Message}");
            }
        }

        public DeviceParameterConfig GetDeviceConfig(string deviceName)
        {
            if (deviceConfigs.TryGetValue(deviceName, out DeviceParameterConfig config))
            {
                return config;
            }
            return null;
        }

        public void ResetCurrentDeviceParameters()
        {
            string deviceName = selectedDeviceType.ToString();

            // Delete existing config file
            string configFilePath = Path.Combine(configFolderPath, $"{deviceName}.json");
            if (File.Exists(configFilePath))
            {
                try
                {
                    File.Delete(configFilePath);
                    SwizzleLog.LogInfo($"Device config file deleted: {deviceName}");
                }
                catch (Exception ex)
                {
                    SwizzleLog.LogError($"Failed to delete device config file: {deviceName}, Error: {ex.Message}");
                }
            }

            // Remove from device config dictionary
            if (deviceConfigs.ContainsKey(deviceName))
            {
                deviceConfigs.Remove(deviceName);
            }

            // Recreate default config
            CreateDefaultConfig(deviceName);
        }

        public void UpdateAndSaveDeviceConfig(Action<DeviceParameterConfig> updateAction)
        {
            string deviceName = selectedDeviceType.ToString();
            if (!deviceConfigs.TryGetValue(deviceName, out DeviceParameterConfig config))
            {
                config = new DeviceParameterConfig { name = deviceName };
                deviceConfigs[deviceName] = config;
            }

            updateAction(config);
            SaveDeviceConfig(config);
        }

        public void SaveCurrentDeviceParameters(float x0, float interval, float slope,
            float interval_fov, float delta_pos, float cam_fov_x,
            bool is27Layout, float KX1, float KX3, float KY2, float KY4, float[] X0Array)
        {
            UpdateAndSaveDeviceConfig(config =>
            {
                config.x0 = x0;
                config.interval = interval;
                config.slope = slope;
                config.interval_fov = interval_fov;
                config.delta_pos = delta_pos;
                config.cam_fov_x = cam_fov_x;
                config.is27Layout = is27Layout;
            });
        }

        public void SaveExtendedParameters(float interval_fov, float delta_pos, float delta_700_pos, float cam_fov_x)
        {
            UpdateAndSaveDeviceConfig(config =>
            {
                // New parameters
                config.interval_fov = interval_fov;
                config.delta_pos = delta_pos;
                config.cam_fov_x = cam_fov_x;
            });
        }

        public List<DeviceType> GetDeviceTypesInCurrentGroup()
        {
            List<DeviceType> result = new List<DeviceType>();

            foreach (DeviceType deviceType in Enum.GetValues(typeof(DeviceType)))
            {
                // If All group is selected, include all device types
                if (activeDeviceGroup == DeviceGroup.All)
                {
                    result.Add(deviceType);
                    continue;
                }

                var field = typeof(DeviceType).GetField(deviceType.ToString());
                var attributes = field.GetCustomAttributes(typeof(DeviceGroupAttribute), false);

                if (attributes.Length == 0)
                {
                    result.Add(deviceType);
                    continue;
                }

                foreach (DeviceGroupAttribute attribute in attributes)
                {
                    if (attribute.Group == activeDeviceGroup)
                    {
                        result.Add(deviceType);
                        break;
                    }
                }
            }

            return result;
        }

        public void InitDeviceData(BatchCameraManager _Batch)
        {
            string deviceName = selectedDeviceType.ToString();

            switch (selectedDeviceType)
            {
                case DeviceType.Type_15p6_only:
                    _Batch._device = new DeviceData()
                    {
                        name = "15p6_Test",
                        screen_name = "15p6_001",

                        imgs_count_x = 6,
                        imgs_count_y = 10,
                        total_viewnum = 60,
                        total_fov = 59.0f,

                        output_size_X = 3840f,
                        output_size_Y = 2160f,
                        subimg_width = 1920f,
                        subimg_height = 1080f,

                        oc_size_x = 345.6f,

                        interval_fov = 13.4f,
                        f_cam = 2778.0f,
                        tan_alpha_2 = 0.3456f,
                        mul_offset = -1.25f,
                        pixel_order = "rgb_012",

                        cam_fov_x = 67.4f,
                        delta_pos = 0.1566f,
                        delta_700_pos = 0.0715f,

                        x0 = 10.2f,
                        interval = 8.666f,
                        slope = -0.2910f,

                        // Flag for 15p6 layout
                        is27Layout = false
                    };
                    break;

                case DeviceType.Type_27_test:
                    _Batch._device = new DeviceData()
                    {
                        name = "27_Screen_Test",
                        screen_name = "27_001",

                        imgs_count_x = 6,
                        imgs_count_y = 10,
                        total_viewnum = 60,
                        total_fov = 59.0f,

                        output_size_X = 5120f,
                        output_size_Y = 2880f,
                        subimg_width = 2560f,
                        subimg_height = 1440f,

                        oc_size_x = 600.0f,

                        interval_fov = 13.4f,
                        f_cam = 2778.0f,
                        tan_alpha_2 = 0.3456f,
                        mul_offset = -1.25f,
                        pixel_order = "rgb_012",

                        cam_fov_x = 71.5f,
                        delta_pos = 0.1566f,
                        delta_700_pos = 0.0715f,

                        x0 = 18.0f,
                        interval = 7.53800f,
                        slope = -0.133632f,

                        // 27-inch screen flag
                        is27Layout = true,

                        rowNum = 10f,
                        colNum = 6f,
                        x0TexFileName = "undefined"

                    };
                    break;

            }

            ApplyDeviceConfig(_Batch, deviceName);
        }

        private void ApplyDeviceConfig(BatchCameraManager _Batch, string deviceName)
        {
            if (_Batch == null || _Batch._device == null) return;

            if (deviceConfigs.TryGetValue(deviceName, out DeviceParameterConfig config))
            {
                // Apply config parameters
                // If json parameters exist, prioritize them
                if (hasJsonParams)
                {
                    _Batch._device.x0 = defaultX0;
                    _Batch._device.interval = defaultInterval;
                    _Batch._device.slope = defaultSlope;
                }
                else
                {
                    _Batch._device.x0 = config.x0;
                    _Batch._device.interval = config.interval;
                    _Batch._device.slope = config.slope;
                }

                // Apply extended parameters
                _Batch._device.interval_fov = config.interval_fov;
                _Batch._device.delta_pos = config.delta_pos;
                _Batch._device.delta_700_pos = defaultDelta700Pos;
                _Batch._device.cam_fov_x = config.cam_fov_x;

                // Apply 27-inch specific parameters
                _Batch._device.is27Layout = config.is27Layout;

                SwizzleLog.LogInfo($"Device config applied: {deviceName}");
            }
        }
    }
}

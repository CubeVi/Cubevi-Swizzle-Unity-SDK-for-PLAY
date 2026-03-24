using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Cubevi_Swizzle
{
    public class BatchCameraManager : MonoBehaviour
    {
        #region Grid Camera Config
        [Header("Grid Camera Related Settings")]
        public Transform RootTransfrom;
        public GameObject BatchCameraPrefab;
        public bool useCameraPrefab = false;

        // Reload variables
        private bool isReinitializing = false;

        // Grid camera and related parameters
        private Camera[] _batchcameras;
        private float[] _batchcameras_angles;
        private Texture2DArray textureArray;
        private RenderTexture[] cameraRenderTextures;
        private float fl;
        #endregion

        #region Focal Config
        [Header("Grid Camera Focal Related Settings")]
        public Transform TargetTransform;
        [Range(0.1f, 500.0f)]
        public float FocalPlane = 10f;
        public bool useTartgetFocal = true;

        // Follow focus
        private Transform Root;
        private Transform Target;

        [Header("Focal and Screen Debug")]
        public bool showFocalPlane = false;
        public bool showFrustumFrame = false;
        public bool showGridSquare = false;
        public bool showSerial = false;
        public bool onlyShowSerial = false;
        #endregion

        #region Eye Tracking Config
        [Header("Eye Tracking Component Settings")]
        public EyeTrackingManager _eye;
        public Button StartEye;
        public Button StopEye;

        [Header("Eye Movement Tracking")]
        public Button ToggleEyeRoll;
        private bool useEyeTrackingXOffset = false;
        [Range(0.2f, 1.8f)]
        public float eyeTrackingK = 1.0f;
        private float eyeTrackingDeadZone = 0.1f;
        private float eyeTrackingSmoothing = 0.5f;
        private Vector2 eyeOffset;
        private float eyeTrackingOffsetX;
        private float eyeTrackingOffsetVelocity;
        private float eyeTrackingOffsetY;
        private float eyeTrackingOffsetVelocityY;

        // Add camera active range variable
        private bool[] isCameraActive;

        // convergence and separation control
        #endregion

        #region Interlace Parameter Config
        private float k1;
        private float k2;
        private float k3;
        private bool suppressKSliderCallback = false;
        [Header("Interlace Parameters Adjustable")]
        public Slider k1_slider;
        public TMP_InputField k1_text;
        public Slider k2_slider;
        public TMP_InputField k2_text;
        public Slider k3_slider;
        public TMP_InputField k3_text;

        [Header("Extended Parameters Adjustable")]
        public TMP_InputField interval_fov_text;
        public TMP_InputField delta_pos_text;
        public TMP_InputField cam_fov_x_text;

        // Add precision control related variables
        [Header("Precision Control")]
        public TMP_Dropdown k1PrecisionDropdown;
        public TMP_Dropdown k2PrecisionDropdown;
        public TMP_Dropdown k3PrecisionDropdown;
        private List<string> precisionOptions = new List<string>() { "0.1", "0.01", "0.001", "0.0001", "0.00001" };
        private List<float> precisionValues = new List<float>() { 0.1f, 0.01f, 0.001f, 0.0001f, 0.00001f };
        private List<string> precisionFormats = new List<string>() { "F1", "F2", "F3", "F4", "F5" };
        private int currentK1PrecisionIndex = 2;
        private int currentK2PrecisionIndex = 3;
        private int currentK3PrecisionIndex = 3;
        [Header("Interlace Parameters")]
        public DeviceData _device;
        public Button saveParametersButton;
        public Button resetParametersButton;
        #endregion

        #region Mode Management
        public enum AppMode
        {
            Default,        // Default mode
            SBSVideo,       // Stereo video mode
            MultipleView    // Multi-view video mode
        }
        [Header("Mode Management")]
        public AppMode currentMode = AppMode.Default;
        public Button BackButton;
        public Button showViewBoundaryButton;
        private bool isShowingViewBoundary = false;

        [Header("Stereo Video Playback Related Settings")]
        public SBSVideoManager sbs_videoManager;
        private bool isSBS = false;

        [Header("Multi-view Playback Related Settings")]
        public Button MultipleView_FileLoad;
        private Coroutine multipleViewLoadRoutine;
        private GameObject multipleViewVideoObject;
        private VideoPlayer multipleViewVideoPlayer;
        private RenderTexture multipleViewRenderTexture;
        private Texture2DArray multipleViewTextureArray;
        private bool multipleViewFrameReady = false;
        private long multipleViewReadyFrame = -1;
        private bool isMultipleViewLoading = false;
        private Material defaultBatchMaterial;
        #endregion

        #region Other Parameter Configuration
        private float virtcam_viewnum;
        private float mysrc_tan;
        private float mycam_tan;

        // 27-inch screen parameters
        [Header("27 Test parameter")]
        public float[] X0Array = new float[77];

        // Device screen
        private int mytargetScreenIndex = 0;
        // Display image
        [HideInInspector]
        public Camera displayCamera;
        private Material _batchMaterial;
        private int uiLayer;
        private Canvas displaycanvas;
        private RawImage BatchImage;

        // Serial Number
        private TextMeshProUGUI[] AngleText;

        // Visual Debugger
        private BatchVisualDebugger visualDebugger;
        // Latency Monitor
        private BatchLatencyMonitor latencyMonitor;

        // Internal property for VisualDebugger to access
        internal bool IsSBS => isSBS;
        #endregion

        #region Unity_Initialization Methods
        private void Awake()
        {
            DontDestroyOnLoad(this);
            isReinitializing = false;
            uiLayer = LayerMask.NameToLayer("UI");

            // Initialize display settings
            InitializeDisplaySettings();

            // Initialize Components
            visualDebugger = gameObject.AddComponent<BatchVisualDebugger>();
            latencyMonitor = gameObject.AddComponent<BatchLatencyMonitor>();
        }

        private void Start()
        {
            // Initialize render textures and shaders
            ConfigureViewParameters();
            InitRenderTexture();
            LoadSwizzleShader();
            defaultBatchMaterial = _batchMaterial;

            // Initialize interlace parameter adjustment
            InitAdjustK();
            InitSaveButton();

            // Initialize target and cameras
            InitTarget();
            InitCamera();
            InitDisplayCamera();

            // Initialize debug visualization and latency monitoring
            visualDebugger.Initialize(this);
            latencyMonitor.Initialize(this, _eye);

            // Initialize special rendering modes
            InitModeManagement();
            sbs_videoManager.Initialize(this);

            // Initialize ToggleEyeRoll button state and events
            if (ToggleEyeRoll != null)
            {
                ToggleEyeRoll.interactable = false;
                ToggleEyeRoll.onClick.AddListener(ToggleEyeTrackingOffset);
            }

            StartEye.onClick.AddListener(() =>
            {
                _eye.StartTracking();
                if (ToggleEyeRoll != null) ToggleEyeRoll.interactable = true;
            });

            StopEye.onClick.AddListener(() =>
            {
                _eye.StopTracking();
                if (ToggleEyeRoll != null) ToggleEyeRoll.interactable = false;
                if (useEyeTrackingXOffset)
                {
                    useEyeTrackingXOffset = false;
                    SwizzleLog.LogInfo("StopEye clicked: useEyeTrackingXOffset reset to false");
                }
            });

            // Initialize view boundary display button
            if (showViewBoundaryButton != null)
            {
                showViewBoundaryButton.onClick.AddListener(ToggleViewBoundary);
                UpdateViewBoundaryButtonVisibility();
            }

            if (_eye != null)
            {
                _eye.OnX0ArrayAvailable += HandleX0ArrayUpdate;
            }

            UpdateManager.Instance.RegisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.BatchCamera);
        }

        private void InitializeDisplaySettings()
        {
            // Display settings
            SwizzleLog.LogInfo("Interlaced image defaults to output to Display2");
            mytargetScreenIndex = 1;
            if (Display.displays.Length > 1)
                Display.displays[1].Activate();
            if (Display.displays.Length > 2)
                Display.displays[2].Activate();
        }

        private void ConfigureViewParameters()
        {
            // Update loading parameters
            DeviceDataManager.Instance.InitDeviceData(this);

            string jsonPath = Path.Combine(Application.streamingAssetsPath, "eye_tracking_x0.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    EyeTrackingConfigWrapper config = JsonConvert.DeserializeObject<EyeTrackingConfigWrapper>(jsonContent);
                    if (config != null && config.grating_params != null)
                    {
                        _device.x0 = config.grating_params.x0;
                        _device.interval = config.grating_params.interval;
                        _device.slope = config.grating_params.slope;
                    }
                }
                catch (Exception e)
                {
                    SwizzleLog.LogError($"Failed to read eye_tracking_x0.json: {e.Message}");
                }
            }
            _eye.fovX = _device.cam_fov_x;
            isSBS = false;

            mycam_tan = MathF.Tan((_eye.fovX / 2) * Mathf.Deg2Rad);
            mysrc_tan = MathF.Tan((_device.interval_fov / 2) * Mathf.Deg2Rad);
            virtcam_viewnum = _device.total_viewnum / MathF.Tan((29.5f) * Mathf.Deg2Rad) * mysrc_tan;
            fl = 1f / (2f * _device.tan_alpha_2);

            _device.imgs_count_x = 1;
            _device.imgs_count_y = 2;
            _device.total_viewnum = 2;
            _device.total_fov = _device.sbs_fov;
        }

        private void OnDestroy()
        {
            if (_eye != null)
            {
                _eye.OnX0ArrayAvailable -= HandleX0ArrayUpdate;
            }
            UpdateManager.Instance.UnregisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.BatchCamera);
        }

        private void HandleX0ArrayUpdate(float[] data)
        {
            if (data != null && X0Array != null && data.Length == X0Array.Length)
            {
                Array.Copy(data, X0Array, data.Length);
            }
        }

        private void InitModeManagement()
        {
            // Set initial mode
            SetAppMode(AppMode.Default);

            // Bind unified back button event
            if (BackButton != null)
            {
                BackButton.onClick.AddListener(HandleUniversalBack);
            }

            // Bind mode switch buttons
            if (sbs_videoManager != null && sbs_videoManager.SBS_FileLoad != null)
            {
                // Remove existing listeners to avoid duplicates
                sbs_videoManager.SBS_FileLoad.onClick.RemoveAllListeners();
                sbs_videoManager.SBS_FileLoad.onClick.AddListener(() =>
                {
                    SetAppMode(AppMode.SBSVideo);
                    sbs_videoManager.VideoLoad();
                });
            }

            if (MultipleView_FileLoad != null)
            {
                MultipleView_FileLoad.onClick.RemoveAllListeners();
                MultipleView_FileLoad.onClick.AddListener(() =>
                {
                    SetAppMode(AppMode.MultipleView);
                    StartMultipleViewVideo();
                });
            }
        }

        // Set application mode
        public void SetAppMode(AppMode mode)
        {
            currentMode = mode;

            // Update back button state
            if (BackButton != null)
            {
                BackButton.gameObject.SetActive(mode != AppMode.Default);
            }

            UpdateModeButtonsInteractable(mode);
        }

        // Update mode button interactability
        private void UpdateModeButtonsInteractable(AppMode currentMode)
        {
            // Set other mode buttons interactability based on current mode
            bool enableButtons = (currentMode == AppMode.Default);

            // Set SBS video button
            if (sbs_videoManager != null && sbs_videoManager.SBS_FileLoad != null)
            {
                sbs_videoManager.SBS_FileLoad.interactable = enableButtons || currentMode == AppMode.SBSVideo;
            }

            if (MultipleView_FileLoad != null)
            {
                MultipleView_FileLoad.interactable = enableButtons || currentMode == AppMode.MultipleView;
            }
        }

        // Unified back handling
        public void HandleUniversalBack()
        {
            switch (currentMode)
            {
                case AppMode.SBSVideo:
                    if (sbs_videoManager != null)
                    {
                        sbs_videoManager.VideoBack();
                    }
                    break;

                case AppMode.MultipleView:
                    MultipleViewBack();
                    break;
            }

            // Return to default mode
            SetAppMode(AppMode.Default);
        }
        #endregion

        #region Unity_Update Methods
        private void SwizzleUpdate()
        {
            // Skip update if reinitializing
            if (isReinitializing)
                return;

            if (currentMode == AppMode.MultipleView)
            {
                if (!isMultipleViewLoading)
                {
                    // Update EyePos in MultipleView mode to respond to K value adjustment
                    UpdateEyePos();
                    UpdateMultipleViewShaderParams();
                }
                return;
            }

            if (isMultipleViewLoading)
                return;

            // New: Handle keyboard input for is2views mode
            // HandleIs2ViewsKeyboardInput();
            UpdateEyeOffset();

            // Update target and view
            UpdateTarget();
            UpdateEyePos();

            // Rendering update
            UpdateCameraPositions();
            UpdateTextureArray();

            // Debug visualization update
            visualDebugger.ManualUpdate();

            // Latency monitor update
            UpdateShader(); // UpdateShader handles the start of latency tracking logic (applied seq)
            latencyMonitor.ManualUpdate();
        }

        #endregion

        #region Device Type / View Mode Switching
        /// <summary>
        /// Toggle view boundary display
        /// </summary>
        private void ToggleViewBoundary()
        {
            isShowingViewBoundary = !isShowingViewBoundary;
            if (_batchMaterial != null)
            {
                _batchMaterial.SetFloat("_ShowBoundary", isShowingViewBoundary ? 1.0f : 0.0f);
            }
        }
        /// <summary>
        /// Update view boundary display button visibility
        /// </summary>
        private void UpdateViewBoundaryButtonVisibility()
        {
            if (showViewBoundaryButton != null)
            {
                showViewBoundaryButton.gameObject.SetActive(true);
            }
        }
        #endregion

        #region Render Textures and Shaders
        private void InitRenderTexture()
        {
            SwizzleLog.LogImportant($"---imgs_count_xy:{_device.imgs_count_x}---{_device.imgs_count_y}---");

            // Create texture array and camera render textures
            cameraRenderTextures = new RenderTexture[_device.total_viewnum];
            textureArray = new Texture2DArray((int)_device.subimg_width, (int)_device.subimg_height, _device.total_viewnum, TextureFormat.RGBA32, false);

            // Create separate RenderTexture for each camera
            for (int i = 0; i < _device.total_viewnum; i++)
            {
                cameraRenderTextures[i] = new RenderTexture((int)_device.subimg_width, (int)_device.subimg_height, 24);
                cameraRenderTextures[i].Create();
            }
        }


        private void LoadSwizzleShader()
        {
            // Select material based on is2views
            string materialName;
            if (_device.pixel_order == "rgb_012")
            {
                materialName = "MultiView_sbs_012";
            }
            else if (_device.pixel_order == "rgb_210")
            {
                materialName = "MultiView_sbs_210";
            }
            else
            {
                SwizzleLog.LogError("Configuration parameter name error, unable to read material normally");
                return;
            }

            if (_device.is27Layout)
            {
                materialName = "MultiView_sbs_012_27";
            }
            Material loadedMaterial = Resources.Load<Material>(materialName);

            if (loadedMaterial != null)
            {
                _batchMaterial = loadedMaterial;

                _batchMaterial.SetFloat("_Slope", _device.slope);
                _batchMaterial.SetFloat("_Interval", _device.interval);
                _batchMaterial.SetFloat("_X0", _device.x0);

                _batchMaterial.SetFloat("_Gamma", 1.0f);
                _batchMaterial.SetFloat("_OutputSizeX", _device.output_size_X);
                _batchMaterial.SetFloat("_OutputSizeY", _device.output_size_Y);
                _batchMaterial.SetTexture("_TextureArray", textureArray);

                // 27-inch special parameters
                // 27-inch special parameters (only set under 27 layout)
                if (_device.is27Layout)
                {
                    _batchMaterial.SetFloat("_RowNum", _device.rowNum);
                    _batchMaterial.SetFloat("_ColNum", _device.colNum);
                }

            }
            else
            {
                SwizzleLog.LogWarning($"Material not found: {materialName}, outputting grid image");
            }
        }
        #endregion

        #region Interlace Parameter Adjustment
        private void InitAdjustK()
        {
            RemoveKValueListeners();
            InitializePrecisionDropdowns();
            SetKValue(_device.x0, _device.interval, _device.slope);
            InitializeExtendedParameters();

            // Adjust X0     
            k1_slider.onValueChanged.AddListener(Updatek1);
            // Adjust interval 
            k2_slider.onValueChanged.AddListener(Updatek2);
            // Adjust Slope 
            k3_slider.onValueChanged.AddListener(Updatek3);

            k1_text.onEndEdit.AddListener(OnK1TextChanged);
            k2_text.onEndEdit.AddListener(OnK2TextChanged);
            k3_text.onEndEdit.AddListener(OnK3TextChanged);

            // Bind extended parameter input field events
            if (interval_fov_text != null)
                interval_fov_text.onEndEdit.AddListener(OnIntervalFovTextChanged);
            if (delta_pos_text != null)
                delta_pos_text.onEndEdit.AddListener(OnDeltaPosTextChanged);
            if (cam_fov_x_text != null)
                cam_fov_x_text.onEndEdit.AddListener(OnCamFovXTextChanged);

            k1PrecisionDropdown.onValueChanged.AddListener(OnK1PrecisionChanged);
            k2PrecisionDropdown.onValueChanged.AddListener(OnK2PrecisionChanged);
            k3PrecisionDropdown.onValueChanged.AddListener(OnK3PrecisionChanged);
        }

        private void InitializePrecisionDropdowns()
        {
            // Check if dropdowns are assigned
            if (k1PrecisionDropdown == null || k2PrecisionDropdown == null || k3PrecisionDropdown == null)
            {
                SwizzleLog.LogWarning("Precision control dropdowns not assigned, using default precision");
                return;
            }

            // Clear default options
            k1PrecisionDropdown.ClearOptions();
            k2PrecisionDropdown.ClearOptions();
            k3PrecisionDropdown.ClearOptions();

            // Add precision options
            k1PrecisionDropdown.AddOptions(precisionOptions);
            k2PrecisionDropdown.AddOptions(precisionOptions);
            k3PrecisionDropdown.AddOptions(precisionOptions);

            // Set default options
            k1PrecisionDropdown.value = currentK1PrecisionIndex;
            k2PrecisionDropdown.value = currentK2PrecisionIndex;
            k3PrecisionDropdown.value = currentK3PrecisionIndex;
        }

        // Initialize save button
        private void InitSaveButton()
        {
            if (saveParametersButton != null)
            {
                saveParametersButton.onClick.RemoveAllListeners();
                saveParametersButton.onClick.AddListener(SaveCurrentParameters);
            }

            // Initialize reset parameters button
            if (resetParametersButton != null)
            {
                resetParametersButton.onClick.RemoveAllListeners();
                resetParametersButton.onClick.AddListener(ResetParameters);
            }
        }

        // Save current parameters
        private void SaveCurrentParameters()
        {
            // Save currently adjusted parameters
            DeviceDataManager.Instance.SaveCurrentDeviceParameters(
            k1, k2, k3,
            _device.interval_fov, _device.delta_pos, _device.cam_fov_x,
            // 27-inch parameters
            _device.is27Layout, 0f, 0f, 0f, 0f, null);

            SwizzleLog.LogImportant($"Saved current parameter settings: x0={k1}, interval={k2}, slope={k3}");
        }

        private void ResetParameters()
        {
            if (isSBS)
            {
                SwizzleLog.LogError("Cannot reset parameters while playing video");
                return;
            }

            // Reset to default configuration
            DeviceDataManager.Instance.ResetCurrentDeviceParameters();

            // Reload device data
            ConfigureViewParameters();

            // Update UI display (existing k1, k2, k3 logic)
            UpdateKParametersUI();

            // Update UI display for new parameters
            UpdateExtendedParametersUI();

            SwizzleLog.LogInfo("Parameters reset to default values");
        }

        private void UpdateKParametersUI()
        {
            SetKValue(_device.x0, _device.interval, _device.slope);
        }

        private void UpdateExtendedParametersUI()
        {
            if (interval_fov_text != null)
                interval_fov_text.text = _device.interval_fov.ToString("F2");

            if (delta_pos_text != null)
                delta_pos_text.text = _device.delta_pos.ToString("F4");

            if (cam_fov_x_text != null)
                cam_fov_x_text.text = _device.cam_fov_x.ToString("F1");
        }

        private void RemoveKValueListeners()
        {
            k1_slider.onValueChanged.RemoveAllListeners();
            k2_slider.onValueChanged.RemoveAllListeners();
            k3_slider.onValueChanged.RemoveAllListeners();

            k1_text.onEndEdit.RemoveAllListeners();
            k2_text.onEndEdit.RemoveAllListeners();
            k3_text.onEndEdit.RemoveAllListeners();

            // Remove extended parameter listeners
            if (interval_fov_text != null)
                interval_fov_text.onEndEdit.RemoveAllListeners();
            if (delta_pos_text != null)
                delta_pos_text.onEndEdit.RemoveAllListeners();
            if (cam_fov_x_text != null)
                cam_fov_x_text.onEndEdit.RemoveAllListeners();

            k1PrecisionDropdown.onValueChanged.RemoveAllListeners();
            k2PrecisionDropdown.onValueChanged.RemoveAllListeners();
            k3PrecisionDropdown.onValueChanged.RemoveAllListeners();
        }


        private void SetKValue(float K1, float K2, float K3)
        {
            k1 = K1;
            suppressKSliderCallback = true;
            UpdateSliderRange(k1_slider, k1, currentK1PrecisionIndex);
            k1_slider.SetValueWithoutNotify(k1);
            suppressKSliderCallback = false;

            k2 = K2;
            suppressKSliderCallback = true;
            UpdateSliderRange(k2_slider, k2, currentK2PrecisionIndex);
            k2_slider.SetValueWithoutNotify(k2);
            suppressKSliderCallback = false;

            k3 = K3;
            suppressKSliderCallback = true;
            UpdateSliderRange(k3_slider, k3, currentK3PrecisionIndex);
            k3_slider.SetValueWithoutNotify(k3);
            suppressKSliderCallback = false;

            UpdatekText();
        }

        private void UpdateSliderRange(Slider slider, float currentValue, int precisionIndex)
        {
            float precision = precisionValues[precisionIndex];
            float range = precision * 100; // Set range based on precision

            slider.minValue = currentValue - range;
            slider.maxValue = currentValue + range;
        }

        private void OnK1PrecisionChanged(int index)
        {
            currentK1PrecisionIndex = index;
            UpdateSliderRange(k1_slider, k1, index);
            UpdatekText();
        }

        private void OnK2PrecisionChanged(int index)
        {
            currentK2PrecisionIndex = index;
            UpdateSliderRange(k2_slider, k2, index);
            UpdatekText();
        }

        private void OnK3PrecisionChanged(int index)
        {
            currentK3PrecisionIndex = index;
            UpdateSliderRange(k3_slider, k3, index);
            UpdatekText();
        }

        private void Updatek1(float value)
        {
            if (suppressKSliderCallback) return;
            k1 = value;
            UpdatekText();
        }

        private void Updatek2(float value)
        {
            if (suppressKSliderCallback) return;
            k2 = value;
            UpdatekText();
        }

        private void Updatek3(float value)
        {
            if (suppressKSliderCallback) return;
            k3 = value;
            UpdatekText();
        }

        private void UpdatekText()
        {
            string k1Format = currentK1PrecisionIndex < precisionFormats.Count ? precisionFormats[currentK1PrecisionIndex] : "F3";
            string k2Format = currentK2PrecisionIndex < precisionFormats.Count ? precisionFormats[currentK2PrecisionIndex] : "F3";
            string k3Format = currentK3PrecisionIndex < precisionFormats.Count ? precisionFormats[currentK3PrecisionIndex] : "F3";

            k1_text.SetTextWithoutNotify(k1.ToString(k1Format, CultureInfo.InvariantCulture));
            k2_text.SetTextWithoutNotify(k2.ToString(k2Format, CultureInfo.InvariantCulture));
            k3_text.SetTextWithoutNotify(k3.ToString(k3Format, CultureInfo.InvariantCulture));
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                   || float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        private void OnK1TextChanged(string value)
        {
            if (TryParseFloat(value, out float newValue))
            {
                k1 = newValue;
                suppressKSliderCallback = true;
                UpdateSliderRange(k1_slider, k1, currentK1PrecisionIndex);
                k1_slider.SetValueWithoutNotify(k1);
                suppressKSliderCallback = false;
                UpdatekText();

                // Update material parameters
                if (_batchMaterial != null)
                    _batchMaterial.SetFloat("_X0", k1);
            }
            else
            {
                UpdatekText();
            }
        }

        private void OnK2TextChanged(string value)
        {
            if (TryParseFloat(value, out float newValue))
            {
                k2 = newValue;
                suppressKSliderCallback = true;
                UpdateSliderRange(k2_slider, k2, currentK2PrecisionIndex);
                k2_slider.SetValueWithoutNotify(k2);
                suppressKSliderCallback = false;
                UpdatekText();

                // Update material parameters
                if (_batchMaterial != null)
                    _batchMaterial.SetFloat("_Interval", k2);
            }
            else
            {
                UpdatekText();
            }
        }

        private void OnK3TextChanged(string value)
        {
            if (TryParseFloat(value, out float newValue))
            {
                k3 = newValue;
                suppressKSliderCallback = true;
                UpdateSliderRange(k3_slider, k3, currentK3PrecisionIndex);
                k3_slider.SetValueWithoutNotify(k3);
                suppressKSliderCallback = false;
                UpdatekText();

                // Update material parameters
                if (_batchMaterial != null)
                    _batchMaterial.SetFloat("_Slope", k3);
            }
            else
            {
                UpdatekText();
            }
        }

        /// <summary>
        /// Initialize the display value of extended parameter input box
        /// </summary>
        private void InitializeExtendedParameters()
        {
            if (interval_fov_text != null)
                interval_fov_text.text = _device.interval_fov.ToString("F2");
            if (delta_pos_text != null)
                delta_pos_text.text = _device.delta_pos.ToString("F4");
            if (cam_fov_x_text != null)
                cam_fov_x_text.text = _device.cam_fov_x.ToString("F1");
        }

        /// <summary>
        /// interval_fov parameter change handling
        /// </summary>
        private void OnIntervalFovTextChanged(string value)
        {
            if (float.TryParse(value, out float newValue))
            {
                _device.interval_fov = newValue;
                SwizzleLog.LogInfo($"interval_fov updated to: {newValue}");
                // Recalculate related parameters
                RecalculateParameters();
            }
            else
            {
                // Parse failed, restore original value
                interval_fov_text.text = _device.interval_fov.ToString("F3");
                SwizzleLog.LogWarning("Invalid interval_fov input, original value restored");
            }
        }

        /// <summary>
        /// delta_pos parameter change handling
        /// </summary>
        private void OnDeltaPosTextChanged(string value)
        {
            if (float.TryParse(value, out float newValue))
            {
                _device.delta_pos = newValue;
                SwizzleLog.LogInfo($"delta_pos updated to: {newValue}");
            }
            else
            {
                // Parse failed, restore original value
                delta_pos_text.text = _device.delta_pos.ToString("F4");
                SwizzleLog.LogWarning("Invalid delta_pos input, original value restored");
            }
        }



        /// <summary>
        /// cam_fov_x parameter change handling
        /// </summary>
        private void OnCamFovXTextChanged(string value)
        {
            if (float.TryParse(value, out float newValue))
            {
                _device.cam_fov_x = newValue;
                // Update EyeTrackingManager fovX
                _eye.fovX = newValue;
                SwizzleLog.LogInfo($"cam_fov_x updated to: {newValue}");
                // Recalculate related parameters
                RecalculateParameters();
            }
            else
            {
                // Parse failed, restore original value
                cam_fov_x_text.text = _device.cam_fov_x.ToString("F1");
                SwizzleLog.LogWarning("Invalid cam_fov_x input, original value restored");
            }
        }

        /// <summary>
        /// Recalculate related parameters (when cam_fov_x or interval_fov changes)
        /// </summary>
        private void RecalculateParameters()
        {
            // Recalculate mycam_tan (needed when cam_fov_x changes)
            _eye.fovX = _device.cam_fov_x;
            mycam_tan = MathF.Tan((_eye.fovX / 2) * Mathf.Deg2Rad);

            // Recalculate mysrc_tan
            mysrc_tan = MathF.Tan((_device.interval_fov / 2) * Mathf.Deg2Rad);

            // Recalculate virtcam_viewnum
            virtcam_viewnum = _device.total_viewnum / MathF.Tan((29.5f) * Mathf.Deg2Rad) * mysrc_tan;

            SwizzleLog.LogInfo($"Parameters recalculation completed -_eye.fovX:{_eye.fovX}, mycam_tan: {mycam_tan}, mysrc_tan: {mysrc_tan}, virtcam_viewnum: {virtcam_viewnum}");
        }
        #endregion

        #region Functions required for SBS video processing
        public Camera[] GetBatchCameras()
        {
            return _batchcameras;
        }

        public void SetSBSMode(bool mode)
        {
            isSBS = mode;
        }
        #endregion

        #region Multi-view video mode
        private void StartMultipleViewVideo()
        {
            string videoPath = OpenFile.ChooseVideo();
            if (string.IsNullOrEmpty(videoPath))
            {
                SetAppMode(AppMode.Default);
                return;
            }

            if (multipleViewLoadRoutine != null)
            {
                StopCoroutine(multipleViewLoadRoutine);
                multipleViewLoadRoutine = null;
            }

            CleanupMultipleViewResources();
            multipleViewLoadRoutine = StartCoroutine(LoadMultipleViewVideoFrames(videoPath));
        }

        private IEnumerator LoadMultipleViewVideoFrames(string videoPath)
        {
            isMultipleViewLoading = true;

            PrepareMultipleViewPlayer(videoPath);
            float prepareStart = Time.realtimeSinceStartup;
            while (multipleViewVideoPlayer != null && !multipleViewVideoPlayer.isPrepared)
            {
                if (Time.realtimeSinceStartup - prepareStart > 5f)
                {
                    SwizzleLog.LogError("[MultipleView] Video preparation timed out, stop loading");
                    CleanupMultipleViewResources();
                    SetAppMode(AppMode.Default);
                    isMultipleViewLoading = false;
                    yield break;
                }
                yield return null;
            }

            const int width = 2560;
            const int height = 1440;
            const int frameCount = 60;

            multipleViewTextureArray = new Texture2DArray(width, height, frameCount, TextureFormat.RGBA32, false, false);
            multipleViewTextureArray.wrapMode = TextureWrapMode.Clamp;
            multipleViewTextureArray.filterMode = FilterMode.Bilinear;

            Texture2D tempTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);

            if (multipleViewVideoPlayer != null)
            {
                multipleViewVideoPlayer.sendFrameReadyEvents = true;
                multipleViewVideoPlayer.frameReady += OnMultipleViewFrameReady;
                for (int i = 0; i < frameCount; i++)
                {
                    multipleViewFrameReady = false;
                    multipleViewReadyFrame = -1;
                    multipleViewVideoPlayer.frame = i;
                    multipleViewVideoPlayer.Play();
                    float frameWaitStart = Time.realtimeSinceStartup;
                    while (!multipleViewFrameReady || multipleViewReadyFrame != i)
                    {
                        if (Time.realtimeSinceStartup - frameWaitStart > 2f)
                        {
                            SwizzleLog.LogWarning($"[MultipleView] Frame {i + 1} wait timed out, continue reading current frame");
                            break;
                        }
                        yield return null;
                    }
                    multipleViewVideoPlayer.Pause();
                    yield return new WaitForEndOfFrame();

                    RenderTexture active = RenderTexture.active;
                    RenderTexture.active = multipleViewRenderTexture;
                    tempTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tempTexture.Apply();
                    RenderTexture.active = active;

                    Graphics.CopyTexture(tempTexture, 0, 0, multipleViewTextureArray, i, 0);
                    SwizzleLog.LogInfo($"[MultipleView] Frame {i + 1}/{frameCount} read");
                }
                multipleViewVideoPlayer.frameReady -= OnMultipleViewFrameReady;
                multipleViewVideoPlayer.Stop();
            }

            Destroy(tempTexture);
            ApplyMultipleViewMaterial();

            isMultipleViewLoading = false;
        }

        private void PrepareMultipleViewPlayer(string videoPath)
        {
            if (multipleViewVideoObject == null)
            {
                multipleViewVideoObject = new GameObject("MultipleViewVideoPlayer");
                multipleViewVideoObject.transform.SetParent(transform, false);
            }

            if (multipleViewVideoPlayer == null)
            {
                multipleViewVideoPlayer = multipleViewVideoObject.AddComponent<VideoPlayer>();
            }

            if (multipleViewRenderTexture == null)
            {
                multipleViewRenderTexture = new RenderTexture(2560, 1440, 0, RenderTextureFormat.ARGB32);
                multipleViewRenderTexture.Create();
            }

            multipleViewVideoPlayer.playOnAwake = false;
            multipleViewVideoPlayer.isLooping = false;
            multipleViewVideoPlayer.waitForFirstFrame = true;
            multipleViewVideoPlayer.skipOnDrop = false;
            multipleViewVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            multipleViewVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            multipleViewVideoPlayer.targetTexture = multipleViewRenderTexture;
            multipleViewVideoPlayer.url = videoPath;
            multipleViewVideoPlayer.Prepare();
        }

        private void ApplyMultipleViewMaterial()
        {
            Material multiViewMaterial = Resources.Load<Material>("MultiView_mul_012");
            if (multiViewMaterial == null)
            {
                SwizzleLog.LogError("MultiView_mul_012 material not found");
                SetAppMode(AppMode.Default);
                CleanupMultipleViewResources();
                return;
            }

            _batchMaterial = multiViewMaterial;
            _batchMaterial.SetTexture("_TextureArray", multipleViewTextureArray);
            _batchMaterial.SetFloat("_Slope", _device.slope);
            _batchMaterial.SetFloat("_Interval", _device.interval);
            _batchMaterial.SetFloat("_X0", _device.x0);
            _batchMaterial.SetFloat("_OutputSizeX", _device.output_size_X);
            _batchMaterial.SetFloat("_OutputSizeY", _device.output_size_Y);
            _batchMaterial.SetFloat("_RowNum", _device.rowNum);
            _batchMaterial.SetFloat("_ColNum", _device.colNum);
            _batchMaterial.SetFloatArray("_X0Array", X0Array);
            UpdateMultipleViewShaderParams();

            if (BatchImage != null)
            {
                BatchImage.material = _batchMaterial;
            }
        }

        private void UpdateMultipleViewShaderParams()
        {
            if (_batchMaterial == null)
                return;

            float imgCount = multipleViewTextureArray != null ? multipleViewTextureArray.depth : 60f;
            float imgFovHalf = 59f * 0.5f;
            float camFovHalf = _device.cam_fov_x * 0.5f;
            float intFovHalf = _device.interval_fov * 0.5f;

            float tanImgFov = Mathf.Tan(imgFovHalf * Mathf.Deg2Rad);
            float tanCamFov = Mathf.Tan(camFovHalf * Mathf.Deg2Rad);
            float tanIntFov = Mathf.Tan(intFovHalf * Mathf.Deg2Rad);

            float camX = _eye != null ? _eye.AveragePos().x : 0.5f;
            float centerX = 0.5f;

            float deltaPos = _eye != null ? _eye.DeltaPos() : _device.delta_pos;
            float delta700Pos = _device.delta_700_pos;
            float camZ0 = 700f;
            float camZ = deltaPos > 0.0001f ? camZ0 * delta700Pos / deltaPos : camZ0;

            float screenW = _device.oc_size_x > 0 ? _device.oc_size_x : _device.output_size_X;

            float horizon_choice = (camX - centerX) * (tanCamFov / tanImgFov) * (imgCount - 1f) + (imgCount - 1f) * 0.5f;
            float interval_view_count = (tanIntFov / tanImgFov) * (imgCount - 1f);
            float delta_vertical_choice_factor = (screenW / camZ0) * (camZ0 / camZ - 1f) * (imgCount - 1f) / (2f * tanImgFov);

            SwizzleLog.LogInfo($"[MultipleView] imgCount:{imgCount:F1} imgFovHalf:{imgFovHalf:F3} camFovHalf:{camFovHalf:F3} intFovHalf:{intFovHalf:F3} tanImg:{tanImgFov:F6} tanCam:{tanCamFov:F6} tanInt:{tanIntFov:F6} camX:{camX:F4} centerX:{centerX:F4} deltaPos:{deltaPos:F6} delta700Pos:{delta700Pos:F6} camZ0:{camZ0:F3} camZ:{camZ:F3} screenW:{screenW:F3} horizon_choice:{horizon_choice:F3} interval_view_count:{interval_view_count:F3} delta_vertical_choice_factor:{delta_vertical_choice_factor:F6}");

            _batchMaterial.SetFloat("_horizon_choice", horizon_choice);
            _batchMaterial.SetFloat("_delta_vertical_choice_factor", delta_vertical_choice_factor);
            _batchMaterial.SetFloat("_ViewCount", interval_view_count);
            _batchMaterial.SetFloat("_ImgCount", imgCount);
        }

        private void OnMultipleViewFrameReady(VideoPlayer source, long frameIdx)
        {
            multipleViewFrameReady = true;
            multipleViewReadyFrame = frameIdx;
        }

        private void MultipleViewBack()
        {
            if (multipleViewLoadRoutine != null)
            {
                StopCoroutine(multipleViewLoadRoutine);
                multipleViewLoadRoutine = null;
            }

            isMultipleViewLoading = false;
            CleanupMultipleViewResources();

            if (defaultBatchMaterial != null)
            {
                _batchMaterial = defaultBatchMaterial;
                _batchMaterial.SetTexture("_TextureArray", textureArray);
                if (BatchImage != null)
                {
                    BatchImage.material = _batchMaterial;
                }
            }
        }

        private void CleanupMultipleViewResources()
        {
            if (multipleViewVideoPlayer != null)
            {
                multipleViewVideoPlayer.frameReady -= OnMultipleViewFrameReady;
                multipleViewVideoPlayer.Stop();
            }

            if (multipleViewVideoObject != null)
            {
                Destroy(multipleViewVideoObject);
                multipleViewVideoObject = null;
                multipleViewVideoPlayer = null;
            }

            if (multipleViewRenderTexture != null)
            {
                multipleViewRenderTexture.Release();
                Destroy(multipleViewRenderTexture);
                multipleViewRenderTexture = null;
            }

            if (multipleViewTextureArray != null)
            {
                Destroy(multipleViewTextureArray);
                multipleViewTextureArray = null;
            }
        }
        #endregion

        #region Target and Camera Initialization
        private void InitTarget()
        {
            // Initialize Root
            Root = new GameObject("Root").transform;
            DontDestroyOnLoad(Root);
            // Initialize Target
            Target = new GameObject("Target").transform;
            DontDestroyOnLoad(Target);
        }

        private void InitCamera()
        {
            _batchcameras = new Camera[_device.total_viewnum];
            _batchcameras_angles = new float[_device.total_viewnum];
            AngleText = new TextMeshProUGUI[_device.total_viewnum];
            isCameraActive = new bool[_device.total_viewnum];

            for (int i = 0; i < _device.total_viewnum; i++)
            {
                // Use prefab or create new camera
                GameObject cameraObj;
                if (useCameraPrefab && BatchCameraPrefab != null)
                {
                    cameraObj = Instantiate(BatchCameraPrefab.gameObject);
                    cameraObj.name = $"_BatchCameraPrefab[{i}]";
                    _batchcameras[i] = cameraObj.GetComponent<Camera>();
                }
                else
                {
                    cameraObj = new GameObject($"_BatchCamera[{i}]");
                    _batchcameras[i] = cameraObj.AddComponent<Camera>();
                }
                DontDestroyOnLoad(cameraObj);

                // Physical camera enabled
                _batchcameras[i].usePhysicalProperties = true;
                _batchcameras[i].enabled = true;

                // Prevent data size overflow, scale by 100 times
                _batchcameras[i].focalLength = _device.f_cam / 100.0f;
                _batchcameras[i].sensorSize = new Vector2(_device.subimg_width / 100.0f, _device.subimg_height / 100.0f);

                // Camera image distribution calculation - Modified to use separate RenderTexture
                _batchcameras[i].targetTexture = cameraRenderTextures[i];
                _batchcameras[i].rect = new Rect(0, 0, 1, 1);

                isCameraActive[i] = true;

                if (onlyShowSerial)
                {
                    // Ensure a layer named "UI_Layer" is created

                    // Create Canvas for each camera
                    GameObject canvasObj = new GameObject($"Canvas_{i}");
                    canvasObj.transform.SetParent(cameraObj.transform);
                    canvasObj.layer = uiLayer; // Set Canvas layer
                    Canvas canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = _batchcameras[i];
                    canvas.sortingLayerName = "UI"; // Ensure Canvas sortingLayerName is UI

                    // Set Canvas scaling mode
                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(_device.subimg_width, _device.subimg_height);

                    // Add GraphicRaycaster component
                    canvasObj.AddComponent<GraphicRaycaster>();

                    // Create black background image
                    GameObject imageObj = new GameObject($"BlackBackground_{i}");
                    imageObj.transform.SetParent(canvasObj.transform, false);
                    imageObj.layer = uiLayer; // Set black background image layer
                    Image blackImage = imageObj.AddComponent<Image>();
                    blackImage.color = Color.white;

                    RectTransform imageRectTransform = imageObj.GetComponent<RectTransform>();
                    // Calculate width and height
                    float width = _device.subimg_width / _device.imgs_count_x;
                    float height = _device.subimg_height / _device.imgs_count_y;
                    // Set image anchoredPosition
                    int quiltRow = i / _device.imgs_count_y;
                    int quiltCol = i % _device.imgs_count_y;
                    float xPos = width / 2 - _device.subimg_width / 2 + quiltRow * width;
                    float yPos = _device.subimg_height / 2 - height / 2 - quiltCol * height;
                    imageRectTransform.anchoredPosition = new Vector2(xPos, yPos);
                    // Set image sizeDelta
                    imageRectTransform.sizeDelta = new Vector2(width, height);

                    // Create number text
                    GameObject textObj = new GameObject($"CameraText_{i}");
                    textObj.transform.SetParent(imageObj.transform, false);
                    textObj.layer = uiLayer; // Set number text layer

                    // Add TextMeshProUGUI component to show number
                    TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                    tmpText.text = i.ToString();
                    tmpText.fontSize = 60;
                    tmpText.fontStyle = TMPro.FontStyles.Bold;
                    tmpText.alignment = TMPro.TextAlignmentOptions.Center;
                    tmpText.color = Color.black;

                    // Set number text RectTransform
                    RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
                    textRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    textRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    textRectTransform.sizeDelta = new Vector2(100, 60);
                    textRectTransform.anchoredPosition = new Vector2(0, 50);

                    // Create angle text
                    GameObject angleTextObj = new GameObject($"CameraAngleText_{i}");
                    angleTextObj.transform.SetParent(imageObj.transform, false);
                    angleTextObj.layer = uiLayer; // Set angle text layer

                    // Add TextMeshProUGUI component to show angle
                    AngleText[i] = angleTextObj.AddComponent<TMPro.TextMeshProUGUI>();
                    AngleText[i].text = "0.00";
                    AngleText[i].fontSize = 30;
                    AngleText[i].fontStyle = TMPro.FontStyles.Bold;
                    AngleText[i].alignment = TMPro.TextAlignmentOptions.Center;
                    AngleText[i].color = Color.black;

                    // Set angle text RectTransform
                    RectTransform angleTextRectTransform = angleTextObj.GetComponent<RectTransform>();
                    angleTextRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    angleTextRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    angleTextRectTransform.sizeDelta = new Vector2(120, 60);
                    angleTextRectTransform.anchoredPosition = new Vector2(0, -50);

                    // Only let child camera see UI layer
                    _batchcameras[i].cullingMask = 1 << uiLayer;

                    // Set background color (if needed)
                    _batchcameras[i].backgroundColor = Color.black;
                }

                else if (showSerial && !onlyShowSerial)
                {
                    // Create Canvas for each camera
                    GameObject canvasObj = new GameObject($"Canvas_{i}");
                    canvasObj.transform.SetParent(cameraObj.transform);
                    Canvas canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = _batchcameras[i];
                    // Set Canvas scaling mode
                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(_device.subimg_width, _device.subimg_height);
                    // Add GraphicRaycaster component
                    canvasObj.AddComponent<GraphicRaycaster>();
                    // Create TextMeshPro text
                    GameObject textObj = new GameObject($"CameraText_{i}");
                    textObj.transform.SetParent(canvasObj.transform, false);
                    GameObject textObj2 = new GameObject($"CameraangleText_{i}");
                    textObj2.transform.SetParent(canvasObj.transform, false);

                    // Add TextMeshProUGUI component
                    TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                    tmpText.text = i.ToString();
                    tmpText.fontSize = 36;
                    tmpText.fontStyle = TMPro.FontStyles.Bold;
                    tmpText.alignment = TMPro.TextAlignmentOptions.Center;
                    // Set RectTransform
                    RectTransform rectTransform = textObj.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(60, 60);

                    // Add TextMeshProUGUI component
                    AngleText[i] = textObj2.AddComponent<TMPro.TextMeshProUGUI>();
                    AngleText[i].text = _batchcameras_angles[i].ToString("F2");
                    AngleText[i].fontSize = 24;
                    AngleText[i].fontStyle = TMPro.FontStyles.Bold;
                    AngleText[i].alignment = TMPro.TextAlignmentOptions.Center;
                    // Set RectTransform
                    RectTransform rectTransform2 = textObj2.GetComponent<RectTransform>();
                    rectTransform2.sizeDelta = new Vector2(120, 60);

                    // Calculate position
                    int quiltRow = i / _device.imgs_count_y;
                    int quiltCol = i % _device.imgs_count_y;

                    float xPos = 50 - _device.subimg_width / 2 + quiltRow * 60;
                    float yPos = _device.subimg_height / 2 - 20 - quiltCol * 120;

                    rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                    rectTransform2.anchoredPosition = new Vector2(xPos, yPos - 60);
                }
            }
        }

        private void InitDisplayCamera()
        {
            // Create a new camera to display the frame
            GameObject displayCameraObj = new GameObject("_DisplayCamera");
            DontDestroyOnLoad(displayCameraObj);

            displayCamera = displayCameraObj.AddComponent<Camera>();
            displayCamera.nearClipPlane = 0.5f;
            displayCamera.farClipPlane = 100.0f;
            displayCamera.targetDisplay = mytargetScreenIndex; // Set camera display target to secondary screen
            displayCamera.enabled = true;

            displayCamera.backgroundColor = Color.black; // Background color
            displayCamera.gameObject.transform.position = new Vector3(0f, 1000f, 0f);

            // Attach probe to display camera (supports Built-in and SRP)
            var probe = displayCamera.gameObject.GetComponent<RenderLatencyProbe>();
            if (probe == null)
            {
                probe = displayCamera.gameObject.AddComponent<RenderLatencyProbe>();
            }
            probe.Manager = this;

            GameObject canvasObj = new GameObject($"DisplayCanvas");
            canvasObj.transform.SetParent(displayCamera.transform);
            canvasObj.layer = uiLayer;
            displaycanvas = canvasObj.AddComponent<Canvas>();
            displaycanvas.renderMode = RenderMode.ScreenSpaceCamera;
            displaycanvas.worldCamera = displayCamera;
            displaycanvas.sortingLayerName = "UI";
            displaycanvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(_device.output_size_X, _device.output_size_Y);
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject imageObj = new GameObject($"Batch");
            imageObj.transform.SetParent(canvasObj.transform, false);
            imageObj.layer = uiLayer;
            BatchImage = imageObj.AddComponent<RawImage>();
            BatchImage.material = _batchMaterial;
            imageObj.GetComponent<RectTransform>().sizeDelta = new Vector2(_device.output_size_X, _device.output_size_Y);
        }
        #endregion

        #region Eye Tracking
        // Check if latency monitoring is enabled
        public bool IsLatencyMonitoringEnabled()
        {
            return _eye != null && _eye.monitorLatency;
        }

        private void UpdateShader()
        {
            if (_batchMaterial != null)
            {
                _batchMaterial.SetFloat("_Slope", _device.slope);
                _batchMaterial.SetFloat("_Interval", _device.interval);
                _batchMaterial.SetFloat("_X0", _device.x0);

                //SwizzleLog.LogImportant(X0Array[0].ToString());
                _batchMaterial.SetFloatArray("_X0Array", X0Array);
            }
        }

        private void UpdateEyePos()
        {
            _device.x0 = k1;
            _device.interval = k2;
            _device.slope = k3;

            if (_eye.cam0Detected && _eye.cam1Detected)
            {
                //SBSEyeTracking();
            }

            UpdateShader();
        }

        public void ToggleEyeTrackingOffset()
        {
            useEyeTrackingXOffset = !useEyeTrackingXOffset;
            if (!useEyeTrackingXOffset)
            {
                ResetEyeTrackingOffsets();
            }
        }

        public void ResetEyeTrackingOffsets()
        {
            eyeTrackingOffsetX = 0f;
            eyeTrackingOffsetY = 0f;
            eyeTrackingOffsetVelocity = 0f;
            eyeTrackingOffsetVelocityY = 0f;
        }

        private Vector2 GetEyeTrackingOffset()
        {
            float tanHalfFovX = Mathf.Tan(_device.cam_fov_x * 0.5f * Mathf.Deg2Rad);
            float tanHalfFovY = tanHalfFovX * (_device.subimg_height / _device.subimg_width);

            float worldOffsetX = eyeTrackingK * FocalPlane * eyeTrackingOffsetX * 2f * tanHalfFovX;
            float worldOffsetY = eyeTrackingK * FocalPlane * eyeTrackingOffsetY * 2f * tanHalfFovY;

            return new Vector2(worldOffsetX, worldOffsetY);
        }
        #endregion

        #region Target Update
        private void UpdateTarget()
        {
            if (useTartgetFocal && TargetTransform != null)
            {
                Target.position = TargetTransform.position;
                FocalPlane = Vector3.Distance(Root.position, Target.position);
            }
            else
            {
                Target.position = Root.position + Root.forward * FocalPlane;
            }

            if (RootTransfrom != null)
            {
                Root.position = RootTransfrom.position;
                Root.rotation = RootTransfrom.rotation;
            }
            else
            {
                Root.position = Camera.main.transform.position;
                Root.rotation = Camera.main.transform.rotation;
            }
        }

        private void UpdateCameraPositions()
        {
            if (isSBS)
            {
                return;
            }
            // Control camera position/rotation separately, for testing
            else
            {
                float x_fov = FocalPlane * Mathf.Tan(_device.total_fov / 2f * Mathf.Deg2Rad);
                //float cam_fov = FocalPlane * MathF.Tan((_eye.fovX / 2) * Mathf.Deg2Rad);

                Vector3 UpDir = Root.transform.up;
                Vector3 curCamDir = Vector3.Normalize(Target.position - Root.position);
                Vector3 x_positive_dir = Vector3.Normalize(Vector3.Cross(curCamDir, UpDir));
                Vector3 y_positive_dir = Vector3.Normalize(Vector3.Cross(x_positive_dir, curCamDir));

                // Eye movement correction amount
                float eyeLensShiftX = 0f;
                float eyeLensShiftY = 0f;
                if (useEyeTrackingXOffset)
                {
                    float aspect = (float)_device.subimg_width / _device.subimg_height;
                    eyeLensShiftX = -(eyeOffset.x * fl) / FocalPlane;
                    eyeLensShiftY = -(eyeOffset.y * fl * aspect) / FocalPlane;
                }

                for (int i = 0; i < _device.total_viewnum; i++)
                {
                    _batchcameras[i].orthographic = false;
                    _batchcameras[i].usePhysicalProperties = true;
                    int n_i = i;

                    if (showSerial || onlyShowSerial)
                    {
                        _batchcameras_angles[i] = -_device.total_fov * 0.5f + (n_i * _device.total_fov) / (_device.total_viewnum - 1);
                        AngleText[i].text = _batchcameras_angles[i].ToString("F2");
                    }

                    float x_i = x_fov - (n_i * 2 * x_fov) / (_device.total_viewnum - 1);
                    float a_i = ((x_i * fl) / FocalPlane);

                    _batchcameras[i].transform.position = Root.position + x_positive_dir * (x_i - eyeOffset.x) + y_positive_dir * eyeOffset.y;
                    _batchcameras[i].lensShift = new Vector2(a_i + eyeLensShiftX, eyeLensShiftY);

                    _batchcameras[i].transform.rotation = Root.rotation;
                }
            }
        }

        private void UpdateEyeOffset()
        {
            if (!useEyeTrackingXOffset || _eye == null || !_eye.isRunning)
            {
                eyeTrackingOffsetX = Mathf.SmoothDamp(eyeTrackingOffsetX, 0f, ref eyeTrackingOffsetVelocity, eyeTrackingSmoothing);
                eyeTrackingOffsetY = Mathf.SmoothDamp(eyeTrackingOffsetY, 0f, ref eyeTrackingOffsetVelocityY, eyeTrackingSmoothing);
                eyeOffset = Vector2.zero;
                return;
            }

            Vector2 avg = _eye.AveragePos();
            float centeredX = (avg.x - 0.5f);
            float centeredY = (avg.y - 0.5f);
            if (Mathf.Abs(centeredX * 2f) < eyeTrackingDeadZone)
            {
                centeredX = 0f;
            }
            if (Mathf.Abs(centeredY * 2f) < eyeTrackingDeadZone)
            {
                centeredY = 0f;
            }

            // Smoothed normalized offset
            eyeTrackingOffsetX = Mathf.SmoothDamp(eyeTrackingOffsetX, centeredX, ref eyeTrackingOffsetVelocity, eyeTrackingSmoothing);
            eyeTrackingOffsetY = Mathf.SmoothDamp(eyeTrackingOffsetY, centeredY, ref eyeTrackingOffsetVelocityY, eyeTrackingSmoothing);

            eyeOffset = GetEyeTrackingOffset();
        }

        private void UpdateTextureArray()
        {
            // Update texture array using camera render textures
            for (int i = 0; i < _device.total_viewnum; i++)
            {
                if (i < cameraRenderTextures.Length && cameraRenderTextures[i] != null)
                {
                    Graphics.CopyTexture(cameraRenderTextures[i], 0, 0, textureArray, i, 0);
                }
            }
        }
        #endregion

        #region Frustum and Focal Plane Visualization
        // Moved to BatchVisualDebugger.cs
        #endregion

        // Render end callback: record and output latency log
        public void OnDisplayCameraPostRender(float renderSubmitRealtime)
        {
            if (latencyMonitor != null)
            {
                latencyMonitor.OnDisplayCameraPostRender(renderSubmitRealtime);
            }
        }

        [Serializable]
        private class EyeTrackingConfigWrapper
        {
            public GratingParams grating_params;
        }

        [Serializable]
        private class GratingParams
        {
            public float x0;
            public float interval;
            public float slope;
        }
    }
}
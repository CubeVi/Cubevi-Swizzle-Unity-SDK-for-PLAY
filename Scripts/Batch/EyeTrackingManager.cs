using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Cubevi_Swizzle
{
    public class EyeTrackingManager : MonoBehaviour
    {
        #region DLL Import
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool MyStartEyeTracking();

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StopEyeTracking();

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetServerIpAndPort(string ip, int port);

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetEyePositionData(StringBuilder buffer, int bufferSize);

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetX0Array([Out] float[] buffer, int bufferSize);

        // Get last error message. Returns copied length, if <0 means failure, no pop.
        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MyGetLastErrorAndPop(StringBuilder buffer, int bufferSize);

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool HasError();

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ClearErrors();

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetOnnxPath(IntPtr onnxPath, int onnxPathSize);

        [DllImport("facetrack_websockets_dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetConfigJsonPath(IntPtr jsonPath, int jsonPathSize);

#endif
        #endregion

        private string ip = "172.28.128.100";
        private int port = 8080;

        public string onnxPath;
        public string configJsonPath;

        // Latency measurement: Last data sequence, receive time and frame, and call/parse duration
        // Latency monitoring switch
        public bool monitorLatency = false;
        // Latency measurement fields
        [HideInInspector]
        public ulong lastSeq = 0;
        [HideInInspector]
        public float lastRecvRealtime = 0f;
        [HideInInspector]
        public int lastRecvFrame = -1;
        [HideInInspector]
        public float lastDllCallMs = 0f;
        [HideInInspector]
        public float lastParsingMs = 0f;

        // Data update frequency (seconds)
        private float updateInterval = 0.02f;

        // Eye tracking data
        [Header("Eye Tracking Data")]
        public bool isRunning = false;

        public bool cam0Detected = false;
        public bool cam1Detected = false;

        // Normalized eye positions
        public Vector2 cam0LeftEyeNorm = new Vector2();
        public Vector2 cam0RightEyeNorm = new Vector2();
        public Vector2 cam1LeftEyeNorm = new Vector2();
        public Vector2 cam1RightEyeNorm = new Vector2();

        // Events
        public delegate void EyeTrackingDataUpdatedHandler(EyeTrackingData data);
        public event EyeTrackingDataUpdatedHandler OnEyeTrackingDataUpdated;

        public Action<float[]> OnX0ArrayAvailable;

        #region Error Handling
        private bool showErrors = true;
        private Queue<string> errorMessages = new Queue<string>();
        private StringBuilder errorBuffer;
        private const int ErrorBufferSize = 1024;
        #endregion

        // JSON Data Classes
        [System.Serializable]
        public class EyeData
        {
            public PointData leftEye;
            public PointData rightEye;
            public bool detected;
        }

        [System.Serializable]
        public class PointData
        {
            public float x;
            public float y;
        }

        [System.Serializable]
        public class EyeTrackingData
        {
            public EyeData cam0;
            public EyeData cam1;
            public long timestamp;
        }

        private const int BufferSize = 1024;
        private StringBuilder buffer;
        private float timer = 0;

        private const int X0ArraySize = 77;
        private readonly float[] x0ArrayBuffer = new float[X0ArraySize];

        [HideInInspector]
        public float fovX = 60f; // Camera fov

        [HideInInspector]
        public float cam_distance_x = 60.0f; // Camera distance

        private void Awake()
        {
            buffer = new StringBuilder(BufferSize);
            errorBuffer = new StringBuilder(ErrorBufferSize);
        }

        private void Start()
        {
            // Set default model paths
            string defaultOnnxName = "eye_tracking_x0.onnx";
            string defaultJsonName = "eye_tracking_x0.json";

            string tempOnnxPath = Path.Combine(Application.streamingAssetsPath, defaultOnnxName);
            string tempJsonPath = Path.Combine(Application.streamingAssetsPath, defaultJsonName);

            // Check if files exist
            if (File.Exists(tempOnnxPath) && File.Exists(tempJsonPath))
            {
                onnxPath = tempOnnxPath;
                configJsonPath = tempJsonPath;
                SwizzleLog.LogInfo($"Eye tracking model files found, will load on startup");
            }
            else
            {
                SwizzleLog.LogWarning("Eye tracking model files not found (eye_tracking_x0.onnx/json), skipping model load");
                onnxPath = null;
                configJsonPath = null;
            }

            UpdateManager.Instance.RegisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.EyeTracking);
        }

        private void OnDestroy()
        {
            StopTracking();

            UpdateManager.Instance.UnregisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.EyeTracking);
        }

        private void SwizzleUpdate()
        {
            if (!isRunning) return;

            UpdateX0Array();

            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0;
                CheckForErrors();
                UpdateEyeTrackingData();
            }
        }

        private void UpdateX0Array()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            int copiedLength = GetX0Array(x0ArrayBuffer, x0ArrayBuffer.Length);
            if (copiedLength > 0)
            {
                OnX0ArrayAvailable?.Invoke(x0ArrayBuffer);
            }
#endif
        }

        private void OnApplicationQuit()
        {
            StopTracking();
        }

        // Add error checking method
        private void CheckForErrors()
        {
            while (HasError())
            {
                errorBuffer.Clear();
                int length = MyGetLastErrorAndPop(errorBuffer, ErrorBufferSize);
                if (length > 0)
                {
                    string error = errorBuffer.ToString();
                    SwizzleLog.LogWarning("EyeTracking DLL Error: " + error);
                    if (showErrors)
                    {
                        errorMessages.Enqueue(error);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // Add OnGUI method to display errors
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!showErrors || errorMessages.Count == 0) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label("EyeTracking Errors:", GUI.skin.box);

            foreach (string error in errorMessages)
            {
                GUILayout.Label(error);
            }

            if (GUILayout.Button("Clear"))
            {
                errorMessages.Clear();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif

        /// <summary>
        /// Start eye tracking
        /// </summary>
        /// <returns>Whether started successfully</returns>
        public bool StartTracking()
        {
            if (isRunning) return true;

            try
            {
                if (!IsDllAvailable())
                {
                    SwizzleLog.LogError("EyeTrackingDLL not available, please check if DLL file is placed correctly");
                    return false;
                }

                // Add extra exception handling
                bool startSuccess = false;
                try
                {
                    SwizzleLog.LogInfo("Starting eye tracking");
                    startSuccess = MyStartEyeTracking();
                    SwizzleLog.LogInfo("Eye tracking start process finished");
                }
                catch (Exception ex)
                {
                    SwizzleLog.LogError($"Exception during eye tracking start: {ex.Message}");
                    return false;
                }

                if (!startSuccess)
                {
                    SwizzleLog.LogError("Failed to start eye tracking");

                    int length = MyGetLastErrorAndPop(errorBuffer, ErrorBufferSize);
                    SwizzleLog.LogError($"DLL Error: {errorBuffer.ToString()}");
                    return false;
                }

                isRunning = true;
                return true;
            }
            catch (Exception e)
            {
                SwizzleLog.LogError($"Error starting eye tracking: {e.Message}");
                return false;
            }
        }

        public void StopTracking()
        {
            if (!isRunning) return;

            try
            {
                StopEyeTracking();
                isRunning = false;
                SwizzleLog.LogInfo("Eye tracking stopped");
            }
            catch (Exception e)
            {
                SwizzleLog.LogError($"Error stopping eye tracking: {e.Message}");
            }
        }

        private void UpdateEyeTrackingData()
        {
            try
            {
                buffer.Clear();
                float callStart = 0f, callEnd = 0f;
                if (monitorLatency) callStart = Time.realtimeSinceStartup;
                int length = GetEyePositionData(buffer, BufferSize);
                if (monitorLatency) callEnd = Time.realtimeSinceStartup;

                if (length > 0)
                {
                    float parseStart = 0f, parseEnd = 0f;
                    if (monitorLatency) parseStart = Time.realtimeSinceStartup;
                    string json = buffer.ToString();
                    EyeTrackingData data = JsonConvert.DeserializeObject<EyeTrackingData>(json);
                    if (monitorLatency) parseEnd = Time.realtimeSinceStartup;

                    cam0Detected = data.cam0 != null && data.cam0.detected;
                    cam1Detected = data.cam1 != null && data.cam1.detected;

                    if (cam0Detected)
                    {
                        cam0LeftEyeNorm.x = data.cam0.leftEye.x;
                        cam0LeftEyeNorm.y = data.cam0.leftEye.y;
                        cam0RightEyeNorm.x = data.cam0.rightEye.x;
                        cam0RightEyeNorm.y = data.cam0.rightEye.y;
                    }
                    if (cam1Detected)
                    {
                        cam1LeftEyeNorm.x = data.cam1.leftEye.x;
                        cam1LeftEyeNorm.y = data.cam1.leftEye.y;
                        cam1RightEyeNorm.x = data.cam1.rightEye.x;
                        cam1RightEyeNorm.y = data.cam1.rightEye.y;
                    }

                    if (monitorLatency)
                    {
                        lastDllCallMs = (callEnd - callStart) * 1000f;
                        lastParsingMs = (parseEnd - parseStart) * 1000f;
                        lastSeq++;
                        lastRecvRealtime = parseEnd;
                        lastRecvFrame = Time.frameCount;

                        SwizzleLog.LogInfo($"[EyeTracking] Recv seq={lastSeq}, call={lastDllCallMs:F2}ms, parse={lastParsingMs:F2}ms, frame={lastRecvFrame}");
                    }

                    OnEyeTrackingDataUpdated?.Invoke(data);
                }
            }
            catch (Exception e)
            {
                SwizzleLog.LogError($"Error updating eye tracking data: {e.Message}");
            }
        }

        /// <summary>
        /// Check if DLL is available and set DLL port
        /// </summary>
        private bool IsDllAvailable()
        {
            try
            {
                SwizzleLog.LogInfo($"{ip}:{port}");
                ConfigureDll();
                return true;
            }
            catch (DllNotFoundException e)
            {
                return false;
            }
            catch (EntryPointNotFoundException e)
            {
                SwizzleLog.LogError($"DLL entry point not found: {e.Message}");
                return false;
            }
            catch (SEHException e)
            {
                SwizzleLog.LogError($"Severe error in DLL (SEH Exception): {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                SwizzleLog.LogError($"Exception checking DLL availability: {e.Message}");
                return false;
            }
        }

        private void ConfigureDll()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            SwizzleLog.LogInfo($"{ip}:{port}");
            SetServerIpAndPort(ip, port);
            TrySetX0ModelPaths();
#endif
        }

        private void TrySetX0ModelPaths()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            TrySetPath(SetOnnxPath, onnxPath);
            TrySetPath(SetConfigJsonPath, configJsonPath);
            SwizzleLog.LogImportant("Setting onnx model path");
#endif
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private static void TrySetPath(Func<IntPtr, int, int> setPathFunc, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            IntPtr pathPtr = IntPtr.Zero;
            try
            {
                pathPtr = AllocUtf8String(path, out int byteSize);
                setPathFunc(pathPtr, byteSize);
            }
            finally
            {
                if (pathPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pathPtr);
                }
            }
        }

        private static IntPtr AllocUtf8String(string text, out int byteSize)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byteSize = bytes.Length;
            IntPtr ptr = Marshal.AllocHGlobal(byteSize + 1);
            Marshal.Copy(bytes, 0, ptr, byteSize);
            Marshal.WriteByte(ptr, byteSize, 0);
            return ptr;
        }
#endif

        public Vector2 AveragePos()
        {
            return (cam0LeftEyeNorm + cam0RightEyeNorm + cam1LeftEyeNorm + cam1RightEyeNorm) / 4;
        }

        public float DeltaPos()
        {
            return Mathf.Abs(cam0LeftEyeNorm.x + cam0RightEyeNorm.x - cam1LeftEyeNorm.x - cam1RightEyeNorm.x) / 2;
        }
    }
}

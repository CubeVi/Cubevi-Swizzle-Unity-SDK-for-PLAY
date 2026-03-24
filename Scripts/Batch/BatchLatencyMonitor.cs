using UnityEngine;

namespace Cubevi_Swizzle
{
    public class BatchLatencyMonitor : MonoBehaviour
    {
        private EyeTrackingManager _eye;
        private BatchCameraManager manager;

        private ulong lastAppliedEyeSeq = 0;
        private ulong lastLoggedEyeSeq = 0;
        private float lastAppliedRealtime = 0f;
        private float lastRenderSubmitRealtime = 0f;
        private int framesDelaySinceData = 0;
        private bool latencyWarnedNoData = false;

        public void Initialize(BatchCameraManager manager, EyeTrackingManager eye)
        {
            this.manager = manager;
            this._eye = eye;
        }

        public void ManualUpdate()
        {
            if (manager == null || _eye == null) return;

            // Update latency tracking logic
            if (manager.IsLatencyMonitoringEnabled())
            {
                if (!_eye.isRunning && !latencyWarnedNoData)
                {
                    SwizzleLog.LogWarning("[Latency] Eye tracking not running; no latency to measure. Click StartEye.");
                    latencyWarnedNoData = true;
                }

                if (_eye.lastSeq != 0 && _eye.lastSeq != lastAppliedEyeSeq)
                {
                    lastAppliedEyeSeq = _eye.lastSeq;
                    lastAppliedRealtime = Time.realtimeSinceStartup;
                    framesDelaySinceData = Time.frameCount - _eye.lastRecvFrame;

                    SwizzleLog.LogInfo($"[Latency] Applied seq={lastAppliedEyeSeq} (frameΔ={framesDelaySinceData})");
                }
            }
        }

        public void OnDisplayCameraPostRender(float renderSubmitRealtime)
        {
            if (_eye == null || !manager.IsLatencyMonitoringEnabled()) return;

            lastRenderSubmitRealtime = renderSubmitRealtime;

            if (lastLoggedEyeSeq != lastAppliedEyeSeq && _eye.lastSeq != 0)
            {
                lastLoggedEyeSeq = lastAppliedEyeSeq;

                float recvToSubmitMs = (lastRenderSubmitRealtime - _eye.lastRecvRealtime) * 1000f;
                float recvToApplyMs = (lastAppliedRealtime - _eye.lastRecvRealtime) * 1000f;

                SwizzleLog.LogInfo(
                    $"[Latency] DLL={_eye.lastDllCallMs:F2}ms, Parse={_eye.lastParsingMs:F2}ms, FrameΔ={framesDelaySinceData}, Recv→Apply={recvToApplyMs:F2}ms, Recv→Submit={recvToSubmitMs:F2}ms"
                );
            }
        }
    }
}
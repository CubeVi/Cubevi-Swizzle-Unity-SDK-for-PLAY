using UnityEngine;
using UnityEngine.UI;

namespace Cubevi_Swizzle
{
    public class EyeTrackingExample : MonoBehaviour
    {
        public EyeTrackingManager eyeTrackingManager;
        public Text statusText;

        public GameObject eyeVisualizer;
        public RectTransform cam0LeftEyeMarker;
        public RectTransform cam0RightEyeMarker;
        public RectTransform cam1LeftEyeMarker;
        public RectTransform cam1RightEyeMarker;

        private float deltaTime = 0.0f;

        private void Start()
        {
            UpdateManager.Instance.RegisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.UI);
        }

        private void OnDestroy()
        {
            UpdateManager.Instance.UnregisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.UI);
        }

        private void SwizzleUpdate()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            UpdateStatusText();
            UpdateEyeMarkers();
        }

        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                float fps = 1.0f / deltaTime;

                string status = eyeTrackingManager.isRunning ? "Running" : "Stopped";
                string cam0Status = eyeTrackingManager.cam0Detected ? "Detected" : "Not detected";
                string cam1Status = eyeTrackingManager.cam1Detected ? "Detected" : "Not detected";

                Vector2 averagePos = eyeTrackingManager.AveragePos();
                float totalDeltaPos = eyeTrackingManager.DeltaPos();

                statusText.text = $"FPS: {fps:F2}    " +
                    $"Status: {status}\n" +
                    "------------------------------------\n" +
                    $"CameraLeft: {cam0Status}\n" +
                    $"LeftEye: ({eyeTrackingManager.cam0LeftEyeNorm.x:F2}, {eyeTrackingManager.cam0LeftEyeNorm.y:F2})   " +
                    $"RightEye: ({eyeTrackingManager.cam0RightEyeNorm.x:F2}, {eyeTrackingManager.cam0RightEyeNorm.y:F2})\n" +
                    $"CameraRight: {cam1Status}\n" +
                    $"LeftEye: ({eyeTrackingManager.cam1LeftEyeNorm.x:F2}, {eyeTrackingManager.cam1RightEyeNorm.y:F2})   " +
                    $"RightEye: ({eyeTrackingManager.cam1RightEyeNorm.x:F2}, {eyeTrackingManager.cam1RightEyeNorm.y:F2})\n" +
                    "------------------------------------\n" +
                    $"Average Position: ({averagePos.x:F4}, {averagePos.y:F4})   " +
                    $"Delta_pos: {totalDeltaPos:F4}\n";
            }
        }

        private void UpdateEyeMarkers()
        {
            if (eyeVisualizer == null) return;

            RectTransform visualizerRect = eyeVisualizer.GetComponent<RectTransform>();
            float width = visualizerRect.rect.width;
            float height = visualizerRect.rect.height;

            if (cam0LeftEyeMarker != null)
            {
                cam0LeftEyeMarker.anchoredPosition = new Vector2(
                    (eyeTrackingManager.cam0LeftEyeNorm.x - 0.5f) * width,
                    (eyeTrackingManager.cam0LeftEyeNorm.y - 0.5f) * height
                );
                cam0LeftEyeMarker.gameObject.SetActive(eyeTrackingManager.cam0Detected);
            }

            if (cam0RightEyeMarker != null)
            {
                cam0RightEyeMarker.anchoredPosition = new Vector2(
                    (eyeTrackingManager.cam0RightEyeNorm.x - 0.5f) * width,
                    (eyeTrackingManager.cam0RightEyeNorm.y - 0.5f) * height
                );
                cam0RightEyeMarker.gameObject.SetActive(eyeTrackingManager.cam0Detected);
            }

            if (cam1LeftEyeMarker != null)
            {
                cam1LeftEyeMarker.anchoredPosition = new Vector2(
                    (eyeTrackingManager.cam1LeftEyeNorm.x - 0.5f) * width,
                    (eyeTrackingManager.cam1LeftEyeNorm.y - 0.5f) * height
                );
                cam1LeftEyeMarker.gameObject.SetActive(eyeTrackingManager.cam1Detected);
            }

            if (cam1RightEyeMarker != null)
            {
                cam1RightEyeMarker.anchoredPosition = new Vector2(
                    (eyeTrackingManager.cam1RightEyeNorm.x - 0.5f) * width,
                    (eyeTrackingManager.cam1RightEyeNorm.y - 0.5f) * height
                );
                cam1RightEyeMarker.gameObject.SetActive(eyeTrackingManager.cam1Detected);
            }
        }
    }
}
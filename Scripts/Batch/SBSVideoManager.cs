using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Cubevi_Swizzle
{
    public class SBSVideoManager : MonoBehaviour
    {
        [Header("Stereo Video Rendering Components")]
        public Renderer leftQuadRenderer;
        public Renderer rightQuadRenderer;
        public VideoPlayer videoPlayer;
        public Button SBS_FileLoad;
        public GameObject CubeGroup;

        [Header("Video Control UI")]
        public Button playPauseButton;
        public Image playIcon;
        public Image pauseIcon;
        public Toggle autoReplayToggle;
        public Slider progressSlider;
        public Slider volumeSlider;
        public Text currentTimeText;
        public Text totalTimeText;
        public GameObject controlPanel;

        private Vector3 InitialquadScale;
        private bool isSBS = false;
        private bool isVideoPlaying = false;
        private bool isDraggingSlider = false;
        private bool isAutoReplay = false;


        // Reference to BatchCameraManager to control cameras
        private BatchCameraManager batchCameraManager;

        private void Start()
        {
            // Initialize video components
            InitializeVideoComponents();

            UpdateManager.Instance.RegisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.Video);
        }

        private void OnDestroy()
        {
            // Unregister
            UpdateManager.Instance.UnregisterUpdate(SwizzleUpdate, UpdateManager.UpdatePriority.Video);
        }

        private void SwizzleUpdate()
        {
            // Toggle video playback/pause when Space key is pressed
            if (isSBS && Input.GetKeyDown(KeyCode.Space))
            {
                ToggleVideoPlayback();
            }

            // Update progress bar and time display
            if (isSBS && videoPlayer.isPlaying && !isDraggingSlider)
            {
                UpdateProgressUI();
            }
        }

        // Toggle video playback/pause state
        private void ToggleVideoPlayback()
        {
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                isVideoPlaying = false;
                playIcon.gameObject.SetActive(true);
                pauseIcon.gameObject.SetActive(false);
            }
            else
            {
                videoPlayer.Play();
                isVideoPlaying = true;
                playIcon.gameObject.SetActive(false);
                pauseIcon.gameObject.SetActive(true);
            }
        }

        private void UpdateProgressUI()
        {
            // Update progress bar
            progressSlider.value = (float)(videoPlayer.time / videoPlayer.length);

            // Update time text
            if (currentTimeText != null)
                currentTimeText.text = FormatTime(videoPlayer.time);

            if (totalTimeText != null)
                totalTimeText.text = FormatTime(videoPlayer.length);
        }

        private string FormatTime(double timeInSeconds)
        {
            int minutes = (int)(timeInSeconds / 60);
            int seconds = (int)(timeInSeconds % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        public void Initialize(BatchCameraManager cameraManager)
        {
            batchCameraManager = cameraManager;
        }

        private void InitializeVideoComponents()
        {
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEnd;
            InitialquadScale = leftQuadRenderer.transform.localScale;

            // Initialize play button
            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(ToggleVideoPlayback);

            if (autoReplayToggle != null)
            {
                autoReplayToggle.isOn = isAutoReplay;
                autoReplayToggle.onValueChanged.AddListener(OnAutoReplayToggleChanged);
            }

            if (progressSlider != null)
            {
                progressSlider.onValueChanged.AddListener(OnProgressSliderValueChanged);

                // Progress bar drag start and end events
                EventTrigger trigger = progressSlider.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = progressSlider.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
                pointerDownEntry.eventID = EventTriggerType.PointerDown;
                pointerDownEntry.callback.AddListener((data) => { OnSliderDragStart(); });
                trigger.triggers.Add(pointerDownEntry);

                EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
                pointerUpEntry.eventID = EventTriggerType.PointerUp;
                pointerUpEntry.callback.AddListener((data) => { OnSliderDragEnd(); });
                trigger.triggers.Add(pointerUpEntry);
            }

            if (volumeSlider != null)
            {
                volumeSlider.value = videoPlayer.GetDirectAudioVolume(0);
                volumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
            }

            // Initialize control panel
            if (controlPanel != null)
                controlPanel.SetActive(false);
        }

        // Video playback end callback
        private void OnVideoEnd(VideoPlayer source)
        {
            if (isAutoReplay)
            {
                // Auto replay
                source.frame = 0;
                source.Play();
            }
            else
            {
                // Pause and show play button
                isVideoPlaying = false;
                if (playIcon != null && pauseIcon != null)
                {
                    playIcon.gameObject.SetActive(true);
                    pauseIcon.gameObject.SetActive(false);
                }
            }
        }

        private void OnAutoReplayToggleChanged(bool value)
        {
            isAutoReplay = value;
            videoPlayer.isLooping = value;
        }

        private void OnProgressSliderValueChanged(float value)
        {
            if (isDraggingSlider)
            {
                // Update time display in real-time while dragging, without changing video position
                double targetTime = value * videoPlayer.length;
                if (currentTimeText != null)
                    currentTimeText.text = FormatTime(targetTime);
            }
        }

        private void OnSliderDragStart()
        {
            isDraggingSlider = true;
            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
        }

        private void OnSliderDragEnd()
        {
            double targetTime = progressSlider.value * videoPlayer.length;
            videoPlayer.time = targetTime;

            if (isVideoPlaying)
                videoPlayer.Play();

            isDraggingSlider = false;
        }

        private void OnVolumeSliderValueChanged(float value)
        {
            videoPlayer.SetDirectAudioVolume(0, value);
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            // Get video texture
            Texture videoTexture = source.texture;
            if (videoTexture == null)
            {
                SwizzleLog.LogError("Texture not assigned or loaded.");
                return;
            }

            // Get video resolution
            int width = videoTexture.width;
            int height = videoTexture.height;

            // Set material for left and right Quads
            leftQuadRenderer.material.mainTexture = videoTexture;
            rightQuadRenderer.material.mainTexture = videoTexture;

            // Calculate video aspect ratio
            float videoAspectRatio = (float)width / height;

            // Get current scale of Quad
            Vector3 NewQuadScale = InitialquadScale;
            // Keep y-axis scale, adjust x-axis to match video aspect ratio (assuming SBS format)
            NewQuadScale.x = (videoAspectRatio / 2) * NewQuadScale.y;

            // Apply new Quad scale
            leftQuadRenderer.transform.localScale = NewQuadScale;
            rightQuadRenderer.transform.localScale = NewQuadScale;

            // Set camera positions
            batchCameraManager.GetBatchCameras()[0].transform.position = new Vector3(-9.5f, 0, 0);
            batchCameraManager.GetBatchCameras()[0].orthographic = true;
            batchCameraManager.GetBatchCameras()[0].orthographicSize = 2.25f;
            batchCameraManager.GetBatchCameras()[1].transform.position = new Vector3(9.5f, 0, 0);
            batchCameraManager.GetBatchCameras()[1].orthographic = true;
            batchCameraManager.GetBatchCameras()[1].orthographicSize = 2.25f;

            // Play video
            AdjustUVs();

            // Show control panel
            if (controlPanel != null)
                controlPanel.SetActive(true);

            // Initialize time display
            if (totalTimeText != null)
                totalTimeText.text = FormatTime(videoPlayer.length);

            if (currentTimeText != null)
                currentTimeText.text = "00:00";

            // Set play button status
            if (playIcon != null && pauseIcon != null)
            {
                playIcon.gameObject.SetActive(false);
                pauseIcon.gameObject.SetActive(true);
            }
        }

        private void AdjustUVs()
        {
            // Get MeshFilter components for left and right Quads
            MeshFilter leftMeshFilter = leftQuadRenderer.GetComponent<MeshFilter>();
            MeshFilter rightMeshFilter = rightQuadRenderer.GetComponent<MeshFilter>();

            // Ensure MeshFilter components exist
            if (leftMeshFilter == null || rightMeshFilter == null)
            {
                SwizzleLog.LogError("MeshFilter component not found.");
                return;
            }

            // Get meshes
            Mesh leftMesh = leftMeshFilter.mesh;
            Mesh rightMesh = rightMeshFilter.mesh;

            // Set UV coordinates for left Quad
            Vector2[] leftUVs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0.5f, 0),
                new Vector2(0, 1),
                new Vector2(0.5f, 1)
            };
            leftMesh.uv = leftUVs;

            // Set UV coordinates for right Quad
            Vector2[] rightUVs = new Vector2[]
            {
                new Vector2(0.5f, 0),
                new Vector2(1, 0),
                new Vector2(0.5f, 1),
                new Vector2(1, 1)
            };
            rightMesh.uv = rightUVs;

            videoPlayer.Play();
            isVideoPlaying = true;
            SwizzleLog.LogInfo("Playing video from: " + videoPlayer.url);
        }

        public void VideoLoad()
        {
            string videoPath = OpenFile.ChooseVideo();
            SwizzleLog.LogInfo(videoPath);

            if (batchCameraManager != null && batchCameraManager.GetBatchCameras() != null && batchCameraManager.GetBatchCameras().Length == 2)
            {
                if (videoPath != null)
                {
                    videoPlayer.gameObject.SetActive(true);
                    videoPlayer.url = videoPath; // Set video path
                    videoPlayer.Prepare(); // Prepare video to get resolution info

                    isSBS = true;
                    CubeGroup.SetActive(false);

                    // Notify BatchCameraManager that video state has changed
                    if (batchCameraManager != null)
                    {
                        foreach (Camera cam in batchCameraManager.GetBatchCameras())
                        {
                            cam.clearFlags = CameraClearFlags.Color;
                        }
                        batchCameraManager.SetSBSMode(true);
                    }
                }
            }
        }

        public void VideoBack()
        {
            if (isSBS)
            {
                videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false);
                CubeGroup.SetActive(true);

                isSBS = false;

                // Hide control panel
                if (controlPanel != null)
                    controlPanel.SetActive(false);

                // Notify BatchCameraManager that video state has changed
                if (batchCameraManager != null)
                {
                    foreach (Camera cam in batchCameraManager.GetBatchCameras())
                    {
                        cam.clearFlags = CameraClearFlags.Skybox;
                    }
                    batchCameraManager.SetSBSMode(false);
                }
            }
        }
    }
}
using UnityEngine;
using TMPro;

namespace Cubevi_Swizzle
{
    public class BatchVisualDebugger : MonoBehaviour
    {
        private BatchCameraManager manager;
        private DeviceData _device => manager._device;
        private Transform Root => manager.RootTransfrom;

        // Visual Elements
        private GameObject frustumFrame;
        private LineRenderer nearFrameRenderer;
        private LineRenderer farFrameRenderer;
        private LineRenderer connectLineRendererTopLeft;
        private LineRenderer connectLineRendererTopRight;
        private LineRenderer connectLineRendererBottomLeft;
        private LineRenderer connectLineRendererBottomRight;

        private GameObject focalPlaneObject;
        private MeshFilter focalPlaneMeshFilter;
        private MeshRenderer focalPlaneMeshRenderer;

        private GameObject gridSquareRoot;
        private GameObject gridFront, gridTop, gridBottom, gridLeft, gridRight;
        private Material gridMaterial;

        public void Initialize(BatchCameraManager manager)
        {
            this.manager = manager;
            InitFrustumFrame();
            InitFocalPlane();
            InitGridSquare();
        }

        public void ManualUpdate()
        {
            if (manager == null) return;

            UpdateFrustumFrame();
            UpdateFocalPlane();
            UpdateGridSquare();
        }

        public void Cleanup()
        {
            if (frustumFrame != null) Destroy(frustumFrame);
            if (focalPlaneObject != null) Destroy(focalPlaneObject);
            if (gridSquareRoot != null) Destroy(gridSquareRoot);
        }

        #region Frustum Frame
        private void InitFrustumFrame()
        {
            frustumFrame = new GameObject("FrustumFrame");
            DontDestroyOnLoad(frustumFrame);
            frustumFrame.transform.SetParent(Root);

            GameObject nearFrame = new GameObject("NearFrame");
            nearFrame.transform.SetParent(frustumFrame.transform);
            nearFrameRenderer = nearFrame.AddComponent<LineRenderer>();
            ConfigureLineRenderer(nearFrameRenderer, Color.red);

            GameObject farFrame = new GameObject("FarFrame");
            farFrame.transform.SetParent(frustumFrame.transform);
            farFrameRenderer = farFrame.AddComponent<LineRenderer>();
            ConfigureLineRenderer(farFrameRenderer, Color.blue);

            GameObject connectLineTopLeft = new GameObject("ConnectLineTopLeft");
            connectLineTopLeft.transform.SetParent(frustumFrame.transform);
            connectLineRendererTopLeft = connectLineTopLeft.AddComponent<LineRenderer>();
            ConfigureLineRenderer(connectLineRendererTopLeft, Color.green);

            GameObject connectLineTopRight = new GameObject("ConnectLineTopRight");
            connectLineTopRight.transform.SetParent(frustumFrame.transform);
            connectLineRendererTopRight = connectLineTopRight.AddComponent<LineRenderer>();
            ConfigureLineRenderer(connectLineRendererTopRight, Color.green);

            GameObject connectLineBottomLeft = new GameObject("ConnectLineBottomLeft");
            connectLineBottomLeft.transform.SetParent(frustumFrame.transform);
            connectLineRendererBottomLeft = connectLineBottomLeft.AddComponent<LineRenderer>();
            ConfigureLineRenderer(connectLineRendererBottomLeft, Color.green);

            GameObject connectLineBottomRight = new GameObject("ConnectLineBottomRight");
            connectLineBottomRight.transform.SetParent(frustumFrame.transform);
            connectLineRendererBottomRight = connectLineBottomRight.AddComponent<LineRenderer>();
            ConfigureLineRenderer(connectLineRendererBottomRight, Color.green);
        }

        private void ConfigureLineRenderer(LineRenderer lineRenderer, Color color)
        {
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.positionCount = 2;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        private void UpdateFrustumFrame()
        {
            if (manager.IsSBS) 
            {
                if (frustumFrame != null) frustumFrame.SetActive(false);
                return;
            }

            if (!manager.showFrustumFrame)
            {
                if (frustumFrame != null) frustumFrame.SetActive(false);
                return;
            }

            if (frustumFrame != null) frustumFrame.SetActive(true);

            float nearDistance = 0.5f;
            float farDistance = manager.FocalPlane * 2f;

            float nearHeight = 2f * nearDistance * Mathf.Tan(_device.total_fov * 0.5f * Mathf.Deg2Rad);
            float nearWidth = nearHeight * (_device.subimg_width / _device.subimg_height);
            float farHeight = 2f * farDistance * Mathf.Tan(_device.total_fov * 0.5f * Mathf.Deg2Rad);
            float farWidth = farHeight * (_device.subimg_width / _device.subimg_height);

            Vector3 nearTopLeft = Root.position + Root.forward * nearDistance - Root.right * (nearWidth * 0.5f) + Root.up * (nearHeight * 0.5f);
            Vector3 nearTopRight = Root.position + Root.forward * nearDistance + Root.right * (nearWidth * 0.5f) + Root.up * (nearHeight * 0.5f);
            Vector3 nearBottomLeft = Root.position + Root.forward * nearDistance - Root.right * (nearWidth * 0.5f) - Root.up * (nearHeight * 0.5f);
            Vector3 nearBottomRight = Root.position + Root.forward * nearDistance + Root.right * (nearWidth * 0.5f) - Root.up * (nearHeight * 0.5f);

            Vector3 farTopLeft = Root.position + Root.forward * farDistance - Root.right * (farWidth * 0.5f) + Root.up * (farHeight * 0.5f);
            Vector3 farTopRight = Root.position + Root.forward * farDistance + Root.right * (farWidth * 0.5f) + Root.up * (farHeight * 0.5f);
            Vector3 farBottomLeft = Root.position + Root.forward * farDistance - Root.right * (farWidth * 0.5f) - Root.up * (farHeight * 0.5f);
            Vector3 farBottomRight = Root.position + Root.forward * farDistance + Root.right * (farWidth * 0.5f) - Root.up * (farHeight * 0.5f);

            nearFrameRenderer.SetPosition(0, nearTopLeft);
            nearFrameRenderer.SetPosition(1, nearTopRight);
            nearFrameRenderer.SetPosition(2, nearBottomRight);
            nearFrameRenderer.SetPosition(3, nearBottomLeft);
            nearFrameRenderer.SetPosition(4, nearTopLeft);
            nearFrameRenderer.positionCount = 5;

            farFrameRenderer.SetPosition(0, farTopLeft);
            farFrameRenderer.SetPosition(1, farTopRight);
            farFrameRenderer.SetPosition(2, farBottomRight);
            farFrameRenderer.SetPosition(3, farBottomLeft);
            farFrameRenderer.SetPosition(4, farTopLeft);
            farFrameRenderer.positionCount = 5;

            connectLineRendererTopLeft.SetPosition(0, nearTopLeft);
            connectLineRendererTopLeft.SetPosition(1, farTopLeft);

            connectLineRendererTopRight.SetPosition(0, nearTopRight);
            connectLineRendererTopRight.SetPosition(1, farTopRight);

            connectLineRendererBottomLeft.SetPosition(0, nearBottomLeft);
            connectLineRendererBottomLeft.SetPosition(1, farBottomLeft);

            connectLineRendererBottomRight.SetPosition(0, nearBottomRight);
            connectLineRendererBottomRight.SetPosition(1, farBottomRight);
        }
        #endregion

        #region Focal Plane
        private void InitFocalPlane()
        {
            focalPlaneObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            focalPlaneObject.name = "FocalPlane";
            DontDestroyOnLoad(focalPlaneObject);
            focalPlaneObject.transform.SetParent(Root);

            focalPlaneMeshFilter = focalPlaneObject.GetComponent<MeshFilter>();
            focalPlaneMeshRenderer = focalPlaneObject.GetComponent<MeshRenderer>();
            focalPlaneMeshRenderer.material.color = new Color(1f, 1f, 0f, 0.3f);
        }

        private void UpdateFocalPlane()
        {
            if (manager.IsSBS)
            {
                if (focalPlaneObject != null) focalPlaneObject.SetActive(false);
                return;
            }

            if (!manager.showFocalPlane)
            {
                if (focalPlaneObject != null) focalPlaneObject.SetActive(false);
                return;
            }

            if (focalPlaneObject != null) focalPlaneObject.SetActive(true);

            float focalHeight = 2f * manager.FocalPlane * Mathf.Tan(_device.total_fov * 0.5f * Mathf.Deg2Rad);
            float focalWidth = focalHeight * (_device.subimg_width / _device.subimg_height);

            focalPlaneObject.transform.position = Root.position + Root.forward * manager.FocalPlane;
            focalPlaneObject.transform.rotation = Root.rotation;
            focalPlaneObject.transform.localScale = new Vector3(focalWidth, focalHeight, 1f);
        }
        #endregion

        #region Grid Square
        private void InitGridSquare()
        {
            gridMaterial = Resources.Load<Material>("Grid");
            if (gridMaterial == null)
            {
                SwizzleLog.LogWarning("Grid material not found in Resources!");
                gridMaterial = new Material(Shader.Find("Standard"));
            }

            gridSquareRoot = new GameObject("GridSquareRoot");
            DontDestroyOnLoad(gridSquareRoot);
            gridSquareRoot.transform.SetParent(Root);

            gridFront = CreateGridPlane("GridFront");
            gridTop = CreateGridPlane("GridTop");
            gridBottom = CreateGridPlane("GridBottom");
            gridLeft = CreateGridPlane("GridLeft");
            gridRight = CreateGridPlane("GridRight");
        }

        private GameObject CreateGridPlane(string name)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = name;
            plane.transform.SetParent(gridSquareRoot.transform);
            plane.GetComponent<MeshRenderer>().material = gridMaterial;
            return plane;
        }

        private void UpdateGridSquare()
        {
            if (manager.IsSBS)
            {
                if (gridSquareRoot != null) gridSquareRoot.SetActive(false);
                return;
            }

            if (!manager.showGridSquare)
            {
                if (gridSquareRoot != null) gridSquareRoot.SetActive(false);
                return;
            }

            if (gridSquareRoot == null) return;
            gridSquareRoot.SetActive(true);

            float distToTarget = Vector3.Distance(Root.position, manager.TargetTransform.position);
            float frontDistance = 1.0f * distToTarget + 5.0f;
            float c = 35.0f; // Side length
            float k = 5.5f; // Scale factor

            float frontHeight = 2f * frontDistance * Mathf.Tan(_device.total_fov * 0.5f * Mathf.Deg2Rad) * k;
            float frontWidth = frontHeight * (16.0f / 9.0f);

            gridSquareRoot.transform.position = Root.position;
            gridSquareRoot.transform.rotation = Root.rotation;

            gridFront.transform.localPosition = Vector3.forward * frontDistance;
            gridFront.transform.localRotation = Quaternion.identity;
            gridFront.transform.localScale = new Vector3(frontWidth, frontHeight, 1f);

            gridTop.transform.localPosition = new Vector3(0, frontHeight / 2f, frontDistance - c / 2f);
            gridTop.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            gridTop.transform.localScale = new Vector3(frontWidth, c, 1f);

            gridBottom.transform.localPosition = new Vector3(0, -frontHeight / 2f, frontDistance - c / 2f);
            gridBottom.transform.localRotation = Quaternion.Euler(90, 0, 0);
            gridBottom.transform.localScale = new Vector3(frontWidth, c, 1f);

            gridLeft.transform.localPosition = new Vector3(-frontWidth / 2f, 0, frontDistance - c / 2f);
            gridLeft.transform.localRotation = Quaternion.Euler(0, -90, 0);
            gridLeft.transform.localScale = new Vector3(c, frontHeight, 1f);

            gridRight.transform.localPosition = new Vector3(frontWidth / 2f, 0, frontDistance - c / 2f);
            gridRight.transform.localRotation = Quaternion.Euler(0, 90, 0);
            gridRight.transform.localScale = new Vector3(c, frontHeight, 1f);
        }
        #endregion
    }
}
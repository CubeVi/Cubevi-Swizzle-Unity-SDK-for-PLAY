using UnityEngine;
using UnityEngine.Rendering;

namespace Cubevi_Swizzle
{
    public class RenderLatencyProbe : MonoBehaviour
    {
        public BatchCameraManager Manager;
        private Camera targetCamera;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= EndCameraRendering;
        }

        private void EndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (Manager != null && cam == targetCamera && Manager.IsLatencyMonitoringEnabled())
            {
                Manager.OnDisplayCameraPostRender(Time.realtimeSinceStartup);
            }
        }

        private void OnPostRender()
        {
            // Compatible with built-in pipeline (non-SRP)
            if (Manager != null && Manager.IsLatencyMonitoringEnabled())
            {
                Manager.OnDisplayCameraPostRender(Time.realtimeSinceStartup);
            }
        }
    }
}
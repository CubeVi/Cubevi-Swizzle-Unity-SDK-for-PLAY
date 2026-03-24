using UnityEngine;
using UnityEngine.Video;

namespace Cubevi_Swizzle
{
    public class QuadUV : MonoBehaviour
{
    public Renderer leftQuadRenderer;
    public Renderer rightQuadRenderer;

    public VideoPlayer videoPlayer;
    private Texture _texture;

    void Start()
    {
        if (videoPlayer == null || leftQuadRenderer == null || rightQuadRenderer == null)
        {
            SwizzleLog.LogError("VideoPlayer or Renderers not assigned.");
            return;
        }

        // Set material and adjust UVs when video is prepared
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        _texture = source.texture;
        if (_texture == null)
        {
            SwizzleLog.LogError("Texture not assigned or loaded.");
            return;
        }

        // Set material and texture for left and right Quads
        leftQuadRenderer.material.mainTexture = _texture;
        rightQuadRenderer.material.mainTexture = _texture;

        // Adjust UV coordinates
        AdjustUVs();
    }

    private void AdjustUVs()
    {
        MeshFilter leftMeshFilter = leftQuadRenderer.GetComponent<MeshFilter>();
        MeshFilter rightMeshFilter = rightQuadRenderer.GetComponent<MeshFilter>();

        if (leftMeshFilter == null || rightMeshFilter == null)
        {
            SwizzleLog.LogError("MeshFilter component not found.");
            return;
        }

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
    }
}
}

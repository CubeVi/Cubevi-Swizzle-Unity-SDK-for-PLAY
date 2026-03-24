using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cubevi_Swizzle
{
    public class ChangeModel : MonoBehaviour
    {
    [Header("Toggle Feature")]
    public GameObject[] models;
    public Button changeModelButton;

    [Header("Enable Mouse Scroll")]
    public bool isControlEnabled = true;

    [Header("Cube Scaling")]
    public GameObject[] CubeToScale;

    private int currentModelIndex = 0;
    private float zoomSpeed = 1.5f;
    private float rotationSpeed = 10.0f;
    private bool isDragging = false;
    private Vector3 lastMousePosition;

    void Start()
    {
        if (models.Length > 0)
        {
            for (int i = 0; i < models.Length; i++)
            {
                models[i].SetActive(i == 0);
            }
        }

        changeModelButton.onClick.AddListener(Change);
    }

    void Update()
    {
        if (isControlEnabled && this.gameObject.activeSelf)
        {
            // Zoom in/out
            float scrollAmount = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            if (scrollAmount != 0)
            {
                if (currentModelIndex != 0)
                {
                    // Adjust position of the currently active model
                    Vector3 pos = models[currentModelIndex].transform.position;
                    pos.z += scrollAmount;
                    models[currentModelIndex].transform.position = pos;
                }
            }
        }

        // Rotation
        if (currentModelIndex != 0)
        {
            // Check if mouse is clicking on UI
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                Vector3 currentMousePosition = Input.mousePosition;
                float deltaX = currentMousePosition.x - lastMousePosition.x;

                // Rotate model based on horizontal mouse movement (Y-axis)
                models[currentModelIndex].transform.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime);

                lastMousePosition = currentMousePosition;
            }
        }
    }

    void Change()
    {
        if (this.gameObject.activeSelf)
        {
            models[currentModelIndex].SetActive(false);

            currentModelIndex = (currentModelIndex + 1) % models.Length;

            models[currentModelIndex].SetActive(true);
        }
    }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;

    [Header("Zoom")]
    public float zoomSpeed = 1f;
    public float minZoom = 4f;
    public float maxZoom = 15;

    Camera cam;
    Program1 map;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        map = Object.FindAnyObjectByType<Program1>();
        ClampToMap();
    }

    void Update()
    {
        MoveCamera();
        ZoomCamera();
        ClampToMap();
    }

    void MoveCamera()
    {
        Vector3 dir = Vector3.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) dir.y = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) dir.y = -1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) dir.x = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) dir.x = 1;
        }

        Vector3 move = dir.normalized * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }

    void ZoomCamera()
    {
        if (Mouse.current == null || cam == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (cam.orthographic)
        {
            float newSize = cam.orthographicSize;
            newSize -= scroll * zoomSpeed * 0.001f;
            newSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            cam.orthographicSize = newSize;
        }
    }

    void ClampToMap()
    {
        if (map == null)
            map = Object.FindAnyObjectByType<Program1>();
        if (map == null || cam == null)
            return;

        map.ClampCameraPosition(cam);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;

    [Header("Zoom")]
    public float zoomSpeed = 1f;
    public float minZoom = 4f;  // Максимальне наближення
    public float maxZoom = 15; // Максимальне віддалення
    

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        MoveCamera();
        ZoomCamera();
    }

    void MoveCamera()
    {
        Vector3 dir = Vector3.zero;

        if (Keyboard.current != null)
        {
            // У 2D "вгору" - це Y, "вправо" - це X
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) dir.y = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) dir.y = -1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) dir.x = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) dir.x = 1;
        }

        // Рухаємо камеру. Normalized не дає рухатись швидше по діагоналі.
        Vector3 move = dir.normalized * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }

    void ZoomCamera()
    {
        if (Mouse.current == null || cam == null) return;

        // Отримуємо прокрутку коліщатка
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (cam.orthographic)
        {
            // Для 2D камери (Orthographic) змінюємо розмір лінзи
            float newSize = cam.orthographicSize;
            newSize -= scroll * zoomSpeed * 0.001f; // Коригуємо чутливість
            newSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            cam.orthographicSize = newSize;
        }
      
    }
}
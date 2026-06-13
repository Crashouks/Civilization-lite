using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// Legacy click handler — disabled when Program1 drives input.
public class UnitAnimationController : MonoBehaviour
{
    private Tilemap tilemap;
    Program1 gameManager;

    [Header("Текущий выбранный юнит игрока")]
    public Animator selectedUnitAnimator;

    void Start()
    {
        gameManager = Object.FindAnyObjectByType<Program1>();
        tilemap = FindFirstObjectByType<Tilemap>();
        if (tilemap == null && gameManager == null)
            Debug.LogError("Тайлмап (Tilemap) не найден на сцене!");
    }

    void Update()
    {
        if (gameManager != null)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleStrategyClick();
    }

    void HandleStrategyClick()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector2 touchPos2D = new Vector2(worldPosition.x, worldPosition.y);

        RaycastHit2D hit = Physics2D.Raycast(touchPos2D, Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                if (selectedUnitAnimator != null)
                    selectedUnitAnimator.SetTrigger("ToAttack");
                return;
            }

            Animator clickedAnimator = hit.collider.GetComponent<Animator>();
            if (clickedAnimator != null && !hit.collider.CompareTag("Enemy"))
            {
                selectedUnitAnimator = clickedAnimator;
                return;
            }
        }

        if (tilemap != null && selectedUnitAnimator != null)
        {
            Vector3Int cellPosition = tilemap.WorldToCell(touchPos2D);
            if (tilemap.HasTile(cellPosition))
                selectedUnitAnimator.SetTrigger("ToWalk");
        }
    }
}

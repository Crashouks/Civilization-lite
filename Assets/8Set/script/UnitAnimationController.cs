using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class UnitAnimationController : MonoBehaviour
{
    private Tilemap tilemap;

    [Header("Текущий выбранный юнит игрока")]
    public Animator selectedUnitAnimator;

    void Start()
    {
        // Автоматически находим карту Tilemap на сцене
        tilemap = FindFirstObjectByType<Tilemap>();
        if (tilemap == null) Debug.LogError("Тайлмап (Tilemap) не найден на сцене!");
    }

    void Update()
    {
        // Проверяем клик мышки через New Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleStrategyClick();
        }
    }

    void HandleStrategyClick()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector2 touchPos2D = new Vector2(worldPosition.x, worldPosition.y);

        // Пускаем 2D-луч в точку клика
        RaycastHit2D hit = Physics2D.Raycast(touchPos2D, Vector2.zero);

        if (hit.collider != null)
        {
            // 1. КЛИК ПО ВРАГУ: Если у объекта стоит тег "Enemy"
            if (hit.collider.CompareTag("Enemy"))
            {
                if (selectedUnitAnimator != null)
                {
                    Debug.Log($"[Атака] Наш выбранный юнит атакует врага: {hit.collider.name}");
                    selectedUnitAnimator.SetTrigger("ToAttack");
                }
                else
                {
                    Debug.LogWarning("Вы хотите атаковать, но сначала нужно выбрать своего юнита (кликните по нему)!");
                }
                return; // Выходим, чтобы клик по карте под врагом не срабатывал
            }

            // 2. КЛИК ПО СВОЕМУ ЮНИТУ: Если кликнули на объект с компонентом Animator
            // и у него нет тега Enemy — значит, это наш юнит (Soldier, Settler и т.д.)
            Animator clickedAnimator = hit.collider.GetComponent<Animator>();
            if (clickedAnimator != null && !hit.collider.CompareTag("Enemy"))
            {
                selectedUnitAnimator = clickedAnimator;
                Debug.Log($"[Выбор] Выбран юнит игрока: {hit.collider.name}. Теперь им можно ходить или атаковать!");
                return;
            }
        }

        // 3. КЛИК ПО КАРТЕ: Если промахнулись мимо юнитов, проверяем клик по гексу земли
        if (tilemap != null && selectedUnitAnimator != null)
        {
            Vector3Int cellPosition = tilemap.WorldToCell(touchPos2D);

            // Проверяем, существует ли вообще гекс в этом месте карты
            if (tilemap.HasTile(cellPosition))
            {
                Debug.Log($"[Движение] Выбранный юнит отправлен на гекс: {cellPosition}");
                selectedUnitAnimator.SetTrigger("ToWalk");
            }
        }
    }
}
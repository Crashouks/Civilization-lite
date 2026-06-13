using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Unit : MonoBehaviour
{
    [Header("Параметри фракції та бою")]
    public bool isPlayer = true; // Додано для ШІ та DiplomacyManager
    public string ownerCivName;
    public int health = 100;
    public int attackPower = 25;
    public bool hasAttackedThisTurn;

    [Header("Параметри руху")]
    public int maxMovement = 3;
    public int currentMovement;
    public float moveSpeed = 9f;
    public Vector3Int gridPosition;
    public bool isSelected;
    public bool hasScoutedFog;
    [HideInInspector] public bool fogVisibleToPlayer = true;
    [HideInInspector] public Unit lastAttacker;

    bool isDead;

    // Властивість для TurnManager
    public bool canMove => currentMovement > 0;

    [Header("Візуалізація вибору")]
    public GameObject selectionCircle;
    public SpriteRenderer spriteRenderer;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    private void Awake()
    {
        currentMovement = maxMovement;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (selectionCircle != null) selectionCircle.SetActive(false);
    }

    public void Select()
    {
        isSelected = true;
        if (selectionCircle != null) selectionCircle.SetActive(true);
        if (spriteRenderer != null) spriteRenderer.color = selectedColor;
    }

    public void Deselect()
    {
        isSelected = false;
        if (selectionCircle != null) selectionCircle.SetActive(false);
        if (spriteRenderer != null) spriteRenderer.color = normalColor;
    }

    public bool IsFogVisibleToPlayer() => fogVisibleToPlayer;

    public void SetFogVisibility(bool visibleToPlayer)
    {
        fogVisibleToPlayer = visibleToPlayer;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr != null)
                sr.enabled = visibleToPlayer;
        }

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            if (col != null)
                col.enabled = visibleToPlayer;
        }

        if (!visibleToPlayer && selectionCircle != null)
            selectionCircle.SetActive(false);
    }

    public void ResetTurn() => ResetMovement();
    public void ResetMovement()
    {
        currentMovement = maxMovement;
        hasAttackedThisTurn = false;
    }

    public bool CanAttackThisTurn()
    {
        return !hasAttackedThisTurn && attackPower > 0;
    }

    public bool IsDead => isDead || health <= 0;

    // Метод для отримання шкоди
    public void TakeDamage(int damage, Unit attacker = null)
    {
        if (isDead) return;

        if (attacker != null)
            lastAttacker = attacker;

        health -= damage;
        Debug.Log(name + " отримав шкоду. HP: " + health);

        UnitAnimator anim = GetComponent<UnitAnimator>();
        if (anim != null && health > 0)
            anim.PlayHurt();

        if (health <= 0)
        {
            isDead = true;
            if (anim != null)
            {
                anim.PlayDeathAnimation();
                StartCoroutine(DestroyAfterDeath(anim.GetDeathDuration()));
            }
            else
            {
                RemoveFromGame();
            }
        }
    }

    IEnumerator DestroyAfterDeath(float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveFromGame();
    }

    void RemoveFromGame()
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager != null) manager.RemoveUnit(this);
        Destroy(gameObject);
    }

    // Метод для заснування міста
    public void CreateCity()
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager == null) return;
        
        Vector3Int cityPos = gridPosition;
        
        // Перевіряємо чи можна заснувати місто
        if (manager.HasCityAt(cityPos))
        {
            Debug.Log("На цій клітинці вже є місто!");
            return;
        }

        if (!manager.IsValidCitySite(cityPos))
        {
            Debug.Log("Тут не можна заснувати місто — замало відстані до інших міст.");
            return;
        }

        string civName = GetCivName(manager);
        if (!isPlayer && !manager.IsValidAiCitySite(civName, cityPos))
        {
            Debug.Log("AI не може заснувати місто — занадто далеко від своїх міст.");
            return;
        }
        if (manager.cityPrefab != null)
        {
            bool isCapital = manager.allCities.Find(c => c != null && c.ownerCivName == civName) == null;
            string generatedName = CityLabel.GenerateCityName(civName, isCapital);

            Vector3 worldPos = manager.tilemap.GetCellCenterWorld(cityPos);
            GameObject cityObj = Instantiate(manager.cityPrefab, new Vector3(worldPos.x, worldPos.y - 1f, -0.1f), Quaternion.identity);
            cityObj.name = civName + "_" + generatedName;

            City city = cityObj.GetComponent<City>() ?? cityObj.AddComponent<City>();
            city.gridPosition = cityPos;
            city.isPlayerCity = isPlayer;
            city.ownerCivName = civName;
            city.isCapital = isCapital;
            city.cityName = generatedName;
            city.Init(cityPos, manager.tilemap);
            city.SetupLabel(civName, manager.GetCivColor(civName));

            city.ownerCivName = isPlayer ? manager.currentCivName : GetCivName();
            
            // Встановлюємо власника міста через City компонент
            // City не має isPlayer та civName, тому просто реєструємо місто
            
            manager.RegisterCity(city);

            if (isPlayer)
                SaveManager.Instance?.MarkUnsaved();

            Debug.Log(name + " заснував місто " + generatedName + " (" + civName + ")");
            
            // Знищуємо юніта після заснування міста
            manager.RemoveUnit(this);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("City prefab не встановлено в Program1!");
        }
    }
    
    public string GetCivName(Program1 manager = null)
    {
        if (!string.IsNullOrEmpty(ownerCivName))
            return ownerCivName;

        if (isPlayer)
        {
            if (manager == null) manager = Object.FindAnyObjectByType<Program1>();
            if (manager != null && !string.IsNullOrEmpty(manager.currentCivName))
                return manager.currentCivName;
            return PlayerPrefs.GetString("SelectedCiv", "Rome");
        }

        if (name.Contains("Rome")) return "Rome";
        if (name.Contains("America")) return "America";
        if (name.Contains("Egypt")) return "Egypt";
        if (name.Contains("Scythia")) return "Scythia";
        return "Unknown";
    }

    // Метод для перевірки хорошого місця для міста (публічний для доступу з DiplomacyManager)
    public bool IsGoodCityLocation(Vector3Int pos, Program1 manager)
    {
        if (manager == null) return false;
        
        // Перевіряємо, чи є поруч ресурси
        int resourceCount = 0;
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                Vector3Int checkPos = pos + new Vector3Int(x, y, 0);
                if (!manager.IsImpassable(checkPos))
                {
                    resourceCount++;
                }
            }
        }

        return resourceCount >= 5 && manager.IsFarEnoughFromCities(pos);
    }

    // Метод для перевірки чи є юніти ворогами
    bool AreEnemies(Unit otherUnit, Program1 manager = null)
    {
        if (otherUnit == null || otherUnit.isPlayer == isPlayer) return false;

        DiplomacyManager diplomacy = DiplomacyManager.Instance;
        if (diplomacy == null) return false;

        string otherCiv = otherUnit.GetCivName(manager);
        if (string.IsNullOrEmpty(otherCiv) || otherCiv == "Unknown") return false;

        if (isPlayer)
            return diplomacy.IsAtWarWith(otherCiv);

        if (otherUnit.isPlayer)
        {
            if (manager == null) manager = Object.FindAnyObjectByType<Program1>();
            return diplomacy.AreAtWar(GetCivName(manager), manager != null ? manager.currentCivName : PlayerPrefs.GetString("SelectedCiv", "Rome"));
        }

        return diplomacy.AreAtWar(GetCivName(manager), otherCiv);
    }

    public IEnumerator JumpAttack(Unit target, Program1 manager)
    {
        if (target == null || manager == null || manager.tilemap == null)
            yield break;

        if (!CanAttackThisTurn())
            yield break;

        int damage = attackPower;
        CombatSystem combat = Object.FindAnyObjectByType<CombatSystem>();
        if (combat != null)
            damage = combat.GetAttackDamage(this);

        Vector3 startPos = transform.position;
        Vector3 targetCellPos = manager.GetUnitPositionForCell(target.gridPosition);

        UnitAnimator anim = GetComponent<UnitAnimator>();
        bool targetDestroyed = false;
        if (anim != null)
        {
            anim.FaceToward(targetCellPos - transform.position);
            target.lastAttacker = this;
            yield return StartCoroutine(anim.PlayAttackRoutine(target, damage));
            hasAttackedThisTurn = true;
            targetDestroyed = target == null || target.health <= 0;
        }
        else
        {
            Vector3 attackPos = Vector3.Lerp(startPos, targetCellPos, 0.6f);
            float forwardDuration = 0.12f;
            float backDuration = 0.10f;
            float jumpHeight = 0.22f;

            for (float t = 0f; t < forwardDuration; t += Time.deltaTime)
            {
                float k = t / forwardDuration;
                Vector3 p = Vector3.Lerp(startPos, attackPos, k);
                p.y += Mathf.Sin(k * Mathf.PI) * jumpHeight;
                transform.position = p;
                yield return null;
            }

            transform.position = attackPos;
            target.TakeDamage(damage, this);
            hasAttackedThisTurn = true;
            targetDestroyed = target == null || target.IsDead;

            for (float t = 0f; t < backDuration; t += Time.deltaTime)
            {
                float k = t / backDuration;
                Vector3 p = Vector3.Lerp(attackPos, startPos, k);
                p.y += Mathf.Sin((1f - k) * Mathf.PI) * jumpHeight * 0.5f;
                transform.position = p;
                yield return null;
            }

            transform.position = startPos;
        }

        if (targetDestroyed && target != null)
            combat?.NotifyUnitDestroyed(this, target);

        currentMovement = 0;
    }

    public IEnumerator MoveAlongPath(List<Vector3Int> path, Tilemap tilemap, Program1 manager)
    {
        UnitAnimator anim = GetComponent<UnitAnimator>();
        if (path.Count > 0)
        {
            if (anim != null) anim.PlayWalk();
        }

        foreach (var cell in path)
        {
            // Перевіряємо чи юніт ще існує
            if (this == null || gameObject == null)
            {
                Debug.LogWarning("Юніт був знищений під час руху");
                yield break;
            }

            if (manager.IsBlockedByPeacefulEnemyCity(cell, this))
            {
                Debug.Log("Занадто близько до чужого міста — спочатку оголосіть війну.");
                break;
            }

            // Блокуємо вхід на зайняту клітинку.
            // Виняток: якщо там ворог і є війна — виконуємо атаку.
            Unit occupant = manager.GetUnitAt(cell);
            if (occupant != null && occupant != this)
            {
                if (manager.CanUnitsFight(this, occupant))
                {
                    yield return StartCoroutine(JumpAttack(occupant, manager));
                    currentMovement = 0;
                    break;
                }

                if (occupant.isPlayer != this.isPlayer)
                    Debug.Log("Неможливо атакувати — війну не оголошено проти " + occupant.GetCivName(manager));

                break;
            }

            int cost = manager.GetMovementCost(cell);
            if (currentMovement < cost) break;

            Vector3 targetPos = manager.GetUnitPositionForCell(cell);

            if (anim != null)
                anim.FaceToward(targetPos - transform.position);

            float timeout = 2f;
            float elapsed = 0f;

            while (Vector3.Distance(transform.position, targetPos) > 0.005f)
            {
                // Однакова швидкість руху для гравця та AI.
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;

                if (elapsed >= timeout)
                {
                    Debug.LogWarning($"Юніт {name} застряг при русі до {cell}, примусово завершуємо рух");
                    break;
                }

                yield return null;
            }

            transform.position = targetPos;
            Vector3Int oldCell = gridPosition;
            gridPosition = cell;
            manager.UpdateUnitCellIndex(this, oldCell, cell);
            currentMovement -= cost;

            City cityOnCell = manager.GetCityAt(cell);
            if (cityOnCell != null && manager.CanAttackCity(this, cityOnCell))
            {
                yield return manager.StartCoroutine(manager.CaptureCityRoutine(this, cityOnCell));
                yield break;
            }

            if (isPlayer)
                hasScoutedFog = true;

            FogOfWarManager fog = manager.GetFogOfWar();
            if (fog != null)
                fog.OnUnitMoved(this);

            // Дуже мала затримка для плавності руху
            yield return new WaitForSeconds(0.02f);
            
            // Якщо це поселенець і він досяг кінцевої точки, пропонуємо заснувати місто
            if (name.Contains("Settler") && currentMovement <= 0)
            {
                // Для AI юнітів - перевіряємо чи це хороше місце для міста
                if (!isPlayer)
                {
                    // Перевіряємо, чи є поруч ресурси та інші міста
                    bool goodLocation = IsGoodCityLocation(gridPosition, manager);
                    if (goodLocation)
                    {
                        CreateCity();
                    }
                    else
                    {
                        Debug.Log(name + " шукає краще місце для міста...");
                    }
                }
                // Для гравця можна показати UI опцію (поки автоматично)
                else
                {
                    Debug.Log("Поселенець досяг мети. Можна заснувати місто.");
                    // Тут можна додати UI кнопку для заснування міста
                }
            }
        }

        // Завжди зупиняємо анімацію після завершення руху
        if (anim != null) anim.ForceIdle();
    }
}
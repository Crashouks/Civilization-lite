using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Unit : MonoBehaviour
{
    [Header("Параметри фракції та бою")]
    public bool isPlayer = true; // Додано для ШІ та DiplomacyManager
    public int health = 100;      // Додано для бойової системи
    public int attackPower = 25;  // Додано для бойової системи

    [Header("Параметри руху")]
    public int maxMovement = 3;
    public int currentMovement;
    public float moveSpeed = 9f;
    public Vector3Int gridPosition;
    public bool isSelected;
    [HideInInspector] public Unit lastAttacker;

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

    public void ResetTurn() => ResetMovement();
    public void ResetMovement() => currentMovement = maxMovement;

    // Метод для отримання шкоди
    public void TakeDamage(int damage, Unit attacker = null)
    {
        if (attacker != null)
            lastAttacker = attacker;

        health -= damage;
        Debug.Log(name + " отримав шкоду. HP: " + health);

        UnitAnimator anim = GetComponent<UnitAnimator>();
        if (anim != null && health > 0)
            anim.PlayHurt();

        if (health <= 0)
        {
            if (anim != null)
            {
                anim.PlayDeathAnimation();
                StartCoroutine(DestroyAfterDeath(anim.GetDeathDuration()));
            }
            else
            {
                RemoveFromGame();
            }
            if (EconomyManager.Instance != null && lastAttacker != null)
                EconomyManager.Instance.AwardKillReward(lastAttacker, this);

            Program1 manager = Object.FindAnyObjectByType<Program1>();
            if (manager != null) manager.RemoveUnit(this);
            Destroy(gameObject);
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
        
        if (manager.cityPrefab != null)
        {
            string civName = GetCivName(manager);
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

            FogOfWarManager fog = manager.GetFogOfWar();
            if (fog != null) fog.RevealAllPlayerUnits();

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
        if (isPlayer)
        {
            if (manager == null) manager = Object.FindAnyObjectByType<Program1>();
            if (manager != null && !string.IsNullOrEmpty(manager.currentCivName))
                return manager.currentCivName;
            return "Rome";
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
        
        // Перевіряємо відстань до інших міст
        float minDistance = float.MaxValue;
        foreach (City city in manager.allCities)
        {
            if (city != null)
            {
                float distance = Vector3Int.Distance(pos, city.gridPosition);
                minDistance = Mathf.Min(minDistance, distance);
            }
        }
        
        // Місце хороше якщо є ресурси і не занадто близько до інших міст
        return resourceCount >= 5 && minDistance >= 3;
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
            string playerCiv = manager != null ? manager.currentCivName : PlayerPrefs.GetString("SelectedCiv", "Rome");
            return diplomacy.IsAtWarWith(GetCivName(manager));
        }

        return false;
    }

    public IEnumerator JumpAttack(Unit target, Program1 manager)
    {
        if (target == null || manager == null || manager.tilemap == null)
            yield break;

        Vector3 startPos = transform.position;
        Vector3 targetCellPos = manager.tilemap.GetCellCenterWorld(target.gridPosition);
        targetCellPos.y -= 1f;
        targetCellPos.z = -0.1f;

        UnitAnimator anim = GetComponent<UnitAnimator>();
        if (anim != null)
        {
            anim.FaceToward(targetCellPos - transform.position);
            target.lastAttacker = this;
            yield return StartCoroutine(anim.PlayAttackRoutine(target, attackPower));
            yield break;
        }

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
        target.TakeDamage(attackPower, this);

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

            // Блокуємо вхід на зайняту клітинку.
            // Виняток: якщо там ворог і є війна — виконуємо атаку.
            Unit occupant = manager.GetUnitAt(cell);
            if (occupant != null && occupant != this)
            {
                if (occupant.isPlayer != this.isPlayer)
                {
                    if (AreEnemies(occupant, manager))
                    {
                        yield return StartCoroutine(JumpAttack(occupant, manager));
                        currentMovement = 0;
                        break;
                    }

                    Debug.Log("Неможливо атакувати — війну не оголошено проти " + occupant.GetCivName(manager));
                    break;
                }

                break;
            }

            int cost = manager.GetMovementCost(cell);
            if (currentMovement < cost) break;

            Vector3 targetPos = tilemap.GetCellCenterWorld(cell);

            // Зберігаємо твою логіку позиціонування y - 1f
            targetPos.y -= 1f;
            targetPos.z = -0.1f;

            // Додаємо тайм-аут для запобігання безкінечних циклів
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
            gridPosition = cell;
            currentMovement -= cost;

            FogOfWarManager fog = manager.GetFogOfWar();
            if (fog != null)
                fog.OnUnitMoved(this);

            if (isPlayer && FogOfWarManager.Instance != null)
            {
                int sight = UnitTypeHelper.GetKind(this) == UnitTypeHelper.UnitKind.Scout
                    ? FogOfWarManager.Instance.scoutSightRange
                    : FogOfWarManager.Instance.defaultSightRange;
                FogOfWarManager.Instance.RevealAround(cell, sight);
            }

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
        if (anim != null) anim.PlayIdle();
    }
}
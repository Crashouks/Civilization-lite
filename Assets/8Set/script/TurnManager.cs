using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Налаштування ходу")]
    public int currentTurn = 1;

    public bool IsPlayerTurnActive { get; private set; }
    public bool IsAiTurnInProgress { get; private set; }
    public bool IsPlayerDefeated { get; private set; }

    bool lastEndTurnEnabled = true;
    bool endTurnRoutineRunning;
    Program1 cachedProgram;
    Button cachedEndTurnButton;
    Button cachedNextTurnButton;
    Button[] cachedRightButtons;
    bool turnButtonsCached;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        IsPlayerTurnActive = false;
        cachedProgram = Object.FindAnyObjectByType<Program1>();
        RefreshEndTurnButtons();
    }

    public void SetPlayerDefeated(bool defeated = true)
    {
        IsPlayerDefeated = defeated;
        RefreshEndTurnButtons();
    }

    Program1 GetProgram()
    {
        if (cachedProgram == null)
            cachedProgram = Object.FindAnyObjectByType<Program1>();
        return cachedProgram;
    }

    void Update()
    {
        bool canEnd = CanEndPlayerTurn();
        if (canEnd != lastEndTurnEnabled)
            RefreshEndTurnButtons();
    }

    public bool CanPlayerAct()
    {
        return !IsPlayerDefeated && IsPlayerTurnActive && !IsAiTurnInProgress && !endTurnRoutineRunning;
    }

    public bool CanEndPlayerTurn()
    {
        if (!CanPlayerAct())
            return false;

        Program1 manager = GetProgram();
        return manager != null && !manager.IsPlayerActionBusy;
    }

    public void EndTurn()
    {
        if (!CanEndPlayerTurn())
            return;

        StartCoroutine(EndPlayerTurnRoutine());
    }

    public void EndPlayerTurn()
    {
        EndTurn();
    }

    public IEnumerator BeginNewGameTurnCycle()
    {
        if (currentTurn <= 1)
        {
            IsAiTurnInProgress = false;
            IsPlayerTurnActive = true;
            RefreshEndTurnButtons();
            if (GameUI.Instance != null)
                GameUI.Instance.RefreshTurn();
            yield break;
        }

        IsPlayerTurnActive = false;
        IsAiTurnInProgress = true;
        RefreshEndTurnButtons();
        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();

        Program1 manager = GetProgram();
        if (currentTurn > 1 && manager != null)
            yield return manager.StartCoroutine(manager.RunAiTurnPhase());

        IsAiTurnInProgress = false;
        IsPlayerTurnActive = true;
        RefreshEndTurnButtons();
        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();
    }

    public void ResumePlayerTurnAfterLoad()
    {
        IsAiTurnInProgress = false;
        IsPlayerTurnActive = true;
        endTurnRoutineRunning = false;
        RefreshEndTurnButtons();
        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();
    }

    IEnumerator EndPlayerTurnRoutine()
    {
        if (endTurnRoutineRunning)
            yield break;

        endTurnRoutineRunning = true;
        SaveManager.Instance?.MarkUnsaved();
        IsPlayerTurnActive = false;
        IsAiTurnInProgress = true;
        RefreshEndTurnButtons();
        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();

        Program1 manager = GetProgram();
        if (manager != null)
        {
            manager.DeselectUnitForTurnEnd();
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.CollectCityIncome(currentTurn, manager);
            yield return manager.StartCoroutine(manager.RunAiTurnPhase());
        }

        currentTurn++;
        Debug.Log("=== НАЧАТОК ХОДУ " + currentTurn + " ===");

        if (manager != null)
            manager.RefreshAllCityHealth(currentTurn);

        if (FogOfWarManager.Instance != null)
            FogOfWarManager.Instance.RefreshVisibility();

        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();

        IsAiTurnInProgress = false;
        IsPlayerTurnActive = true;
        endTurnRoutineRunning = false;
        RefreshEndTurnButtons();
        if (GameUI.Instance != null)
            GameUI.Instance.RefreshTurn();
    }

    public void RefreshEndTurnButtons()
    {
        CacheTurnButtons();
        bool canEnd = CanEndPlayerTurn();
        lastEndTurnEnabled = canEnd;

        SetButtonState(cachedEndTurnButton, canEnd);
        SetButtonState(cachedNextTurnButton, canEnd);

        if (cachedRightButtons != null)
        {
            foreach (Button btn in cachedRightButtons)
                SetButtonState(btn, canEnd);
        }
    }

    void CacheTurnButtons()
    {
        if (turnButtonsCached)
            return;

        cachedEndTurnButton = FindSceneButton("EndTurnButton");
        cachedNextTurnButton = FindSceneButton("NextTurnButton");

        GameObject rightButtons = FindSceneObject("RightButtons");
        if (rightButtons != null)
            cachedRightButtons = rightButtons.GetComponentsInChildren<Button>(true);
        else
            cachedRightButtons = System.Array.Empty<Button>();

        turnButtonsCached = true;
    }

    static void SetButtonState(Button btn, bool interactable)
    {
        if (btn == null)
            return;

        btn.interactable = interactable;
    }

    static Button FindSceneButton(string objectName)
    {
        GameObject obj = FindSceneObject(objectName);
        return obj != null ? obj.GetComponent<Button>() : null;
    }

    static GameObject FindSceneObject(string objectName)
    {
        Program1 manager = Object.FindAnyObjectByType<Program1>();
        if (manager != null)
        {
            GameObject found = manager.FindObjectByNameInScenePublic(objectName);
            if (found != null)
                return found;
        }

        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindChildRecursive(root.transform, objectName);
            if (t != null)
                return t.gameObject;
        }

        return null;
    }

    static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    public void ResetAllUnits(List<Unit> units)
    {
        foreach (Unit u in units)
        {
            if (u != null)
                u.ResetTurn();
        }
    }
}

using UnityEngine;

/// <summary>
/// Attach to Canvas if MainMenu script is missing. Forwards to MainMenu bootstrap.
/// </summary>
[DefaultExecutionOrder(-200)]
public class MainMenuSceneBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (FindAnyObjectByType<MainMenu>() != null) return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MainMenuSceneBootstrap: No Canvas found. Open Assets/MainMenu/Scenes/NewMenu.unity");
            return;
        }

        canvas.gameObject.AddComponent<MainMenu>();
        Debug.Log("MainMenu auto-added to Canvas. Save the scene after verifying Play mode.");
    }
}

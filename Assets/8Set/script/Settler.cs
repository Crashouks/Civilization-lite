using UnityEngine;
using UnityEngine.InputSystem;

public class Settler : Unit
{
    Program1 cachedGame;

    void Update()
    {
        if (Keyboard.current == null || !isSelected || !Keyboard.current.bKey.wasPressedThisFrame)
            return;

        if (cachedGame == null)
            cachedGame = Object.FindAnyObjectByType<Program1>();
        if (cachedGame == null || !cachedGame.CanPlayerAct())
            return;

        CreateCity();
    }
}

using System;

[Serializable]
public struct SaveSlotInfo
{
    public int slot;
    public bool exists;
    public int turnNumber;
    public string playerCiv;

    public string GetDisplayLine()
    {
        if (!exists)
            return "Порожньо";

        string civ = string.IsNullOrEmpty(playerCiv) ? "?" : playerCiv;
        return civ + " · хід " + turnNumber;
    }
}

[Serializable]
public class CloudSlotsResponse
{
    public bool success;
    public CloudSlotEntry[] slots;
}

[Serializable]
public class CloudSlotEntry
{
    public string saveName;
    public bool exists;
    public int turnNumber;
    public string playerCiv;
}

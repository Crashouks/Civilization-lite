using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public float mapSeed;
    public int mapWidth;
    public int mapHeight;
    public int currentTurn;
    public string playerCiv;
    public int playerCoins;
    public bool isAtWar;
    public List<string> enemyNations = new List<string>();
    public bool[] exploredFlat;
    public List<UnitSaveData> units = new List<UnitSaveData>();
    public List<CitySaveData> cities = new List<CitySaveData>();
}

[Serializable]
public class UnitSaveData
{
    public string unitType;
    public int x;
    public int y;
    public int health;
    public int currentMovement;
    public bool isPlayer;
    public string civName;
}

[Serializable]
public class CitySaveData
{
    public int x;
    public int y;
    public bool isPlayerCity;
    public string ownerCivName;
    public string cityName;
}

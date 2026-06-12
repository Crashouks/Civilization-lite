using UnityEngine;

public static class UnitTypeHelper
{
    public enum UnitKind { Settler, Scout, Warrior, Archer, Other }

    public static UnitKind GetKind(Unit unit)
    {
        if (unit == null) return UnitKind.Other;
        return GetKind(unit.name);
    }

    public static UnitKind GetKind(string unitName)
    {
        if (string.IsNullOrEmpty(unitName)) return UnitKind.Other;
        if (unitName.Contains("Settler")) return UnitKind.Settler;
        if (unitName.Contains("Scout")) return UnitKind.Scout;
        if (unitName.Contains("Warrior") || unitName.Contains("Soldier")) return UnitKind.Warrior;
        if (unitName.Contains("Archer")) return UnitKind.Archer;
        return UnitKind.Other;
    }

    public static string GetTypeName(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Settler: return "Settler";
            case UnitKind.Scout: return "Scout";
            case UnitKind.Warrior: return "Warrior";
            case UnitKind.Archer: return "Archer";
            default: return "Unit";
        }
    }
}

using UnityEngine;

public static class UnitSetup
{
    const int BaseSortingOrder = 12;

    public static void Configure(GameObject obj, string unitName, bool isPlayer)
    {
        if (obj == null) return;

        obj.name = unitName;

        Unit unit = obj.GetComponent<Unit>() ?? obj.AddComponent<Unit>();
        unit.isPlayer = isPlayer;
        if (unit.moveSpeed < 6f)
            unit.moveSpeed = 9f;

        if (unitName.Contains("Settler") && obj.GetComponent<Settler>() == null)
            obj.AddComponent<Settler>();

        if (!isPlayer && obj.GetComponent<UnitAI>() == null)
            obj.AddComponent<UnitAI>();

        Collider2D col = obj.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = obj.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            col = box;
        }
        else col.isTrigger = true;

        if (col is BoxCollider2D boxCol)
        {
            SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
            boxCol.size = sr != null && sr.sprite != null ? sr.sprite.bounds.size : new Vector2(1f, 1.8f);
        }

        Animator anim = obj.GetComponent<Animator>() ?? obj.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            anim.enabled = true;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.updateMode = AnimatorUpdateMode.Normal;
        }

        if (obj.GetComponent<UnitVisualFix>() == null)
            obj.AddComponent<UnitVisualFix>();

        Transform body = obj.transform.Find("Body");
        ApplySorting(body != null ? body : obj.transform);

        UnitAnimator visuals = obj.GetComponent<UnitAnimator>() ?? obj.AddComponent<UnitAnimator>();
        visuals.EnsureReady();
        visuals.PlayIdle();
    }

    public static void ApplySorting(Transform root)
    {
        if (root == null) return;

        Transform body = root.name == "Body" ? root : root.Find("Body");
        if (body != null)
        {
            SetSortingOrder(body, "Back leg", BaseSortingOrder);
            SetSortingOrder(body, "Back arm", BaseSortingOrder + 1);
            SetSortingOrderDeep(body, "shield", BaseSortingOrder + 1);
            SetSortingOrder(body, "Body", BaseSortingOrder + 2);
            SetSortingOrder(body, "Front leg", BaseSortingOrder + 3);
            SetSortingOrder(body, "Front arm", BaseSortingOrder + 4);
            SetSortingOrderDeep(body, "Weapon", BaseSortingOrder + 5);
            SetSortingOrderDeep(body, "sword", BaseSortingOrder + 5);
            SetSortingOrder(body, "Head", BaseSortingOrder + 6);

            foreach (SpriteRenderer sr in body.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                sr.enabled = true;
                if (sr.sortingOrder < BaseSortingOrder)
                    sr.sortingOrder = BaseSortingOrder + 2;
            }
            return;
        }

        SpriteRenderer single = root.GetComponentInChildren<SpriteRenderer>();
        if (single != null)
            single.sortingOrder = BaseSortingOrder + 4;
    }

    static void SetSortingOrder(Transform body, string partName, int order)
    {
        Transform part = body.Find(partName);
        if (part == null) return;

        SpriteRenderer sr = part.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = order;
    }

    static void SetSortingOrderDeep(Transform root, string partName, int order)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != partName) continue;

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = order;
        }
    }
}

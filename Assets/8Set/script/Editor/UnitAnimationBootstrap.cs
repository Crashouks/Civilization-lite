#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class UnitAnimationBootstrap
{
    const string Key = "CivLite_Anim_v6";

    static UnitAnimationBootstrap()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorPrefs.GetBool(Key, false))
                Run(silent: true);
        };
    }

    [MenuItem("Tools/Civilization/Setup Unit Animations")]
    public static void RunManual()
    {
        Run(silent: false);
        EditorUtility.DisplayDialog("Готово", "Анімації юнітів налаштовано.", "OK");
    }

    static void Run(bool silent = true)
    {
        int c = 0, p = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/Miniature Army 2D V.1" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Gemini")) continue;
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl != null && NeedsSetup(ctrl) && SetupController(ctrl)) { EditorUtility.SetDirty(ctrl); c++; }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Miniature Army 2D V.1/Prefab" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab == null) continue;
            if (prefab.GetComponentInChildren<Animator>(true) == null) continue;

            bool ch = false;
            if (prefab.GetComponent<Unit>() == null) { prefab.AddComponent<Unit>(); ch = true; }
            if (prefab.GetComponent<UnitAnimator>() == null) { prefab.AddComponent<UnitAnimator>(); ch = true; }
            if (prefab.GetComponent<UnitVisualFix>() == null) { prefab.AddComponent<UnitVisualFix>(); ch = true; }
            var a = prefab.GetComponent<Animator>() ?? prefab.GetComponentInChildren<Animator>(true);
            if (a != null && a.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                ch = true;
            }
            if (ch) { EditorUtility.SetDirty(prefab); p++; }
        }

        EditorPrefs.SetBool(Key, true);
        AssetDatabase.SaveAssets();
        if (!silent || c > 0 || p > 0)
            Debug.Log($"[UnitAnimation] {c} controllers, {p} prefabs configured.");
    }

    static bool NeedsSetup(AnimatorController ctrl)
    {
        bool hasWalk = false, hasDie = false;
        foreach (var p in ctrl.parameters)
        {
            if (p.name == "IsWalking") hasWalk = true;
            if (p.name == "Die") hasDie = true;
        }
        return !hasWalk || !hasDie;
    }

    static bool SetupController(AnimatorController ctrl)
    {
        AddParam(ctrl, "IsWalking", AnimatorControllerParameterType.Bool);
        AddParam(ctrl, "Attack", AnimatorControllerParameterType.Trigger);
        AddParam(ctrl, "Hurt", AnimatorControllerParameterType.Trigger);
        AddParam(ctrl, "Die", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var idle = Find(sm, "idle");
        var walk = Find(sm, "walk");
        var attack = Find(sm, "attack");
        var hurt = Find(sm, "hurt");
        var die = Find(sm, "die");
        if (idle == null || walk == null) return false;

        Link(idle, walk, AnimatorConditionMode.If, "IsWalking", false);
        Link(walk, idle, AnimatorConditionMode.IfNot, "IsWalking", false);
        if (attack != null)
        {
            AnyLink(sm, attack, "Attack");
            Link(attack, idle, AnimatorConditionMode.IfNot, "IsWalking", true, 0.9f);
        }
        if (hurt != null)
        {
            AnyLink(sm, hurt, "Hurt");
            Link(hurt, idle, AnimatorConditionMode.IfNot, "IsWalking", true, 0.9f);
        }
        if (die != null)
        {
            AnyLink(sm, die, "Die");
            die.writeDefaultValues = false;
        }
        return true;
    }

    static void AddParam(AnimatorController c, string n, AnimatorControllerParameterType t)
    {
        foreach (var p in c.parameters) if (p.name == n) return;
        c.AddParameter(n, t);
    }

    static AnimatorState Find(AnimatorStateMachine sm, string n)
    {
        foreach (ChildAnimatorState ch in sm.states)
            if (ch.state.name == n) return ch.state;
        return null;
    }

    static bool Has(AnimatorState from, AnimatorState to)
    {
        foreach (var t in from.transitions) if (t.destinationState == to) return true;
        return false;
    }

    static bool HasAny(AnimatorStateMachine sm, AnimatorState to)
    {
        foreach (var t in sm.anyStateTransitions) if (t.destinationState == to) return true;
        return false;
    }

    static void Link(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string param, bool exit, float exitTime = 0f)
    {
        if (from == null || to == null || Has(from, to)) return;
        var t = from.AddTransition(to);
        t.hasExitTime = exit;
        t.exitTime = exitTime;
        t.duration = 0.1f;
        t.hasFixedDuration = true;
        t.AddCondition(mode, 0, param);
    }

    static void AnyLink(AnimatorStateMachine sm, AnimatorState to, string param)
    {
        if (HasAny(sm, to)) return;
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, param);
    }
}
#endif

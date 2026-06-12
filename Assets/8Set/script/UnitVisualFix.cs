using UnityEngine;

public class UnitVisualFix : MonoBehaviour
{
    static readonly Vector3 FrontLegPos = new Vector3(0.102f, -0.26f, 0.1f);
    static readonly Vector3 BackLegPos = new Vector3(-0.112f, -0.26f, 0.2f);
    static readonly Vector3 BackLegRot = new Vector3(0f, 0f, -17.518f);

    Transform body;
    Animator animator;

    void Awake()
    {
        body = transform.Find("Body");
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        UnitSetup.ApplySorting(body != null ? body : transform);
    }

    void LateUpdate()
    {
        if (body == null) return;

        EnsureLegsActive();

        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("walk"))
            return;

        FixIdleLegPose(body.Find("Front leg"), FrontLegPos, Vector3.zero);
        FixIdleLegPose(body.Find("Back leg"), BackLegPos, BackLegRot);
    }

    void EnsureLegsActive()
    {
        foreach (string legName in new[] { "Front leg", "Back leg" })
        {
            Transform leg = body.Find(legName);
            if (leg == null) continue;

            leg.gameObject.SetActive(true);
            SpriteRenderer sr = leg.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                if (sr.sortingOrder < 12)
                    sr.sortingOrder = legName == "Front leg" ? 15 : 13;
            }
        }
    }

    static void FixIdleLegPose(Transform leg, Vector3 pos, Vector3 rot)
    {
        if (leg == null) return;
        if (leg.localPosition.y < -0.35f || Mathf.Abs(leg.localPosition.y - pos.y) > 0.2f)
        {
            leg.localPosition = pos;
            leg.localEulerAngles = rot;
        }
    }
}

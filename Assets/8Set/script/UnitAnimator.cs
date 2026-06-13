using System.Collections;
using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    Animator animator;
    Transform flipRoot;

    public void EnsureReady()
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        if (flipRoot == null)
        {
            Transform body = transform.Find("Body");
            flipRoot = body != null ? body : transform;
        }
    }

    public void PlayIdle()
    {
        ForceIdle();
    }

    public void PlayWalk()
    {
        EnsureReady();
        SetWalking(true);
    }

    void SetWalking(bool walking)
    {
        if (animator == null) return;

        if (HasParameter("IsWalking"))
            animator.SetBool("IsWalking", walking);

        if (walking)
        {
            ResetAllTriggers();
            if (animator.HasState(0, Animator.StringToHash("walk")))
                animator.Play("walk", 0, 0f);
            else
                SetTrigger("walk");
        }
        else
        {
            ForceIdle();
            return;
        }

        animator.Update(0f);
    }

    public void PlayAttack()
    {
        EnsureReady();
        ResetAllTriggers();
        if (HasParameter("IsWalking"))
            animator.SetBool("IsWalking", false);

        if (animator.HasState(0, Animator.StringToHash("attack")))
            animator.Play("attack", 0, 0f);

        SetTrigger("Attack");
        SetTrigger("attack");
        SetTrigger("ToAttack");
        animator.Update(0f);
    }

    public void PlayHurt()
    {
        EnsureReady();
        SetTrigger("Hurt");
        SetTrigger("hurt");
    }

    public void PlayAttackAnimation() => PlayAttack();

    public void PlayDeathAnimation()
    {
        EnsureReady();
        ResetAllTriggers();
        if (HasParameter("IsWalking"))
            animator.SetBool("IsWalking", false);
        SetTrigger("Die");
        SetTrigger("die");
    }

    public void PlaySettleAnimation()
    {
        EnsureReady();
        ForceIdle();
    }

    public float GetAttackDuration()
    {
        EnsureReady();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name.ToLower().Contains("attack"))
                    return Mathf.Clamp(clip.length, 0.35f, 1.1f);
            }
        }
        return 0.55f;
    }

    public float GetDeathDuration() => 0.8f;

    public void FaceToward(Vector3 direction)
    {
        if (Mathf.Abs(direction.x) < 0.01f) return;

        EnsureReady();
        Vector3 scale = flipRoot.localScale;
        scale.x = direction.x < 0f ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        flipRoot.localScale = scale;
    }

    public IEnumerator PlayAttackRoutine(Unit target, int damage)
    {
        if (target == null) yield break;

        FaceToward(target.transform.position - transform.position);
        PlayAttack();

        float duration = GetAttackDuration();
        float hitTime = duration * 0.45f;
        float elapsed = 0f;
        bool hitApplied = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (!hitApplied && elapsed >= hitTime)
            {
                hitApplied = true;
                if (target != null)
                {
                    Unit attacker = GetComponent<Unit>() ?? GetComponentInParent<Unit>();
                    target.TakeDamage(damage, attacker);
                }
            }
            yield return null;
        }

        ForceIdle();
    }

    public IEnumerator WalkTo(Vector3 worldTarget)
    {
        PlayWalk();
        while (Vector3.Distance(transform.position, worldTarget) > 0.005f)
            yield return null;
        ForceIdle();
    }

    public void ForceIdle()
    {
        EnsureReady();
        if (animator == null) return;

        ResetAllTriggers();
        if (HasParameter("IsWalking"))
            animator.SetBool("IsWalking", false);

        if (animator.HasState(0, Animator.StringToHash("idle")))
            animator.Play("idle", 0, 0f);
        else
            SetTrigger("idle");

        animator.Update(0f);
    }

    void ResetAllTriggers()
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(p.name);
        }
    }

    void SetTrigger(string name)
    {
        if (animator != null && HasParameter(name))
            animator.SetTrigger(name);
    }

    bool HasParameter(string name)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == name) return true;
        }
        return false;
    }
}

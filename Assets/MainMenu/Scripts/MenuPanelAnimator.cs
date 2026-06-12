using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuPanelAnimator : MonoBehaviour
{
    public void Show(bool instant = false)
    {
        gameObject.SetActive(true);
        if (instant)
        {
            var cg = GetComponent<CanvasGroup>();
            cg.alpha = 1f;
            transform.localScale = Vector3.one;
            return;
        }
        StopAllCoroutines();
        StartCoroutine(Animate(0f, 1f, 0.92f, 1f, 0f, 1f));
    }

    public void Hide(System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(HideRoutine(onComplete));
    }

    IEnumerator HideRoutine(System.Action onComplete)
    {
        yield return Animate(1f, 0f, 1f, 0.96f, 1f, 0f);
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    IEnumerator Animate(float aFrom, float aTo, float sFrom, float sTo, float fadeFrom, float fadeTo)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        float t = 0f;
        const float duration = 0.32f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            cg.alpha = Mathf.Lerp(aFrom, aTo, k);
            transform.localScale = Vector3.one * Mathf.Lerp(sFrom, sTo, k);
            yield return null;
        }

        cg.alpha = aTo;
        transform.localScale = Vector3.one * sTo;
    }
}

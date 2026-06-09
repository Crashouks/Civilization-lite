using UnityEngine;
using System.Collections;

public class UnitAnimator : MonoBehaviour
{
    private Unit unit;
    private SpriteRenderer spriteRenderer;
    private Vector3 lastPosition;
    private bool isMoving = false;
    
    [Header("Налаштування анімації")]
    public float animationSpeed = 0.2f;
    public float bounceHeight = 0.15f;
    public float bounceSpeed = 8f;
    
    void Start()
    {
        unit = GetComponent<Unit>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        lastPosition = transform.position;
        
        // Завжди запускаємо просту анімацію через корутини
        StartCoroutine(SimpleAnimation());
    }
    
    void Update()
    {
        // Перевіряємо чи юніт рухається
        if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            if (!isMoving)
            {
                StartMovingAnimation();
            }
            isMoving = true;
        }
        else
        {
            if (isMoving)
            {
                StopMovingAnimation();
            }
            isMoving = false;
        }
        
        lastPosition = transform.position;
    }
    
    void StartMovingAnimation()
    {
        // Завжди використовуємо корутини анімації, оскільки Animator параметри відсутні
        StartCoroutine(MoveBounce());
    }
    
    void StopMovingAnimation()
    {
        // Нічого не робимо - корутини зупиняться автоматично
        // Animator не використовується, оскільки параметри відсутні
    }
    
    IEnumerator SimpleAnimation()
    {
        while (true)
        {
            // Проста idle анімація - легке підстрибування
            float time = Time.time;
            Vector3 basePos = transform.position;
            
            for (float t = 0; t < 2f; t += Time.deltaTime)
            {
                float bounce = Mathf.Sin(time * bounceSpeed + t * Mathf.PI) * bounceHeight * 0.3f;
                transform.position = basePos + Vector3.up * bounce;
                yield return null;
            }
            
            yield return null;
        }
    }
    
    IEnumerator MoveBounce()
    {
        Vector3 basePos = transform.position;
        float startTime = Time.time;
        
        while (isMoving)
        {
            float elapsed = Time.time - startTime;
            float bounce = Mathf.Sin(elapsed * bounceSpeed) * bounceHeight;
            transform.position = basePos + Vector3.up * bounce;
            yield return null;
        }
        
        // Повертаємо на базову позицію
        transform.position = basePos;
    }
    
    public void PlayAttackAnimation()
    {
        // Використовуємо корутину атаки замість Animator
        StartCoroutine(AttackBounce());
    }
    
    IEnumerator AttackBounce()
    {
        Vector3 basePos = transform.position;
        
        // Анімація атаки - різкий стрибок вперед і назад
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            transform.position = basePos + Vector3.right * (t * 2f);
            yield return null;
        }
        
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            transform.position = basePos + Vector3.right * (0.4f - t * 2f);
            yield return null;
        }
        
        transform.position = basePos;
    }
    
    public void PlayDeathAnimation()
    {
        // Використовуємо корутину смерті замість Animator
        StartCoroutine(DeathFade());
    }
    
    IEnumerator DeathFade()
    {
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            Vector3 baseScale = transform.localScale;
            
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                spriteRenderer.color = Color.Lerp(startColor, Color.clear, t);
                transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, t);
                transform.Rotate(0, 0, t * 360f);
                yield return null;
            }
        }
    }
    
    public void PlaySettleAnimation()
    {
        // Використовуємо корутину заснування міста замість Animator
        StartCoroutine(SettleBounce());
    }
    
    IEnumerator SettleBounce()
    {
        Vector3 basePos = transform.position;
        
        // Анімація заснування міста - кілька радісних стрибків
        for (int i = 0; i < 3; i++)
        {
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float bounce = Mathf.Sin(t * Mathf.PI / 0.3f) * bounceHeight * 2f;
                transform.position = basePos + Vector3.up * bounce;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        transform.position = basePos;
    }
}

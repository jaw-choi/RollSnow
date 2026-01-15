using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerInvincibility : MonoBehaviour
{
    [Header("Invincibility")]
    [SerializeField] private Transform spriteRoot;
    [SerializeField] private float colorChangeInterval = 0.08f;
    [SerializeField] private float colorTransitionSeconds = 0.05f;
    [SerializeField] private float colorResetSeconds = 0.12f;

    [Header("Obstacle Impact")]
    [SerializeField] private float obstacleKnockbackForce = 12f;
    [SerializeField] private float obstacleUpForce = 4f;
    [SerializeField] private float obstacleTorque = 45f;
    [SerializeField] private float obstacleDestroyDelay = 0.35f;
    [SerializeField] private bool disableObstacleOnImpact = true;
    [SerializeField] private float obstacleFallbackBounceDistance = 1.2f;
    [SerializeField] private float obstacleFallbackBounceDuration = 0.3f;
    [SerializeField] private float obstacleFallbackBounceAmplitude = 0.2f;
    [SerializeField] private float obstacleFallbackSpinDegreesPerSecond = 540f;

    [Header("Bonus Score")]
    [SerializeField] private int bonusScore = 25;
    [SerializeField] private string bonusScoreFormat = "BONUS +{0}";
    [SerializeField] private Color bonusScoreColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float bonusScoreFontSize = 3f;
    [SerializeField] private Vector3 bonusScoreOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private float bonusScorePopupLifetime = 0.8f;
    [SerializeField] private float bonusScorePopupRise = 1f;

    SpriteRenderer[] spriteRenderers;
    Color[] spriteBaseColors;
    readonly HashSet<int> hitObstacleIds = new HashSet<int>();
    Coroutine colorRoutine;
    Coroutine invincibilityRoutine;
    Coroutine colorResetRoutine;
    float invincibleUntil;
    bool invincible;

    public bool IsInvincible => invincible;

    void Awake()
    {
        CacheRenderers();
    }

    public void SetSpriteRoot(Transform root)
    {
        spriteRoot = root;
        CacheRenderers();
    }

    public void Activate(float duration)
    {
        duration = Mathf.Max(0f, duration);
        if (duration <= 0f)
            return;

        float targetUntil = Time.time + duration;
        if (targetUntil > invincibleUntil)
            invincibleUntil = targetUntil;

        if (!invincible)
        {
            invincible = true;
            CacheRenderers();
            StartColorRoutine();
        }

        if (invincibilityRoutine == null)
            invincibilityRoutine = StartCoroutine(InvincibilityTimer());
    }

    public void ResetState()
    {
        invincible = false;
        invincibleUntil = 0f;
        hitObstacleIds.Clear();

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        if (colorResetRoutine != null)
        {
            StopCoroutine(colorResetRoutine);
            colorResetRoutine = null;
        }

        RestoreBaseColors();
    }

    public void HandleObstacleHit(Component obstacle)
    {
        if (!invincible || obstacle == null)
            return;

        GameObject obstacleObject = obstacle.gameObject;
        int id = obstacleObject.GetInstanceID();
        if (!hitObstacleIds.Add(id))
            return;

        DisableObstacleColliders(obstacleObject);
        ApplyObstacleImpact(obstacle);
        AwardBonusScore(obstacleObject.transform.position);
    }

    void CacheRenderers()
    {
        Transform root = spriteRoot != null ? spriteRoot : transform;
        spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteBaseColors = null;
            return;
        }

        spriteBaseColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var renderer = spriteRenderers[i];
            spriteBaseColors[i] = renderer != null ? renderer.color : Color.white;
        }
    }

    void StartColorRoutine()
    {
        if (colorRoutine != null)
            StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(ColorFlickerRoutine());
    }

    IEnumerator InvincibilityTimer()
    {
        while (Time.time < invincibleUntil)
            yield return null;

        EndInvincibility();
    }

    IEnumerator ColorFlickerRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            yield break;

        Color current = spriteRenderers[0] != null ? spriteRenderers[0].color : Color.white;
        float interval = Mathf.Max(0.01f, colorChangeInterval);
        float transition = Mathf.Max(0.01f, colorTransitionSeconds);

        while (invincible)
        {
            Color target = RandomVividColor();
            float elapsed = 0f;
            while (elapsed < transition && invincible)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transition);
                Color blended = Color.Lerp(current, target, t);
                ApplyColor(blended);
                yield return null;
            }

            ApplyColor(target);
            current = target;

            if (interval > 0f)
                yield return new WaitForSeconds(interval);
            else
                yield return null;
        }
    }

    void ApplyColor(Color color)
    {
        if (spriteRenderers == null || spriteBaseColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var renderer = spriteRenderers[i];
            if (renderer == null)
                continue;
            var c = color;
            c.a = spriteBaseColors[i].a;
            renderer.color = c;
        }
    }

    Color RandomVividColor()
    {
        return Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f, 1f, 1f);
    }

    void EndInvincibility()
    {
        invincible = false;
        invincibleUntil = 0f;
        hitObstacleIds.Clear();

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        if (colorRoutine != null)
        {
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        if (colorResetRoutine != null)
            StopCoroutine(colorResetRoutine);
        colorResetRoutine = StartCoroutine(ResetColorsRoutine());
    }

    IEnumerator ResetColorsRoutine()
    {
        if (spriteRenderers == null || spriteBaseColors == null || spriteRenderers.Length == 0)
            yield break;

        float duration = Mathf.Max(0f, colorResetSeconds);
        if (duration <= 0f)
        {
            RestoreBaseColors();
            yield break;
        }

        Color[] startColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            startColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var renderer = spriteRenderers[i];
                if (renderer == null)
                    continue;
                renderer.color = Color.Lerp(startColors[i], spriteBaseColors[i], t);
            }
            yield return null;
        }

        RestoreBaseColors();
    }

    void RestoreBaseColors()
    {
        if (spriteRenderers == null || spriteBaseColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var renderer = spriteRenderers[i];
            if (renderer == null)
                continue;
            renderer.color = spriteBaseColors[i];
        }
    }

    void DisableObstacleColliders(GameObject obstacleObject)
    {
        var colliders3d = obstacleObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders3d.Length; i++)
            colliders3d[i].enabled = false;

        var colliders2d = obstacleObject.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2d.Length; i++)
            colliders2d[i].enabled = false;
    }

    void ApplyObstacleImpact(Component obstacle)
    {
        Vector3 origin = transform.position;
        Vector3 targetPos = obstacle.transform.position;
        Vector3 direction = targetPos - origin;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.up;
        direction.Normalize();

        bool handled = false;
        var collider3d = obstacle as Collider;
        Rigidbody rb = collider3d != null ? collider3d.attachedRigidbody : null;
        if (rb == null)
            rb = obstacle.GetComponent<Rigidbody>();

        if (rb != null && !rb.isKinematic)
        {
            Vector3 force = (direction + Vector3.up * obstacleUpForce).normalized * obstacleKnockbackForce;
            rb.AddForce(force, ForceMode.Impulse);
            Vector3 torqueAxis = Random.onUnitSphere;
            rb.AddTorque(torqueAxis * obstacleTorque, ForceMode.Impulse);
            handled = true;
        }

        var collider2d = obstacle as Collider2D;
        Rigidbody2D rb2d = collider2d != null ? collider2d.attachedRigidbody : null;
        if (!handled)
        {
            if (rb2d == null)
                rb2d = obstacle.GetComponent<Rigidbody2D>();

            if (rb2d != null && rb2d.bodyType == RigidbodyType2D.Dynamic)
            {
                Vector2 dir2 = new Vector2(direction.x, direction.y).normalized;
                Vector2 force2 = (dir2 + Vector2.up * obstacleUpForce).normalized * obstacleKnockbackForce;
                rb2d.AddForce(force2, ForceMode2D.Impulse);
                float torque = obstacleTorque * (Random.value < 0.5f ? -1f : 1f);
                rb2d.AddTorque(torque, ForceMode2D.Impulse);
                handled = true;
            }
        }

        if (!handled)
            StartCoroutine(BoingTransformRoutine(obstacle.transform, direction));

        AudioManager.instance?.PlaySfx(AudioManager.Sfx.ObstacleHit);

        if (disableObstacleOnImpact)
        {
            if (obstacleDestroyDelay > 0f)
                StartCoroutine(DisableObstacleAfterDelay(obstacle.gameObject, obstacleDestroyDelay));
            else
                obstacle.gameObject.SetActive(false);
        }
        else
        {
            if (obstacleDestroyDelay > 0f)
                Destroy(obstacle.gameObject, obstacleDestroyDelay);
            else
                Destroy(obstacle.gameObject);
        }
    }

    IEnumerator BoingTransformRoutine(Transform target, Vector3 direction)
    {
        if (target == null)
            yield break;

        float duration = Mathf.Max(0.01f, obstacleFallbackBounceDuration);
        float distance = Mathf.Max(0.01f, obstacleFallbackBounceDistance);
        float bounce = Mathf.Max(0f, obstacleFallbackBounceAmplitude);
        float spin = obstacleFallbackSpinDegreesPerSecond;
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
            float boing = Mathf.Sin(t * Mathf.PI) * bounce;
            float offset = distance * ease + boing;
            target.position = startPos + direction * offset;
            if (!Mathf.Approximately(spin, 0f))
            {
                float angle = spin * elapsed;
                target.rotation = startRot * Quaternion.Euler(0f, 0f, angle);
            }
            yield return null;
        }
    }

    IEnumerator DisableObstacleAfterDelay(GameObject target, float delay)
    {
        if (target == null)
            yield break;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (target != null)
            target.SetActive(false);
    }

    void AwardBonusScore(Vector3 position)
    {
        if (bonusScore <= 0)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.AddBonusScore(bonusScore);

        SpawnBonusPopup(position);
    }

    void SpawnBonusPopup(Vector3 position)
    {
        if (bonusScorePopupLifetime <= 0f)
            return;

        GameObject popup = new GameObject("BonusScorePopup");
        popup.transform.position = position + bonusScoreOffset;

        var text = popup.AddComponent<TextMeshPro>();
        text.text = string.Format(bonusScoreFormat, bonusScore);
        text.fontSize = bonusScoreFontSize;
        text.color = bonusScoreColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;

        var fade = popup.AddComponent<FloatingFadeEffect>();
        fade.Configure(new Vector3(0f, bonusScorePopupRise, 0f), bonusScorePopupLifetime, false, true);
    }
}

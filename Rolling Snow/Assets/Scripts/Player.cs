using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Tooltip("Growth rate in scale units per second")]
    public float growthRate = 0.02f;
    [Tooltip("Maximum uniform scale")]
    public float maxScale = 1f;
    [SerializeField] private Animator deathAnimator;
    [SerializeField] private Animator deathEffectAnimator;
    [SerializeField] private Transform deathEffectAnchor;
    [SerializeField] private Transform spriteRoot;
    [SerializeField] private string deathTriggerName = "Death";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private float deathAnimationSeconds = 0.8f;
    [SerializeField] private float deathAnimationLogInterval = 0.2f;
    [SerializeField] private bool disableSpriteOnGameOver = true;

    Vector3 baseScale;
    Vector3 initialBaseScale;
    float externalScaleMultiplier = 1f;
    Camera cachedCamera;
    bool isDying = false;
    Coroutine deathRoutine;
    Animator activeDeathAnimator;

    void Awake()
    {
        baseScale = transform.localScale;
        if (baseScale.x > maxScale)
            baseScale = Vector3.one * maxScale;

        initialBaseScale = baseScale;

        if (deathAnimator == null)
            deathAnimator = GetComponentInChildren<Animator>();

        CacheCamera();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Update()
    {
        if (isDying)
            return;

        // Increase player scale over time (uniformly)
        if (baseScale.x < maxScale)
        {
            var delta = Vector3.one * growthRate * Time.deltaTime;
            baseScale += delta;
            if (baseScale.x > maxScale)
                baseScale = Vector3.one * maxScale;
        }

        ApplyScale();

        // Check if player left the camera viewport -> game over
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            var cam = cachedCamera;
            if (cam == null || cam.Equals(null))
            {
                CacheCamera();
                cam = cachedCamera;
            }

            if (cam != null && !cam.Equals(null))
            {
                Vector3 vp = cam.WorldToViewportPoint(transform.position);
                if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
                {
                    Vector3 camPos = cam.transform.position;
                    Vector2 boundaryMin = Vector2.zero;
                    Vector2 boundaryMax = Vector2.one;
                    Debug.Log($"GAME OVER: left screen (CamPos={camPos}, PlayerPos={transform.position}, Viewport={vp}, BoundsMin={boundaryMin}, BoundsMax={boundaryMax})");
                    TriggerGameOver();
                }
            }
        }
    }

    public void ResetPlayerData()
    {
        baseScale = initialBaseScale;
        externalScaleMultiplier = 1f;
        ApplyScale();
        ClearTrails();
        ResetDeathState();
        SetSpriteRenderersEnabled(true);
        ResetControllerState();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleCollision(other);
    }

    void OnTriggerEnter(Collider other)
    {
        TryHandleCollision(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
            TryHandleCollision(collision.collider);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
            TryHandleCollision(collision.collider);
    }

    void TryHandleCollision(Component other)
    {
        if (other == null) return;

        if (!other.CompareTag("Obstacle"))
        {
            return;
        }

        TriggerGameOver();
    }

    void TriggerGameOver()
    {
        if (isDying)
            return;

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        isDying = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayGameOverEffects();
            GameManager.Instance.BeginGameOver();
        }

        Animator animatorToUse = GetDeathAnimator();
        activeDeathAnimator = animatorToUse;
        bool usingEffectAnimator = animatorToUse != null && animatorToUse == deathEffectAnimator;

        if (disableSpriteOnGameOver && usingEffectAnimator)
            SetSpriteRenderersEnabled(false);

        if (animatorToUse != null)
        {
            bool played = false;
            if (!string.IsNullOrEmpty(deathStateName))
            {
                int stateHash = Animator.StringToHash(deathStateName);
                if (animatorToUse.HasState(0, stateHash))
                {
                    animatorToUse.Play(stateHash, 0, 0f);
                    played = true;
                }
            }

            if (!played && !string.IsNullOrEmpty(deathTriggerName))
                animatorToUse.SetTrigger(deathTriggerName);
        }

        deathRoutine = StartCoroutine(DeathSequence());
    }

    void ApplyScale()
    {
        transform.localScale = baseScale * externalScaleMultiplier;
    }

    void ClearTrails()
    {
        var trails = GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            var trail = trails[i];
            if (trail != null)
                trail.Clear();
        }
    }

    void SetSpriteRenderersEnabled(bool enabled)
    {
        Transform root = spriteRoot != null ? spriteRoot : transform;
        Transform effectRoot = deathEffectAnimator != null ? deathEffectAnimator.transform : null;
        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
                continue;

            if (effectRoot != null && renderer.transform.IsChildOf(effectRoot))
                continue;

            renderer.enabled = enabled;
        }
    }

    public void SetExternalScaleMultiplier(float multiplier)
    {
        externalScaleMultiplier = Mathf.Max(0.01f, multiplier);
        ApplyScale();
    }

    void ResetControllerState()
    {
        var controller = GetComponent<PlayerController>();
        if (controller == null)
            controller = GetComponentInChildren<PlayerController>();

        if (controller != null)
            controller.ResetControllerState(transform.position, transform.rotation);
    }

    IEnumerator DeathSequence()
    {
        float waitTime = Mathf.Max(0f, deathAnimationSeconds);
        if (waitTime > 0f)
        {
            float elapsed = 0f;
            float logInterval = Mathf.Max(0.01f, deathAnimationLogInterval);
            while (elapsed < waitTime)
            {
                LogDeathAnimationProgress(elapsed, waitTime);
                float step = Mathf.Min(logInterval, waitTime - elapsed);
                yield return new WaitForSecondsRealtime(step);
                elapsed += step;
            }
        }

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
        else
            Time.timeScale = 0f; // fallback
    }

    void LogDeathAnimationProgress(float elapsed, float total)
    {
        var animator = activeDeathAnimator != null ? activeDeathAnimator : deathAnimator;
        if (animator == null)
        {
            Debug.Log($"Death anim tick: animator missing ({elapsed:F2}/{total:F2}s)");
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isDeathState = !string.IsNullOrEmpty(deathStateName) && stateInfo.IsName(deathStateName);
        Debug.Log($"Death anim tick: normalized={stateInfo.normalizedTime:F2} isDeathState={isDeathState} ({elapsed:F2}/{total:F2}s)");
    }

    void ResetDeathState()
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        isDying = false;

        ResetAnimator(deathAnimator);
        if (deathEffectAnimator != deathAnimator)
            ResetAnimator(deathEffectAnimator);
        activeDeathAnimator = null;
    }

    Animator GetDeathAnimator()
    {
        if (deathEffectAnimator != null)
        {
            if (deathEffectAnchor == null)
                deathEffectAnchor = transform;
            if (deathEffectAnchor != null)
                deathEffectAnimator.transform.SetPositionAndRotation(deathEffectAnchor.position, deathEffectAnchor.rotation);
            if (!deathEffectAnimator.gameObject.activeSelf)
                deathEffectAnimator.gameObject.SetActive(true);
            return deathEffectAnimator;
        }

        if (deathAnimator == null)
            deathAnimator = GetComponentInChildren<Animator>();

        return deathAnimator;
    }

    void ResetAnimator(Animator animator)
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(deathTriggerName))
            animator.ResetTrigger(deathTriggerName);
        animator.Rebind();
        animator.Update(0f);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheCamera();
    }

    void CacheCamera()
    {
        if (cachedCamera == null || cachedCamera.Equals(null))
            cachedCamera = Camera.main;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayer(this);
        }
    }
}

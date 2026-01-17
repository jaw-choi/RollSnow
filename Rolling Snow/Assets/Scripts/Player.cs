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
    [Header("Skin Effects")]
    [SerializeField] private SkinCatalog skinCatalog;
    [SerializeField] private SkinDeathEffect[] skinDeathEffects;
    [SerializeField] private AudioManager.Sfx defaultDeathSfx = AudioManager.Sfx.Dead;
    [SerializeField] private SkinDeathSfx[] skinDeathSfx;
    [Header("Powerups")]
    [SerializeField] private PlayerInvincibility invincibility;
    [Header("Boundary")]
    [SerializeField] private string boundaryTag = "Boundary";
    [SerializeField] private LayerMask boundaryLayers;
    [SerializeField] private string boundaryNameContains = "background obstacle";

    [System.Serializable]
    private class SkinDeathEffect
    {
        public string skinId;
        public Animator animator;
    }

    [System.Serializable]
    private class SkinDeathSfx
    {
        public string skinId;
        public AudioManager.Sfx sfx = AudioManager.Sfx.Dead;
    }

    Vector3 baseScale;
    Vector3 initialBaseScale;
    float externalScaleMultiplier = 1f;
    Camera cachedCamera;
    bool isDying = false;
    Coroutine deathRoutine;
    Animator activeDeathAnimator;
    Animator defaultDeathEffectAnimator;

    void Awake()
    {
        baseScale = transform.localScale;
        if (baseScale.x > maxScale)
            baseScale = Vector3.one * maxScale;

        initialBaseScale = baseScale;

        if (deathAnimator == null)
            deathAnimator = GetComponentInChildren<Animator>();

        CacheCamera();
        defaultDeathEffectAnimator = deathEffectAnimator;
        RefreshSkinDeathEffect();
        if (invincibility == null)
            invincibility = GetComponentInChildren<PlayerInvincibility>(true);
        if (invincibility == null)
            invincibility = gameObject.AddComponent<PlayerInvincibility>();
        if (invincibility != null)
            invincibility.SetSpriteRoot(spriteRoot);

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

        var controller = GetController();
        if (controller != null && controller.HasBoundaryClamp && (invincibility == null || !invincibility.IsInvincible))
            TriggerGameOver();
    }

    public void ResetPlayerData()
    {
        baseScale = initialBaseScale;
        externalScaleMultiplier = 1f;
        ApplyScale();
        ClearTrails();
        ResetDeathState();
        if (invincibility != null)
            invincibility.ResetState();
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

    void OnTriggerExit2D(Collider2D other)
    {
        HandleBoundaryExit(other);
    }

    void OnTriggerExit(Collider other)
    {
        HandleBoundaryExit(other);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision != null)
            HandleBoundaryExit(collision.collider);
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision != null)
            HandleBoundaryExit(collision.collider);
    }

    void TryHandleCollision(Component other)
    {
        if (other == null) return;

        if (IsBoundary(other))
        {
            HandleBoundaryCollision(other);
            return;
        }

        if (!other.CompareTag("Obstacle"))
        {
            return;
        }

        if (invincibility != null && invincibility.IsInvincible)
        {
            invincibility.HandleObstacleHit(other);
            return;
        }

        TriggerGameOver();
    }

    void HandleBoundaryCollision(Component other)
    {
        if (invincibility != null && invincibility.IsInvincible)
        {
            if (TryGetBoundaryInfo(other, out float boundaryX, out bool isLeftBoundary))
            {
                var controller = GetController();
                if (controller != null)
                    controller.ActivateBoundarySlide(boundaryX, isLeftBoundary);
            }
            return;
        }

        TriggerGameOver();
    }

    void HandleBoundaryExit(Component other)
    {
        if (other == null || !IsBoundary(other))
            return;

        if (!TryGetBoundaryInfo(other, out _, out bool isLeftBoundary))
            return;

        var controller = GetController();
        if (controller != null)
            controller.ClearBoundaryClamp(isLeftBoundary);
    }

    bool IsBoundary(Component other)
    {
        var obj = other.gameObject;
        if (boundaryLayers.value != 0 && (boundaryLayers.value & (1 << obj.layer)) != 0)
            return true;

        if (!string.IsNullOrEmpty(boundaryTag) && obj.CompareTag(boundaryTag))
            return true;

        if (!string.IsNullOrEmpty(boundaryNameContains))
        {
            string name = obj.name;
            if (!string.IsNullOrEmpty(name) && name.IndexOf(boundaryNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    bool TryGetBoundaryInfo(Component other, out float boundaryX, out bool isLeftBoundary)
    {
        boundaryX = 0f;
        isLeftBoundary = false;
        if (other == null)
            return false;

        float playerX = transform.position.x;
        var collider2D = other as Collider2D;
        if (collider2D != null)
        {
            var bounds = collider2D.bounds;
            isLeftBoundary = playerX > bounds.center.x;
            boundaryX = isLeftBoundary ? bounds.max.x : bounds.min.x;
            return true;
        }

        var collider3D = other as Collider;
        if (collider3D != null)
        {
            var bounds = collider3D.bounds;
            isLeftBoundary = playerX > bounds.center.x;
            boundaryX = isLeftBoundary ? bounds.max.x : bounds.min.x;
            return true;
        }

        boundaryX = other.transform.position.x;
        isLeftBoundary = playerX > boundaryX;
        return true;
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

    PlayerController GetController()
    {
        var controller = GetComponent<PlayerController>();
        if (controller == null)
            controller = GetComponentInChildren<PlayerController>();
        return controller;
    }

    void RefreshSkinDeathEffect()
    {
        if (skinCatalog == null || skinDeathEffects == null || skinDeathEffects.Length == 0)
        {
            deathEffectAnimator = defaultDeathEffectAnimator;
            return;
        }

        string equippedId = GetEquippedSkinId();
        if (string.IsNullOrEmpty(equippedId))
        {
            deathEffectAnimator = defaultDeathEffectAnimator;
            return;
        }

        Animator selected = null;
        for (int i = 0; i < skinDeathEffects.Length; i++)
        {
            var entry = skinDeathEffects[i];
            if (entry == null || entry.animator == null)
                continue;

            if (entry.skinId == equippedId)
            {
                selected = entry.animator;
                break;
            }
        }

        deathEffectAnimator = selected != null ? selected : defaultDeathEffectAnimator;
    }

    string GetEquippedSkinId()
    {
        if (skinCatalog == null)
            return string.Empty;

        string defaultId = skinCatalog.GetDefaultSkinId();
        string equippedId = SkinStorage.GetEquippedSkinId(defaultId, skinCatalog.equippedKey);
        if (!SkinStorage.IsUnlocked(equippedId, defaultId, skinCatalog.unlockPrefix))
            equippedId = defaultId;

        return equippedId;
    }

    public AudioManager.Sfx GetDeathSfx()
    {
        if (skinDeathSfx == null || skinDeathSfx.Length == 0)
            return defaultDeathSfx;

        string equippedId = GetEquippedSkinId();
        if (string.IsNullOrEmpty(equippedId))
            return defaultDeathSfx;

        for (int i = 0; i < skinDeathSfx.Length; i++)
        {
            var entry = skinDeathSfx[i];
            if (entry == null || string.IsNullOrEmpty(entry.skinId))
                continue;

            if (entry.skinId == equippedId)
                return entry.sfx;
        }

        return defaultDeathSfx;
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

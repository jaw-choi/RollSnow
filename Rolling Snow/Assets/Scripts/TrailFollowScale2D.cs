using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class TrailFollowScale2D : MonoBehaviour
{
    [Header("Target to follow (usually Player transform)")]
    [SerializeField] private Transform target;

    [Header("Width when target scale = 1")]
    [SerializeField] private float baseWidth = 0.2f;

    [Header("Use max(targetScale.x, targetScale.y)")]
    [SerializeField] private bool useMaxXY = true;

    [Header("Stop updating after scale stabilizes")]
    [SerializeField] private bool stopWhenStable = false;
    [SerializeField] private float stableEpsilon = 0.0005f;
    [SerializeField] private int stableFrames = 20;

    private TrailRenderer tr;
    private float lastScale;
    private int stableCount;

    private void Awake()
    {
        tr = GetComponent<TrailRenderer>();
        if (target == null) target = transform; // fallback
        lastScale = GetScaleFactor();
        ApplyWidth(lastScale);
    }

    private void Update()
    {
        float s = GetScaleFactor();

        // If you only want to update while scaling up, this is enough:
        ApplyWidth(s);

        if (!stopWhenStable) return;

        // Detect stabilization and stop updating
        if (Mathf.Abs(s - lastScale) <= stableEpsilon) stableCount++;
        else stableCount = 0;

        lastScale = s;

        if (stableCount >= stableFrames)
        {
            enabled = false; // stop updating once stable
        }
    }

    private float GetScaleFactor()
    {
        Vector3 ls = target.lossyScale; // world scale (safe if parent scales too)
        return useMaxXY ? Mathf.Max(ls.x, ls.y) : ls.x;
    }

    private void ApplyWidth(float scaleFactor)
    {
        tr.widthMultiplier = baseWidth * scaleFactor;
    }
}

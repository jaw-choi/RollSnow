using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class ConvertFigmaCourseToWorld
{
    private const float PixelsPerUnit = 100f;
    private const float TargetWorldZ = 0f;

    [MenuItem("Tools/Figma/Convert Selected UI Root To World Sprites (Relative)")]
    private static void ConvertSelected()
    {
        Transform uiRoot = Selection.activeTransform;
        if (uiRoot == null) return;

        RectTransform rootRT = uiRoot as RectTransform;
        if (rootRT == null) return;

        Canvas canvas = uiRoot.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Create world root
        GameObject worldRootObj = new GameObject(uiRoot.name + "_World");
        Transform worldRoot = worldRootObj.transform;

        // Place the world root where you want the course to be centered in the world
        worldRoot.position = new Vector3(0f, 0f, TargetWorldZ);

        // Canvas scale factor affects UI units; normalize it
        float scaleFactor = 1f;
        if (canvas.renderMode != RenderMode.WorldSpace)
            scaleFactor = canvas.scaleFactor;

        Image[] images = uiRoot.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null) continue;
            if (img.sprite == null) continue;

            RectTransform childRT = img.rectTransform;

            // Get bounds of child relative to root (root local space)
            Bounds rel = RectTransformUtility.CalculateRelativeRectTransformBounds(rootRT, childRT);

            // Convert UI local units -> pixels (normalize by canvas scale factor), then -> world units
            Vector3 centerPx = rel.center / scaleFactor;
            Vector3 worldPos = worldRoot.position + new Vector3(centerPx.x / PixelsPerUnit, centerPx.y / PixelsPerUnit, 0f);
            worldPos.z = TargetWorldZ;

            GameObject go = new GameObject(img.gameObject.name);
            go.transform.SetParent(worldRoot, false);
            go.transform.position = worldPos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = img.sprite;

            // Match size using child rect (pixels) vs sprite bounds (world units)
            Vector2 rectSizePx = (childRT.rect.size) / scaleFactor;
            Vector2 desiredSizeUnits = rectSizePx / PixelsPerUnit;

            Vector2 spriteSizeUnits = sr.sprite.bounds.size;
            Vector3 localScale = Vector3.one;
            if (spriteSizeUnits.x > 0f) localScale.x = desiredSizeUnits.x / spriteSizeUnits.x;
            if (spriteSizeUnits.y > 0f) localScale.y = desiredSizeUnits.y / spriteSizeUnits.y;

            go.transform.localScale = localScale;
        }
    }
}

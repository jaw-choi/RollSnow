// Assets/Editor/UnusedPngFinder.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UnusedPngFinder : EditorWindow
{
    private Vector2 _scroll;
    private List<string> _candidates = new List<string>();

    // 보호 폴더(여기는 기본적으로 "사용 중"으로 간주)
    // 필요에 맞게 추가/삭제하세요.
    private readonly string[] _keepFolderPrefixes =
    {
        "Assets/Resources/",
        "Assets/StreamingAssets/",
        // Addressables을 폴더 규칙으로 관리 중이면 여기에 추가하세요. 예:
        // "Assets/Addressables/",
    };

    private const string MoveTargetFolder = "Assets/_UnusedTextures";

    [MenuItem("Tools/Unused PNG Finder")]
    public static void Open()
    {
        GetWindow<UnusedPngFinder>("Unused PNG Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Unused PNG 후보를 씬 의존성 기준으로 스캔합니다.", EditorStyles.boldLabel);

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Scan (Build Settings 씬 기준)"))
        {
            Scan();
        }

        using (new EditorGUI.DisabledScope(_candidates.Count == 0))
        {
            if (GUILayout.Button($"Move Candidates -> {MoveTargetFolder}"))
            {
                MoveCandidates();
            }

            if (GUILayout.Button("Export List (.txt)"))
            {
                ExportList();
            }
        }

        EditorGUILayout.Space(10);
        GUILayout.Label($"Candidates: {_candidates.Count}", EditorStyles.helpBox);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var path in _candidates)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(path);
            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "주의: Resources.Load/Addressables(키 로드)/문자열 경로 로드는 참조가 안 잡혀서 오탐 가능성이 있습니다.\n" +
            "의심되면 해당 폴더를 Keep에 추가하거나, 후보를 먼저 이동 후 테스트하세요.",
            MessageType.Warning
        );
    }

    private void Scan()
    {
        _candidates.Clear();

        // 1) 루트: Build Settings에 포함된 씬
        var scenePaths = EditorBuildSettings.scenes
            .Where(s => s != null && s.enabled)
            .Select(s => s.path)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        // 씬이 없으면 오탐이 너무 커지니 여기서는 후보 0으로 처리
        if (scenePaths.Length == 0)
        {
            Debug.Log("Build Settings에 활성화된 씬이 없습니다. Scan을 중단합니다.");
            return;
        }

        // 2) 씬들의 모든 의존성 수집
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var scenePath in scenePaths)
        {
            var deps = AssetDatabase.GetDependencies(scenePath, true);
            foreach (var d in deps)
                used.Add(Normalize(d));
        }

        // 3) 보호 폴더는 무조건 used로 간주(후보에서 제외)
        foreach (var keep in _keepFolderPrefixes)
        {
            var keepNorm = Normalize(keep);
            // keep 폴더 아래의 모든 Texture2D를 used로 추가
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { keepNorm.TrimEnd('/') });
            foreach (var g in guids)
            {
                var p = Normalize(AssetDatabase.GUIDToAssetPath(g));
                if (!string.IsNullOrEmpty(p))
                    used.Add(p);
            }
        }

        // 4) 프로젝트 내 모든 Texture2D 중 PNG만 추출
        var allTexGuids = AssetDatabase.FindAssets("t:Texture2D");
        var allPngPaths = new List<string>(allTexGuids.Length);

        foreach (var guid in allTexGuids)
        {
            var path = Normalize(AssetDatabase.GUIDToAssetPath(guid));
            if (string.IsNullOrEmpty(path)) continue;

            // PNG만 대상으로 (대소문자 무시)
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

            // 유니티 패키지 캐시/라이브러리 제외(보통 Assets 밖은 안 잡히지만 안전)
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;

            allPngPaths.Add(path);
        }

        // 5) used에 없는 PNG를 후보로
        foreach (var png in allPngPaths)
        {
            if (IsInKeepFolder(png)) continue;
            if (!used.Contains(png))
                _candidates.Add(png);
        }

        _candidates = _candidates.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p).ToList();

        Debug.Log($"Scan complete. Candidates: {_candidates.Count}");
    }

    private bool IsInKeepFolder(string assetPath)
    {
        var norm = Normalize(assetPath);
        foreach (var keep in _keepFolderPrefixes)
        {
            var k = Normalize(keep);
            if (norm.StartsWith(k, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void MoveCandidates()
    {
        if (_candidates.Count == 0) return;

        if (!AssetDatabase.IsValidFolder(MoveTargetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "_UnusedTextures");
        }

        int moved = 0;
        foreach (var path in _candidates.ToList())
        {
            if (!File.Exists(path)) continue;

            var fileName = Path.GetFileName(path);
            var dest = $"{MoveTargetFolder}/{fileName}";

            // 이름 충돌 방지
            dest = GetNonConflictingPath(dest);

            var result = AssetDatabase.MoveAsset(path, dest);
            if (string.IsNullOrEmpty(result))
            {
                moved++;
            }
            else
            {
                Debug.LogWarning($"Move failed: {path} -> {dest} / {result}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Moved: {moved}");
    }

    private void ExportList()
    {
        var savePath = EditorUtility.SaveFilePanel("Export unused PNG list", Application.dataPath, "unused_png_list", "txt");
        if (string.IsNullOrEmpty(savePath)) return;

        File.WriteAllLines(savePath, _candidates);
        Debug.Log($"Exported: {savePath}");
    }

    private string GetNonConflictingPath(string dest)
    {
        if (!File.Exists(dest)) return dest;

        var dir = Path.GetDirectoryName(dest)?.Replace("\\", "/");
        var name = Path.GetFileNameWithoutExtension(dest);
        var ext = Path.GetExtension(dest);

        for (int i = 1; i < 10000; i++)
        {
            var candidate = $"{dir}/{name}_{i}{ext}";
            if (!File.Exists(candidate))
                return candidate;
        }
        return dest;
    }

    private string Normalize(string path)
    {
        return (path ?? "").Replace("\\", "/");
    }
}

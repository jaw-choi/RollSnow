using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HeartStatusUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI heartCountLabel;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private GameObject timerRoot;

    [Header("Hearts")]
    [SerializeField] private Transform heartsRoot;
    [SerializeField] private GameObject fullHeartPrefab;
    [SerializeField] private GameObject emptyHeartPrefab;

    [Header("Behavior")]
    [SerializeField] private bool hideTimerWhenFull = true;
    [SerializeField] private string fullText = "FULL";
    [SerializeField] private float refreshIntervalSeconds = 1f;

    Coroutine refreshRoutine;
    readonly List<GameObject> heartInstances = new List<GameObject>();
    readonly List<bool> heartStates = new List<bool>();
    int cachedSlots = -1;

    void OnEnable()
    {
        StartRefreshRoutine();
        RefreshNow();
    }

    void OnDisable()
    {
        StopRefreshRoutine();
    }

    void StartRefreshRoutine()
    {
        if (refreshRoutine != null)
            return;

        refreshRoutine = StartCoroutine(RefreshLoop());
    }

    void StopRefreshRoutine()
    {
        if (refreshRoutine == null)
            return;

        StopCoroutine(refreshRoutine);
        refreshRoutine = null;
    }

    IEnumerator RefreshLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.2f, refreshIntervalSeconds));
        while (true)
        {
            RefreshNow();
            yield return wait;
        }
    }

    void RefreshNow()
    {
        var system = HeartSystem.GetOrCreate();
        if (system == null)
            return;

        var status = system.GetStatus();
        if (heartCountLabel != null)
            heartCountLabel.text = status.Current.ToString();

        UpdateHeartIcons(status);

        if (timerLabel == null)
            return;

        if (status.IsFull)
        {
            if (hideTimerWhenFull)
            {
                if (timerRoot != null)
                    timerRoot.SetActive(false);
            }
            else
            {
                if (timerRoot != null)
                    timerRoot.SetActive(true);
                timerLabel.text = fullText;
            }

            return;
        }

        if (timerRoot != null)
            timerRoot.SetActive(true);

        timerLabel.text = FormatTimer(status.SecondsToNext);
    }

    void UpdateHeartIcons(HeartSystem.HeartStatus status)
    {
        if (heartsRoot == null || fullHeartPrefab == null || emptyHeartPrefab == null)
            return;

        int slots = Mathf.Max(1, status.Max);
        if (slots != cachedSlots || heartInstances.Count != slots)
        {
            RebuildSlots(slots);
        }

        for (int i = 0; i < slots; i++)
        {
            bool shouldBeFull = i < status.Current;
            if (i < heartStates.Count && heartStates[i] == shouldBeFull && heartInstances[i] != null)
                continue;

            if (i < heartInstances.Count && heartInstances[i] != null)
                Destroy(heartInstances[i]);

            var prefab = shouldBeFull ? fullHeartPrefab : emptyHeartPrefab;
            var instance = Instantiate(prefab, heartsRoot);
            instance.transform.SetSiblingIndex(i);

            if (i < heartInstances.Count)
                heartInstances[i] = instance;
            else
                heartInstances.Add(instance);

            if (i < heartStates.Count)
                heartStates[i] = shouldBeFull;
            else
                heartStates.Add(shouldBeFull);
        }
    }

    void RebuildSlots(int slots)
    {
        for (int i = 0; i < heartInstances.Count; i++)
        {
            if (heartInstances[i] != null)
                Destroy(heartInstances[i]);
        }

        heartInstances.Clear();
        heartStates.Clear();
        cachedSlots = slots;

        for (int i = 0; i < slots; i++)
        {
            var instance = Instantiate(emptyHeartPrefab, heartsRoot);
            heartInstances.Add(instance);
            heartStates.Add(false);
        }
    }

    static string FormatTimer(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        int minutes = seconds / 60;
        int secs = seconds % 60;
        return $"{minutes:00}:{secs:00}";
    }
}

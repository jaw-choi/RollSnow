using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;

public class BackendRank
{
    private static BackendRank _instance = null;

    public static BackendRank Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new BackendRank();
            }

            return _instance;
        }
    }

    public struct RankEntry
    {
        public int Rank;
        public string Nickname;
        public int Score;
        public string GamerInDate;
        public int Index;
        public string UserId;
    }

    private const string DefaultRankUuid = "019beecc-63cc-7bc4-84c8-bdb15f588a51";
    public string RankUuid { get; set; } = DefaultRankUuid;

    const int DefaultLimit = 50;

    List<RankEntry> cachedEntries = new List<RankEntry>();

    public IEnumerator RankInsert(int score)
    {
        if (!TryGetBackend(out FirebaseAuth auth, out FirebaseFirestore firestore))
        {
            Debug.LogWarning("Rank insert skipped: Firebase is not initialized.");
            yield break;
        }

        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("Rank insert failed: not logged in.");
            yield break;
        }

        yield return BackendGameData.Instance.EnsureUserDocument();

        string dateKey = GetDateKey();
        string nickname = BackendManager.Instance != null ? BackendManager.Instance.Nickname : string.Empty;

        var entryRef = GetEntriesCollection(firestore, dateKey).Document(user.UserId);
        var data = new Dictionary<string, object>
        {
            { "uid", user.UserId },
            { "nickname", nickname ?? string.Empty },
            { "score", score },
            { "dateKey", dateKey },
            { "updatedAt", FieldValue.ServerTimestamp }
        };

        var task = entryRef.SetAsync(data, SetOptions.MergeAll);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Rank update failed: " + task.Exception.GetBaseException().Message);
            yield break;
        }

        Debug.Log("Rank update success.");
        yield return BackendGameData.Instance.GameDataUpdate(score, null);

        if (RankingPanelUI.Instance != null && RankingPanelUI.Instance.IsVisible())
            RankingPanelUI.Instance.RefreshRankings();
    }

    public IEnumerator FetchRankList(Action<bool, List<RankEntry>> onComplete, int limit = DefaultLimit)
    {
        if (onComplete == null)
            yield break;

        var entries = new List<RankEntry>();
        if (!TryGetBackend(out FirebaseAuth _, out FirebaseFirestore firestore))
        {
            Debug.LogWarning("Rank list fetch skipped: Firebase is not initialized.");
            onComplete(false, entries);
            yield break;
        }

        var query = GetEntriesCollection(firestore, GetDateKey()).OrderByDescending("score").Limit(Mathf.Max(1, limit));
        var task = query.GetSnapshotAsync(Source.Server);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogWarning("Rank list server fetch failed. Fallback to default source: " + task.Exception.GetBaseException().Message);
            task = query.GetSnapshotAsync();
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.LogError("Rank list fetch failed: " + task.Exception.GetBaseException().Message);
                onComplete(false, entries);
                yield break;
            }
        }

        int index = 0;
        var snapshot = task.Result;
        if (snapshot != null)
        {
            foreach (var doc in snapshot.Documents)
            {
                index++;
                string nickname = ReadString(doc, "nickname");
                int score = ReadInt(doc, "score");
                string uidField = ReadString(doc, "uid");
                string uid = !string.IsNullOrEmpty(uidField) ? uidField : doc.Id;

                entries.Add(new RankEntry
                {
                    Rank = index,
                    Nickname = nickname,
                    Score = score,
                    GamerInDate = uid,
                    Index = index - 1,
                    UserId = uid
                });
            }
        }

        Debug.Log("Rank list fetch success. count=" + entries.Count);
        cachedEntries = entries;
        onComplete(true, entries);
    }

    public bool TryGetRankList(out List<RankEntry> entries)
    {
        entries = new List<RankEntry>(cachedEntries);
        return entries.Count > 0;
    }

    public bool TryGetMyRank(out RankEntry entry)
    {
        var auth = BackendManager.Instance != null && BackendManager.Instance.IsInitialized
            ? BackendManager.Instance.Auth
            : null;

        entry = FindMyEntry(cachedEntries,
            auth != null && auth.CurrentUser != null ? auth.CurrentUser.UserId : string.Empty,
            BackendManager.Instance != null ? BackendManager.Instance.Nickname : string.Empty);

        return entry.Rank > 0;
    }

    public RankEntry FindMyEntry(List<RankEntry> entries, string userId, string nickname)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!string.IsNullOrEmpty(userId) && entry.UserId == userId)
                    return entry;
                if (!string.IsNullOrEmpty(nickname) && entry.Nickname == nickname)
                    return entry;
            }
        }

        return new RankEntry
        {
            Rank = 0,
            Nickname = nickname ?? string.Empty,
            Score = 0,
            GamerInDate = userId ?? string.Empty,
            Index = -1,
            UserId = userId ?? string.Empty
        };
    }

    public string GetCachedYesterdayWinner()
    {
        return PlayerPrefs.GetString("Rank.YesterdayWinner", string.Empty);
    }

    public void SetCachedYesterdayWinner(string nickname)
    {
        PlayerPrefs.SetString("Rank.YesterdayWinner", nickname ?? string.Empty);
        PlayerPrefs.Save();
    }

    static int ReadInt(DocumentSnapshot doc, string key)
    {
        if (doc == null || !doc.ContainsField(key))
            return 0;

        try
        {
            object raw = doc.GetValue<object>(key);
            if (raw is long l)
                return (int)l;
            if (raw is int i)
                return i;
            if (raw is double d)
                return Mathf.RoundToInt((float)d);
            if (raw is string s && int.TryParse(s, out int value))
                return value;
        }
        catch { }

        return 0;
    }

    static string ReadString(DocumentSnapshot doc, string key)
    {
        if (doc == null || !doc.ContainsField(key))
            return string.Empty;

        try
        {
            return doc.GetValue<string>(key) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    static string GetDateKey()
    {
        DateTime kst = GetKoreaNow();
        return kst.ToString("yyyyMMdd");
    }

    static DateTime GetKoreaNow()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            return DateTime.UtcNow.AddHours(9);
        }
    }

    static bool TryGetBackend(out FirebaseAuth auth, out FirebaseFirestore firestore)
    {
        var manager = BackendManager.Instance;
        if (manager != null && manager.IsInitialized)
        {
            auth = manager.Auth;
            firestore = manager.Firestore;
            if (auth != null && firestore != null)
                return true;
        }

        auth = null;
        firestore = null;
        return false;
    }

    CollectionReference GetEntriesCollection(FirebaseFirestore firestore, string dateKey)
    {
        return firestore.Collection("leaderboards")
            .Document(RankUuid)
            .Collection("daily")
            .Document(dateKey)
            .Collection("entries");
    }
}

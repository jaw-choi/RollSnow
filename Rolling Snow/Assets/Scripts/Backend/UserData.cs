using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;

public class UserData
{
    public int level = 1;
    public int score = 1;
    public float atk = 3.5f;
    public string info = string.Empty;
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public List<string> equipment = new List<string>();

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        result.AppendLine($"level : {level}");
        result.AppendLine($"atk : {atk}");
        result.AppendLine($"info : {info}");

        result.AppendLine("inventory");
        foreach (var itemKey in inventory.Keys)
        {
            result.AppendLine($"| {itemKey} : {inventory[itemKey]}");
        }

        result.AppendLine("equipment");
        foreach (var equip in equipment)
        {
            result.AppendLine($"| {equip}");
        }

        return result.ToString();
    }
}

public class BackendGameData
{
    public const string TableName = "users";
    const int LookupRetryCount = 2;
    const float LookupRetryDelaySeconds = 0.4f;

    private static BackendGameData _instance = null;

    public static BackendGameData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new BackendGameData();
            }

            return _instance;
        }
    }

    public static UserData userData;

    private string userDocId = string.Empty;

    public string RowInDate => userDocId;

    static bool IsOfflineError(System.Exception ex)
    {
        if (ex == null)
            return false;

        string message = ex.GetBaseException().Message;
        return !string.IsNullOrEmpty(message) &&
               message.IndexOf("offline", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public IEnumerator EnsureUserDocument()
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("EnsureUserDocument failed: not logged in.");
            yield break;
        }

        userDocId = user.UserId;
        var firestore = FirebaseFirestore.DefaultInstance;
        var docRef = firestore.Collection(TableName).Document(userDocId);

        Firebase.Firestore.DocumentSnapshot snapshot = null;
        System.Exception lookupException = null;
        for (int attempt = 0; attempt <= LookupRetryCount; attempt++)
        {
            var getTask = docRef.GetSnapshotAsync();
            yield return new WaitUntil(() => getTask.IsCompleted);

            if (getTask.Exception == null)
            {
                snapshot = getTask.Result;
                lookupException = null;
                break;
            }

            lookupException = getTask.Exception;
            if (!IsOfflineError(lookupException) || attempt >= LookupRetryCount)
                break;

            Debug.LogWarning($"User data lookup offline. Retry {attempt + 1}/{LookupRetryCount}.");
            yield return new WaitForSeconds(LookupRetryDelaySeconds);
        }

        if (lookupException != null)
        {
            if (IsOfflineError(lookupException))
            {
                Debug.LogWarning("User data lookup still offline. Using local default data for now.");
                BuildDefaultUserData();
                yield break;
            }

            Debug.LogError("User data lookup failed: " + lookupException.GetBaseException().Message);
            yield break;
        }

        if (snapshot != null && snapshot.Exists)
        {
            LoadUserData(snapshot);
            yield break;
        }

        BuildDefaultUserData();
        var data = SerializeUserData();
        data["uid"] = user.UserId;
        data["createdAt"] = FieldValue.ServerTimestamp;

        string nickname = BackendManager.Instance != null ? BackendManager.Instance.Nickname : string.Empty;
        if (!string.IsNullOrEmpty(nickname))
        {
            data["nickname"] = nickname;
            data["nicknameLower"] = nickname.ToLowerInvariant();
        }

        var setTask = docRef.SetAsync(data);
        yield return new WaitUntil(() => setTask.IsCompleted);

        if (setTask.Exception != null)
        {
            Debug.LogError("User data insert failed: " + setTask.Exception.GetBaseException().Message);
            yield break;
        }

        Debug.Log("User data insert success.");
    }

    public IEnumerator GameDataUpdate()
    {
        if (userData == null)
        {
            Debug.LogError("No user data exists. Insert or Get data before update.");
            yield break;
        }

        var updates = SerializeUserData();
        yield return UpdateUserDocument(updates);
    }

    public IEnumerator GameDataUpdate(int? score = null, string nickname = null)
    {
        var updates = new Dictionary<string, object>();

        if (score.HasValue)
        {
            updates["score"] = score.Value;
            if (userData != null)
                userData.score = score.Value;
        }

        if (!string.IsNullOrEmpty(nickname))
        {
            updates["nickname"] = nickname;
            updates["nicknameLower"] = nickname.ToLowerInvariant();
        }

        if (updates.Count == 0)
            yield break;

        yield return UpdateUserDocument(updates);
    }

    IEnumerator UpdateUserDocument(Dictionary<string, object> updates)
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
        {
            Debug.LogError("User data update failed: not logged in.");
            yield break;
        }

        updates["updatedAt"] = FieldValue.ServerTimestamp;

        var firestore = FirebaseFirestore.DefaultInstance;
        var docRef = firestore.Collection(TableName).Document(user.UserId);

        var task = docRef.SetAsync(updates, SetOptions.MergeAll);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            if (IsOfflineError(task.Exception))
                Debug.LogWarning("User data update skipped: client offline.");
            else
                Debug.LogError("User data update failed: " + task.Exception.GetBaseException().Message);
            yield break;
        }

        Debug.Log("User data update success.");
    }

    void BuildDefaultUserData()
    {
        if (userData == null)
            userData = new UserData();

        Debug.Log("Initializing default game data.");
        userData.level = 1;
        userData.score = 0;
        userData.atk = 3.5f;
        userData.info = "New player data";

        userData.equipment.Clear();
        userData.inventory.Clear();
        userData.equipment.Add("Starter Sword");
        userData.equipment.Add("Starter Shield");
        userData.inventory.Add("Potion", 1);
        userData.inventory.Add("Elixir", 1);
    }

    Dictionary<string, object> SerializeUserData()
    {
        if (userData == null)
            BuildDefaultUserData();

        return new Dictionary<string, object>
        {
            { "level", userData.level },
            { "atk", userData.atk },
            { "info", userData.info },
            { "equipment", new List<string>(userData.equipment) },
            { "inventory", new Dictionary<string, int>(userData.inventory) },
            { "score", userData.score }
        };
    }

    void LoadUserData(DocumentSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists)
            return;

        if (userData == null)
            userData = new UserData();

        if (snapshot.ContainsField("level"))
            userData.level = ReadInt(snapshot, "level", userData.level);
        if (snapshot.ContainsField("score"))
            userData.score = ReadInt(snapshot, "score", userData.score);
        if (snapshot.ContainsField("atk"))
            userData.atk = ReadFloat(snapshot, "atk", userData.atk);
        if (snapshot.ContainsField("info"))
            userData.info = snapshot.GetValue<string>("info");
        if (snapshot.ContainsField("equipment"))
            userData.equipment = ReadStringList(snapshot, "equipment");
        if (snapshot.ContainsField("inventory"))
            userData.inventory = ReadIntDictionary(snapshot, "inventory");
    }

    static int ReadInt(DocumentSnapshot snapshot, string field, int fallback)
    {
        if (snapshot == null || !snapshot.ContainsField(field))
            return fallback;

        object raw = snapshot.GetValue<object>(field);
        if (raw is long l)
            return (int)l;
        if (raw is int i)
            return i;
        if (raw is double d)
            return Mathf.RoundToInt((float)d);
        if (raw is string s && int.TryParse(s, out int value))
            return value;

        return fallback;
    }

    static float ReadFloat(DocumentSnapshot snapshot, string field, float fallback)
    {
        if (snapshot == null || !snapshot.ContainsField(field))
            return fallback;

        object raw = snapshot.GetValue<object>(field);
        if (raw is double d)
            return (float)d;
        if (raw is float f)
            return f;
        if (raw is long l)
            return l;
        if (raw is int i)
            return i;
        if (raw is string s && float.TryParse(s, out float value))
            return value;

        return fallback;
    }

    static List<string> ReadStringList(DocumentSnapshot snapshot, string field)
    {
        var list = new List<string>();
        if (snapshot == null || !snapshot.ContainsField(field))
            return list;

        try
        {
            var raw = snapshot.GetValue<IList<object>>(field);
            if (raw != null)
            {
                for (int i = 0; i < raw.Count; i++)
                {
                    if (raw[i] != null)
                        list.Add(raw[i].ToString());
                }
            }
        }
        catch { }

        return list;
    }

    static Dictionary<string, int> ReadIntDictionary(DocumentSnapshot snapshot, string field)
    {
        var dict = new Dictionary<string, int>();
        if (snapshot == null || !snapshot.ContainsField(field))
            return dict;

        try
        {
            var raw = snapshot.GetValue<Dictionary<string, object>>(field);
            if (raw != null)
            {
                foreach (var kv in raw)
                {
                    int value = 0;
                    if (kv.Value is long l)
                        value = (int)l;
                    else if (kv.Value is int i)
                        value = i;
                    else if (kv.Value is double d)
                        value = Mathf.RoundToInt((float)d);
                    else if (kv.Value is string s && int.TryParse(s, out int parsed))
                        value = parsed;

                    dict[kv.Key] = value;
                }
            }
        }
        catch { }

        return dict;
    }
}

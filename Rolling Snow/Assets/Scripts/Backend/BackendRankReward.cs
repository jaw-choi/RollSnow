using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Functions;

public class BackendRankReward
{
    public const string DefaultFunctionName = "DailyRankReward";

    public struct RewardStatus
    {
        public bool IsSuccess;
        public bool IsClaimable;
        public bool IsClaimed;
        public int RewardGold;
        public string WinnerNickname;
        public string RewardDate;
        public string Message;
        public int RemainingSeconds;
    }

    public struct ClaimResult
    {
        public bool IsSuccess;
        public int RewardGold;
        public string WinnerNickname;
        public string RewardDate;
        public string Message;
    }

    public string FunctionName { get; set; } = DefaultFunctionName;
    public string FunctionKey { get; set; } = string.Empty;

    public IEnumerator TryGetStatus(Action<bool, RewardStatus> onComplete)
    {
        if (onComplete == null)
            yield break;

        RewardStatus status = new RewardStatus
        {
            IsSuccess = false,
            IsClaimable = false,
            IsClaimed = false,
            RewardGold = 0,
            WinnerNickname = string.Empty,
            RewardDate = string.Empty,
            Message = string.Empty,
            RemainingSeconds = 0
        };

        var functions = GetFunctions();
        if (functions == null)
        {
            status.Message = "FunctionsUnavailable";
            onComplete(false, status);
            yield break;
        }

        var data = new Dictionary<string, object>
        {
            { "action", "status" },
            { "leaderboardId", BackendRank.Instance.RankUuid },
            { "dateKey", GetDateKey() }
        };

        if (!string.IsNullOrEmpty(FunctionKey))
            data["functionSignatureKey"] = FunctionKey;

        var task = functions.GetHttpsCallable(FunctionName).CallAsync(data);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Rank reward status failed: " + task.Exception.GetBaseException().Message);
            status.Message = "FunctionError";
            onComplete(false, status);
            yield break;
        }

        var root = GetRootData(task.Result != null ? task.Result.Data : null);
        status.IsClaimable = ReadBool(root, "claimable", "isClaimable", "available");
        status.IsClaimed = ReadBool(root, "claimed", "isClaimed");
        status.RewardGold = ReadInt(root, "rewardGold", "gold");
        status.WinnerNickname = ReadString(root, "winnerNickname", "winner", "nickname");
        status.RewardDate = ReadString(root, "rewardDate", "date");
        status.RemainingSeconds = ReadInt(root, "remainingSeconds", "remaining", "timeRemaining");
        status.Message = ReadString(root, "message", "msg");

        if (status.RemainingSeconds <= 0)
        {
            string nextResetTimeStr = ReadString(root, "nextResetTime", "resetTime", "timeUntilReset");
            if (!string.IsNullOrEmpty(nextResetTimeStr))
                status.RemainingSeconds = CalculateRemainingSecondsFromTime(nextResetTimeStr);
        }

        status.IsSuccess = true;
        onComplete(true, status);
    }

    public IEnumerator TryClaim(Action<bool, ClaimResult> onComplete)
    {
        if (onComplete == null)
            yield break;

        ClaimResult result = new ClaimResult
        {
            IsSuccess = false,
            RewardGold = 0,
            WinnerNickname = string.Empty,
            RewardDate = string.Empty,
            Message = string.Empty
        };

        var functions = GetFunctions();
        if (functions == null)
        {
            result.Message = "FunctionsUnavailable";
            onComplete(false, result);
            yield break;
        }

        var data = new Dictionary<string, object>
        {
            { "action", "claim" },
            { "leaderboardId", BackendRank.Instance.RankUuid },
            { "dateKey", GetDateKey() }
        };

        if (!string.IsNullOrEmpty(FunctionKey))
            data["functionSignatureKey"] = FunctionKey;

        var task = functions.GetHttpsCallable(FunctionName).CallAsync(data);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Rank reward claim failed: " + task.Exception.GetBaseException().Message);
            result.Message = "FunctionError";
            onComplete(false, result);
            yield break;
        }

        var root = GetRootData(task.Result != null ? task.Result.Data : null);
        result.RewardGold = ReadInt(root, "rewardGold", "gold");
        result.WinnerNickname = ReadString(root, "winnerNickname", "winner", "nickname");
        result.RewardDate = ReadString(root, "rewardDate", "date");
        result.Message = ReadString(root, "message", "msg");
        result.IsSuccess = true;

        onComplete(true, result);
    }

    public int CalculateRemainingSecondsFromTime(string targetTimeStr)
    {
        try
        {
            if (string.IsNullOrEmpty(targetTimeStr))
                return 0;

            if (!DateTime.TryParse(targetTimeStr, out DateTime targetTime))
                return 0;

            DateTime now = DateTime.UtcNow;
            if (targetTime.Kind == DateTimeKind.Local)
                now = DateTime.Now;

            TimeSpan remaining = targetTime - now;
            return Mathf.Max(0, (int)remaining.TotalSeconds);
        }
        catch (Exception ex)
        {
            Debug.LogError("CalculateRemainingSecondsFromTime error: " + ex.Message);
            return 0;
        }
    }

    public int GetRemainingSecondsUntil3_47PM()
    {
        try
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime koreaNow = ToKoreaTime(utcNow);
            DateTime next3_47PM = koreaNow.Date.AddHours(15).AddMinutes(47);

            if (koreaNow >= next3_47PM)
                next3_47PM = next3_47PM.AddDays(1);

            TimeSpan remaining = next3_47PM - koreaNow;
            return Mathf.Max(0, (int)remaining.TotalSeconds);
        }
        catch (Exception ex)
        {
            Debug.LogError("GetRemainingSecondsUntil3_47PM error: " + ex.Message);
            return 0;
        }
    }

    static DateTime ToKoreaTime(DateTime utcNow)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        }
        catch
        {
            return utcNow.AddHours(9);
        }
    }

    static string GetDateKey()
    {
        DateTime kst = ToKoreaTime(DateTime.UtcNow);
        return kst.ToString("yyyyMMdd");
    }

    static FirebaseFunctions GetFunctions()
    {
        var manager = BackendManager.Instance;
        if (manager != null && manager.IsInitialized)
            return manager.Functions;

        return null;
    }

    static IDictionary<string, object> GetRootData(object data)
    {
        if (data is IDictionary<string, object> dict)
        {
            if (dict.TryGetValue("data", out object nested) && nested is IDictionary<string, object> nestedDict)
                return nestedDict;

            return dict;
        }

        return new Dictionary<string, object>();
    }

    static bool ReadBool(IDictionary<string, object> data, params string[] keys)
    {
        if (data == null || keys == null)
            return false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (TryReadBool(data, keys[i], out bool value))
                return value;
        }

        return false;
    }

    static bool TryReadBool(IDictionary<string, object> data, string key, out bool value)
    {
        value = false;
        if (data == null || string.IsNullOrEmpty(key))
            return false;

        if (!data.TryGetValue(key, out object raw) || raw == null)
            return false;

        if (raw is bool b)
        {
            value = b;
            return true;
        }

        if (bool.TryParse(raw.ToString(), out bool parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    static int ReadInt(IDictionary<string, object> data, params string[] keys)
    {
        if (data == null || keys == null)
            return 0;

        for (int i = 0; i < keys.Length; i++)
        {
            if (TryReadInt(data, keys[i], out int value))
                return value;
        }

        return 0;
    }

    static bool TryReadInt(IDictionary<string, object> data, string key, out int value)
    {
        value = 0;
        if (data == null || string.IsNullOrEmpty(key))
            return false;

        if (!data.TryGetValue(key, out object raw) || raw == null)
            return false;

        if (raw is long l)
        {
            value = (int)l;
            return true;
        }

        if (raw is int i)
        {
            value = i;
            return true;
        }

        if (raw is double d)
        {
            value = Mathf.RoundToInt((float)d);
            return true;
        }

        if (int.TryParse(raw.ToString(), out int parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    static string ReadString(IDictionary<string, object> data, params string[] keys)
    {
        if (data == null || keys == null)
            return string.Empty;

        for (int i = 0; i < keys.Length; i++)
        {
            string value = ReadString(data, keys[i]);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return string.Empty;
    }

    static string ReadString(IDictionary<string, object> data, string key)
    {
        if (data == null || string.IsNullOrEmpty(key))
            return string.Empty;

        if (!data.TryGetValue(key, out object raw) || raw == null)
            return string.Empty;

        return raw.ToString();
    }
}

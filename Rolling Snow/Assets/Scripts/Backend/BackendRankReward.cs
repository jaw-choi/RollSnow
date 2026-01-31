using UnityEngine;
using LitJson;
using System;

// Backend SDK namespace
using BackEnd;

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
    // Function signature key required by BackEnd cloud functions
    // Use BackendManager.FunctionSignatureKey or set this directly
    public string FunctionKey { get; set; } = string.Empty;

    public bool TryGetStatus(out RewardStatus status)
    {
        status = new RewardStatus
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

        var bro = InvokeFunction("status");
        if (bro == null)
        {
            Debug.LogError("Rank reward status failed: no response (bro == null)");
            status.Message = "NoResponse";
            return false;
        }

        if (!bro.IsSuccess())
        {
            Debug.LogError("Rank reward status failed: " + bro.GetMessage());
            try
            {
                var dbgJson = bro.GetReturnValuetoJSON();
                Debug.LogError("BRO Return JSON: " + (dbgJson != null ? dbgJson.ToJson() : "null"));
            }
            catch { }
            status.Message = bro.GetMessage();
            return false;
        }

        var data = GetRootObject(bro.GetReturnValuetoJSON());

        // 디버그: 서버로부터 받은 JSON 전체 출력 (문자열)
        try
        {
            var dbg = bro.GetReturnValuetoJSON();
            Debug.Log("Server response JSON: " + (dbg != null ? dbg.ToJson() : "null"));
        }
        catch
        {
            Debug.Log("Server response JSON: (could not stringify)");
        }
        
        status.IsClaimable = ReadBool(data, "claimable", "isClaimable", "available");
        status.IsClaimed = ReadBool(data, "claimed", "isClaimed");
        status.RewardGold = ReadInt(data, "rewardGold", "gold");
        status.WinnerNickname = ReadString(data, "winnerNickname", "winner", "nickname");
        status.RewardDate = ReadString(data, "rewardDate", "date");
        status.RemainingSeconds = ReadInt(data, "remainingSeconds", "remaining", "timeRemaining");

        // 디버그: 파싱된 RemainingSeconds 출력
        Debug.Log($"Parsed RemainingSeconds: {status.RemainingSeconds}");
        
        // If server didn't return remainingSeconds, try to calculate from nextResetTime
        if (status.RemainingSeconds <= 0)
        {
            string nextResetTimeStr = ReadString(data, "nextResetTime", "resetTime", "timeUntilReset");
            if (!string.IsNullOrEmpty(nextResetTimeStr))
            {
                status.RemainingSeconds = CalculateRemainingSecondsFromTime(nextResetTimeStr);
                Debug.Log($"Calculated RemainingSeconds from nextResetTime: {status.RemainingSeconds}");
            }
        }
        
        status.Message = ReadString(data, "message", "msg");
        status.IsSuccess = true;

        // 디버그: 전체 status 정보 로깅
        Debug.Log($"=== FULL REWARD STATUS ===");
        Debug.Log($"IsSuccess: {status.IsSuccess}");
        Debug.Log($"IsClaimable: {status.IsClaimable}");
        Debug.Log($"IsClaimed: {status.IsClaimed}");
        Debug.Log($"RewardGold: {status.RewardGold}");
        Debug.Log($"WinnerNickname: {status.WinnerNickname}");
        Debug.Log($"RewardDate: {status.RewardDate}");
        Debug.Log($"RemainingSeconds: {status.RemainingSeconds}");
        Debug.Log($"Message: {status.Message}");
        Debug.Log($"========================");

        return true;
    }

    // Try to get the server time and calculate remaining seconds until a target time
    public int CalculateRemainingSecondsFromTime(string targetTimeStr)
    {
        try
        {
            // Get current server time
            var serverTimeBro = Backend.Utils.GetServerTime();
            if (serverTimeBro == null || !serverTimeBro.IsSuccess())
            {
                Debug.LogWarning("Failed to get server time: " + (serverTimeBro != null ? serverTimeBro.GetMessage() : "null"));
                return 0;
            }

            var serverTimeJson = serverTimeBro.GetReturnValuetoJSON();
            string serverTimeStr = ReadString(serverTimeJson, "utcTime");
            
            if (string.IsNullOrEmpty(serverTimeStr) || string.IsNullOrEmpty(targetTimeStr))
            {
                Debug.LogWarning($"Invalid time strings: serverTime={serverTimeStr}, targetTime={targetTimeStr}");
                return 0;
            }

            DateTime serverTime = DateTime.Parse(serverTimeStr);
            DateTime targetTime = DateTime.Parse(targetTimeStr);

            TimeSpan remaining = targetTime - serverTime;
            int remainingSeconds = Mathf.Max(0, (int)remaining.TotalSeconds);

            Debug.Log($"Server time: {serverTime:u}, Target time: {targetTime:u}, Remaining: {remainingSeconds}s");
            return remainingSeconds;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CalculateRemainingSecondsFromTime error: " + ex.Message);
            return 0;
        }
    }

    // Calculate remaining seconds until 15:47 (3:47 PM) Korea time (UTC+9)
    public int GetRemainingSecondsUntil3_47PM()
    {
        try
        {
            // Get current server time (UTC)
            var serverTimeBro = Backend.Utils.GetServerTime();
            if (serverTimeBro == null || !serverTimeBro.IsSuccess())
            {
                Debug.LogWarning("Failed to get server time for 3:47 PM calculation");
                return 0;
            }

            var serverTimeJson = serverTimeBro.GetReturnValuetoJSON();
            string serverTimeStr = ReadString(serverTimeJson, "utcTime");
            
            if (string.IsNullOrEmpty(serverTimeStr))
            {
                Debug.LogWarning("Invalid server time string");
                return 0;
            }

            DateTime utcNow = DateTime.Parse(serverTimeStr);
            
            // Convert to Korea time (UTC+9)
            TimeZoneInfo koreaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            DateTime koreaNow = TimeZoneInfo.ConvertTime(utcNow, koreaTimeZone);
            
            // Calculate next 15:47 (3:47 PM) in Korea time
            DateTime next3_47PM = koreaNow.Date.AddHours(15).AddMinutes(47);
            
            // If current time is already past 15:47, set target to tomorrow's 15:47
            if (koreaNow >= next3_47PM)
            {
                next3_47PM = next3_47PM.AddDays(1);
            }

            TimeSpan remaining = next3_47PM - koreaNow;
            int remainingSeconds = Mathf.Max(0, (int)remaining.TotalSeconds);

            Debug.Log($"Korea time: {koreaNow:u}, Next 15:47: {next3_47PM:u}, Remaining: {remainingSeconds}s");
            return remainingSeconds;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("GetRemainingSecondsUntil3_47PM error: " + ex.Message);
            return 0;
        }
    }

    public bool TryClaim(out ClaimResult result)
    {
        result = new ClaimResult
        {
            IsSuccess = false,
            RewardGold = 0,
            WinnerNickname = string.Empty,
            RewardDate = string.Empty,
            Message = string.Empty
        };

        var bro = InvokeFunction("claim");
        if (bro == null)
        {
            Debug.LogError("Rank reward claim failed: no response (bro == null)");
            result.Message = "NoResponse";
            return false;
        }

        if (!bro.IsSuccess())
        {
            Debug.LogError("Rank reward claim failed: " + bro.GetMessage());
            try
            {
                var dbgJson = bro.GetReturnValuetoJSON();
                Debug.LogError("BRO Return JSON: " + (dbgJson != null ? dbgJson.ToJson() : "null"));
            }
            catch { }
            result.Message = bro.GetMessage();
            return false;
        }

        var data = GetRootObject(bro.GetReturnValuetoJSON());
        result.RewardGold = ReadInt(data, "rewardGold", "gold");
        result.WinnerNickname = ReadString(data, "winnerNickname", "winner", "nickname");
        result.RewardDate = ReadString(data, "rewardDate", "date");
        result.Message = ReadString(data, "message", "msg");
        result.IsSuccess = true;
        return true;
    }

    BackendReturnObject InvokeFunction(string action)
    {
        var param = new Param();
        param.Add("action", action);
        param.Add("rankUuid", BackendRank.Instance.RankUuid);
        
        // 디버그: FunctionKey 확인
        Debug.Log($"InvokeFunction - FunctionName: {FunctionName}, FunctionKey: {(string.IsNullOrEmpty(FunctionKey) ? "EMPTY" : FunctionKey)}");
        
        // Add signature key to param if available
        if (!string.IsNullOrEmpty(FunctionKey))
        {
            param.Add("functionSignatureKey", FunctionKey);
            Debug.Log($"Added functionSignatureKey to param: {FunctionKey}");
        }
        else
        {
            Debug.LogWarning("FunctionKey is empty! Cloud function call may fail.");
        }

        var bro = Backend.BFunc.InvokeFunction(FunctionName, param);
        return bro;
    }

    static JsonData GetRootObject(JsonData data)
    {
        if (data == null)
            return null;

        if (data.IsObject)
            return data;

        if (data.IsArray && data.Count > 0)
            return data[0];

        return data;
    }

    static bool ReadBool(JsonData data, params string[] keys)
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

    static bool TryReadBool(JsonData data, string key, out bool value)
    {
        value = false;
        if (data == null || string.IsNullOrEmpty(key))
            return false;

        try
        {
            var raw = data[key];
            if (raw == null)
                return false;

            if (raw.IsBoolean)
            {
                value = (bool)raw;
                return true;
            }

            if (bool.TryParse(raw.ToString(), out bool parsed))
            {
                value = parsed;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    static int ReadInt(JsonData data, params string[] keys)
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

    static bool TryReadInt(JsonData data, string key, out int value)
    {
        value = 0;
        if (data == null || string.IsNullOrEmpty(key))
            return false;

        try
        {
            var raw = data[key];
            if (raw == null)
                return false;

            return int.TryParse(raw.ToString(), out value);
        }
        catch
        {
            return false;
        }
    }

    static string ReadString(JsonData data, params string[] keys)
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

    static string ReadString(JsonData data, string key)
    {
        if (data == null || string.IsNullOrEmpty(key))
            return string.Empty;

        try
        {
            var raw = data[key];
            return raw != null ? raw.ToString() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

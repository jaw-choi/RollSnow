using UnityEngine;
using LitJson;

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
            Message = string.Empty
        };

        var bro = InvokeFunction("status");
        if (bro == null || !bro.IsSuccess())
        {
            Debug.LogError("Rank reward status failed: " + bro);
            status.Message = bro != null ? bro.GetMessage() : "NoResponse";
            return false;
        }

        var data = GetRootObject(bro.GetReturnValuetoJSON());
        status.IsClaimable = ReadBool(data, "claimable", "isClaimable", "available");
        status.IsClaimed = ReadBool(data, "claimed", "isClaimed");
        status.RewardGold = ReadInt(data, "rewardGold", "gold");
        status.WinnerNickname = ReadString(data, "winnerNickname", "winner", "nickname");
        status.RewardDate = ReadString(data, "rewardDate", "date");
        status.Message = ReadString(data, "message", "msg");
        status.IsSuccess = true;
        return true;
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
        if (bro == null || !bro.IsSuccess())
        {
            Debug.LogError("Rank reward claim failed: " + bro);
            result.Message = bro != null ? bro.GetMessage() : "NoResponse";
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
        return Backend.BFunc.InvokeFunction(FunctionName, param);
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

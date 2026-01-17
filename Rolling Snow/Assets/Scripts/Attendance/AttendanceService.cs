using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class AttendanceService
{
    public const string AttendanceKey = "ATTENDANCE_DATA";
    public const string OwnedSkinsKey = "OWNED_SKINS";
    public const string DefaultDay7SkinId = "blacksesame";
    public const string DefaultSkinUnlockPrefix = "Skin.Unlocked.";

    [Serializable]
    public class AttendanceData
    {
        public string lastClaimDate;
        public int streak;
        public int cycleIndex;
    }

    [Serializable]
    private class OwnedSkinsData
    {
        public string[] skins = new string[0];
    }

    public struct ClaimResult
    {
        public bool success;
        public int dayIndex;
        public int hearts;
        public int gold;
        public string skinId;
        public bool usedGoldInsteadOfSkin;
    }

    private AttendanceData data;
    private HashSet<string> ownedSkins;

    public string Day7SkinId { get; set; } = DefaultDay7SkinId;
    public string SkinUnlockPrefix { get; set; } = DefaultSkinUnlockPrefix;

    public AttendanceService(string day7SkinId = null)
    {
        if (!string.IsNullOrEmpty(day7SkinId))
            Day7SkinId = day7SkinId;
        Load();
    }

    public AttendanceData Data => data;

    public void Load()
    {
        data = LoadData();
        ownedSkins = LoadOwnedSkins();
    }

    public void Save()
    {
        SaveData();
        SaveOwnedSkins();
    }

    public bool HasClaimedToday()
    {
        string today = GetTodayKstString();
        return !string.IsNullOrEmpty(data.lastClaimDate) && data.lastClaimDate == today;
    }

    public bool IsClaimableToday()
    {
        return !HasClaimedToday();
    }

    public int GetCurrentCycleIndex()
    {
        return Mathf.Clamp(data.cycleIndex, 0, 6);
    }

    public int GetPreviousClaimIndex()
    {
        return (GetCurrentCycleIndex() + 6) % 7;
    }

    public int GetClaimedCountInCycle()
    {
        int claimed = GetCurrentCycleIndex();
        if (HasClaimedToday() && claimed == 0)
            claimed = 7;
        return Mathf.Clamp(claimed, 0, 7);
    }

    public string GetRewardDisplayForDay(int dayIndex)
    {
        switch (dayIndex)
        {
            case 0:
                return "Hearts x3";
            case 1:
                return "Gold x50";
            case 2:
                return "Hearts x1";
            case 3:
                return "Gold x100";
            case 4:
                return "Hearts x1";
            case 5:
                return "Gold x150";
            case 6:
                return "Hearts x3 + Skin";
            default:
                return string.Empty;
        }
    }

    public ClaimResult TryClaimToday()
    {
        var result = new ClaimResult
        {
            success = false,
            dayIndex = GetCurrentCycleIndex(),
            hearts = 0,
            gold = 0,
            skinId = string.Empty,
            usedGoldInsteadOfSkin = false
        };

        if (!IsClaimableToday())
            return result;

        string today = GetTodayKstString();
        bool isConsecutive = IsYesterday(data.lastClaimDate, today);

        if (!isConsecutive)
        {
            data.streak = 1;
            data.cycleIndex = 0;
        }
        else
        {
            data.streak = Mathf.Max(1, data.streak + 1);
        }

        int rewardIndex = GetCurrentCycleIndex();
        result.dayIndex = rewardIndex;

        switch (rewardIndex)
        {
            case 0:
                result.hearts = 3;
                break;
            case 1:
                result.gold = 50;
                break;
            case 2:
                result.hearts = 1;
                break;
            case 3:
                result.gold = 100;
                break;
            case 4:
                result.hearts = 1;
                break;
            case 5:
                result.gold = 150;
                break;
            case 6:
                result.hearts = 3;
                if (!string.IsNullOrEmpty(Day7SkinId))
                {
                    if (HasSkin(Day7SkinId))
                    {
                        result.gold = 200;
                        result.usedGoldInsteadOfSkin = true;
                    }
                    else
                    {
                        result.skinId = Day7SkinId;
                    }
                }
                break;
        }

        if (result.hearts > 0)
            AddHearts(result.hearts);
        if (result.gold > 0)
            AddGold(result.gold);
        if (!string.IsNullOrEmpty(result.skinId))
            UnlockSkin(result.skinId);

        data.lastClaimDate = today;
        data.cycleIndex = (data.cycleIndex + 1) % 7;
        SaveData();

        result.success = true;
        return result;
    }

    AttendanceData LoadData()
    {
        if (PlayerPrefs.HasKey(AttendanceKey))
        {
            string json = PlayerPrefs.GetString(AttendanceKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                var loaded = JsonUtility.FromJson<AttendanceData>(json);
                if (loaded != null)
                {
                    loaded.cycleIndex = Mathf.Clamp(loaded.cycleIndex, 0, 6);
                    loaded.streak = Mathf.Max(0, loaded.streak);
                    if (loaded.lastClaimDate == null)
                        loaded.lastClaimDate = string.Empty;
                    return loaded;
                }
            }
        }

        return new AttendanceData
        {
            lastClaimDate = string.Empty,
            streak = 0,
            cycleIndex = 0
        };
    }

    void SaveData()
    {
        if (data == null)
            return;

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(AttendanceKey, json);
        PlayerPrefs.Save();
    }

    HashSet<string> LoadOwnedSkins()
    {
        if (!PlayerPrefs.HasKey(OwnedSkinsKey))
            return new HashSet<string>();

        string json = PlayerPrefs.GetString(OwnedSkinsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return new HashSet<string>();

        var loaded = JsonUtility.FromJson<OwnedSkinsData>(json);
        if (loaded == null || loaded.skins == null)
            return new HashSet<string>();

        return new HashSet<string>(loaded.skins);
    }

    void SaveOwnedSkins()
    {
        if (ownedSkins == null)
            return;

        var data = new OwnedSkinsData
        {
            skins = new List<string>(ownedSkins).ToArray()
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(OwnedSkinsKey, json);
        PlayerPrefs.Save();
    }

    public static string GetTodayKstString()
    {
        return GetKstNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static DateTime GetKstNow()
    {
        DateTime utc = DateTime.UtcNow;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }
        catch
        {
            return utc + TimeSpan.FromHours(9);
        }
    }

    static bool IsYesterday(string lastDateString, string todayString)
    {
        if (!TryParseDate(lastDateString, out DateTime lastDate))
            return false;
        if (!TryParseDate(todayString, out DateTime todayDate))
            return false;

        return lastDate.AddDays(1) == todayDate;
    }

    static bool TryParseDate(string dateString, out DateTime date)
    {
        return DateTime.TryParseExact(
            dateString,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    // Stub hooks for reward application.
    public virtual void AddGold(int amount)
    {
        var gold = GoldSystem.GetOrCreate();
        if (gold != null)
            gold.AddGold(amount);
    }

    public virtual void AddHearts(int amount)
    {
        var hearts = HeartSystem.GetOrCreate();
        if (hearts != null)
            hearts.GrantHearts(amount);
    }

    public virtual bool HasSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId))
            return false;

        if (SkinStorage.IsUnlocked(skinId, string.Empty, SkinUnlockPrefix))
            return true;

        if (ownedSkins == null)
            ownedSkins = LoadOwnedSkins();

        return ownedSkins.Contains(skinId);
    }

    public virtual void UnlockSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId))
            return;

        SkinStorage.Unlock(skinId, SkinUnlockPrefix);

        if (ownedSkins == null)
            ownedSkins = LoadOwnedSkins();

        if (ownedSkins.Add(skinId))
            SaveOwnedSkins();
    }
}

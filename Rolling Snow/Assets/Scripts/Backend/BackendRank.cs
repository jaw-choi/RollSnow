using System.Collections.Generic;
using UnityEngine;
using LitJson;

// Backend SDK namespace
using BackEnd;

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
    }

    private const string DefaultRankUuid = "019beecc-63cc-7bc4-84c8-bdb15f588a51";
    public string RankUuid { get; set; } = DefaultRankUuid;

    public bool RankInsert(int score)
    {
        string tableName = BackendGameData.TableName;
        if (!BackendGameData.Instance.EnsureRowInDate())
        {
            Debug.LogError("Rank insert failed: missing user data row.");
            return false;
        }

        string rowInDate = BackendGameData.Instance.RowInDate;
        if (string.IsNullOrEmpty(rowInDate))
        {
            Debug.LogError("Rank insert failed: rowInDate empty.");
            return false;
        }

        Param param = new Param();
        param.Add("score", score);

        Debug.Log("Requesting rank update.");
        var rankBro = Backend.URank.User.UpdateUserScore(RankUuid, tableName, rowInDate, param);

        if (!rankBro.IsSuccess())
        {
            Debug.LogError("Rank update failed: " + rankBro);
            return false;
        }

        Debug.Log("Rank update success: " + rankBro);
        BackendGameData.Instance.GameDataUpdate(score, null);
        return true;
    }

    public bool TryGetRankList(out List<RankEntry> entries)
    {
        entries = new List<RankEntry>();
        var bro = Backend.URank.User.GetRankList(RankUuid);

        if (!bro.IsSuccess())
        {
            Debug.LogError("Rank list fetch failed: " + bro);
            return false;
        }

        foreach (JsonData jsonData in bro.FlattenRows())
        {
            if (TryParseRankEntry(jsonData, out RankEntry entry))
                entries.Add(entry);
        }

        return true;
    }

    public bool TryGetMyRank(out RankEntry entry)
    {
        entry = new RankEntry();
        var bro = Backend.URank.User.GetMyRank(RankUuid);

        if (!bro.IsSuccess())
        {
            Debug.LogWarning("My rank fetch failed: " + bro);
            return false;
        }

        var rows = bro.FlattenRows();
        if (rows == null || rows.Count == 0)
            return false;

        return TryParseRankEntry(rows[0], out entry);
    }

    public bool TryGetTopRank(out RankEntry entry)
    {
        entry = new RankEntry();
        if (!TryGetRankList(out List<RankEntry> entries))
            return false;

        if (entries.Count == 0)
            return false;

        entry = entries[0];
        return true;
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

    private bool TryParseRankEntry(JsonData jsonData, out RankEntry entry)
    {
        entry = new RankEntry
        {
            Rank = ParseInt(jsonData, "rank"),
            Nickname = ParseString(jsonData, "nickname"),
            Score = ParseInt(jsonData, "score"),
            GamerInDate = ParseString(jsonData, "gamerInDate"),
            Index = ParseInt(jsonData, "index")
        };

        return entry.Rank > 0 || !string.IsNullOrEmpty(entry.Nickname);
    }

    private static int ParseInt(JsonData data, string key)
    {
        if (data == null || string.IsNullOrEmpty(key))
            return 0;

        try
        {
            int value;
            return int.TryParse(data[key].ToString(), out value) ? value : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string ParseString(JsonData data, string key)
    {
        if (data == null || string.IsNullOrEmpty(key))
            return string.Empty;

        try
        {
            var value = data[key];
            return value != null ? value.ToString() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

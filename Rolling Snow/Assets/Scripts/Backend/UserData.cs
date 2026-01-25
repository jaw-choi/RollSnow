using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Backend SDK namespace
using BackEnd;

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
    public const string TableName = "USER_DATA";

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

    private string gameDataRowInDate = string.Empty;

    public string RowInDate => gameDataRowInDate;

    public bool GameDataInsert()
    {
        if (userData == null)
        {
            userData = new UserData();
        }

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

        Param param = new Param();
        param.Add("level", userData.level);
        param.Add("atk", userData.atk);
        param.Add("info", userData.info);
        param.Add("equipment", userData.equipment);
        param.Add("inventory", userData.inventory);
        param.Add("score", userData.score);

        Debug.Log("Requesting user data insert.");
        var bro = Backend.GameData.Insert(TableName, param);

        if (bro.IsSuccess())
        {
            Debug.Log("User data insert success: " + bro);
            gameDataRowInDate = bro.GetInDate();
            return true;
        }

        Debug.LogError("User data insert failed: " + bro);
        return false;
    }

    public bool EnsureRowInDate()
    {
        if (!string.IsNullOrEmpty(gameDataRowInDate))
            return true;

        Debug.Log("Fetching user data row.");
        var bro = Backend.GameData.GetMyData(TableName, new Where());

        if (!bro.IsSuccess())
        {
            Debug.LogError("User data lookup failed: " + bro);
            return false;
        }

        if (bro.FlattenRows().Count > 0)
        {
            gameDataRowInDate = bro.FlattenRows()[0]["inDate"].ToString();
            return true;
        }

        Debug.Log("User data missing. Creating new row.");
        var insertBro = Backend.GameData.Insert(TableName);
        if (!insertBro.IsSuccess())
        {
            Debug.LogError("User data insert failed: " + insertBro);
            return false;
        }

        gameDataRowInDate = insertBro.GetInDate();
        return true;
    }

    public void GameDataUpdate()
    {
        if (userData == null)
        {
            Debug.LogError("No user data exists. Insert or Get data before update.");
            return;
        }

        Param param = new Param();
        param.Add("level", userData.level);
        param.Add("atk", userData.atk);
        param.Add("info", userData.info);
        param.Add("equipment", userData.equipment);
        param.Add("inventory", userData.inventory);
        param.Add("score", userData.score);

        BackendReturnObject bro = null;

        if (string.IsNullOrEmpty(gameDataRowInDate))
        {
            Debug.Log("Requesting latest user data update.");
            bro = Backend.GameData.Update(TableName, new Where(), param);
        }
        else
        {
            Debug.Log($"{gameDataRowInDate} user data update request.");
            bro = Backend.GameData.UpdateV2(TableName, gameDataRowInDate, Backend.UserInDate, param);
        }

        if (bro != null && bro.IsSuccess())
        {
            Debug.Log("User data update success: " + bro);
        }
        else
        {
            Debug.LogError("User data update failed: " + bro);
        }
    }

    public bool GameDataUpdate(int? score = null, string nickname = null)
    {
        if (!score.HasValue && string.IsNullOrEmpty(nickname))
            return false;

        Param param = new Param();
        if (score.HasValue)
        {
            param.Add("score", score.Value);
            if (userData != null)
                userData.score = score.Value;
        }

        if (!string.IsNullOrEmpty(nickname))
        {
            param.Add("nickname", nickname);
        }

        BackendReturnObject bro = null;
        if (string.IsNullOrEmpty(gameDataRowInDate))
        {
            Debug.Log("Requesting latest user data update.");
            bro = Backend.GameData.Update(TableName, new Where(), param);
        }
        else
        {
            Debug.Log($"{gameDataRowInDate} user data update request.");
            bro = Backend.GameData.UpdateV2(TableName, gameDataRowInDate, Backend.UserInDate, param);
        }

        if (bro != null && bro.IsSuccess())
        {
            Debug.Log("User data update success: " + bro);
            return true;
        }

        Debug.LogError("User data update failed: " + bro);
        return false;
    }
}

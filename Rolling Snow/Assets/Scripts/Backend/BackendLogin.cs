using System;
using UnityEngine;
using LitJson;

// Backend SDK namespace
using BackEnd;

public class BackendLogin
{
    private static BackendLogin _instance = null;

    public static BackendLogin Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new BackendLogin();
            }

            return _instance;
        }
    }

    public BackendReturnObject CustomSignUp(string id, string pw)
    {
        Debug.Log("Requesting custom sign up.");

        var bro = Backend.BMember.CustomSignUp(id, pw);

        if (bro.IsSuccess())
        {
            Debug.Log("Custom sign up success: " + bro);
        }
        else
        {
            Debug.LogError("Custom sign up failed: " + bro);
        }

        return bro;
    }

    public BackendReturnObject CustomLogin(string id, string pw)
    {
        Debug.Log("Requesting custom login.");

        var bro = Backend.BMember.CustomLogin(id, pw);

        if (bro.IsSuccess())
        {
            Debug.Log("Custom login success: " + bro);
        }
        else
        {
            Debug.LogError("Custom login failed: " + bro);
        }

        return bro;
    }

    public BackendReturnObject UpdateNickname(string nickname)
    {
        Debug.Log("Requesting nickname update.");

        var bro = Backend.BMember.UpdateNickname(nickname);

        if (bro.IsSuccess())
        {
            Debug.Log("Nickname update success: " + bro);
        }
        else
        {
            Debug.LogError("Nickname update failed: " + bro);
        }

        return bro;
    }

    public BackendReturnObject CheckNickname(string nickname)
    {
        Debug.Log("Checking nickname availability.");

        var bro = Backend.BMember.CheckNicknameDuplication(nickname);
        if (bro.IsSuccess())
        {
            Debug.Log("Nickname is available: " + bro);
        }
        else
        {
            Debug.LogWarning("Nickname is not available: " + bro);
        }

        return bro;
    }

    public bool TryGetNickname(out string nickname)
    {
        nickname = string.Empty;

        var bro = Backend.BMember.GetUserInfo();
        if (!bro.IsSuccess())
        {
            Debug.LogWarning("GetUserInfo failed: " + bro);
            return false;
        }

        JsonData json = bro.GetReturnValuetoJSON();
        if (json == null)
            return false;

        try
        {
            JsonData row = json["row"];
            if (row == null)
                return false;

            JsonData nickData = row["nickname"];
            if (nickData == null)
                return false;

            string raw = nickData.ToString();
            if (string.IsNullOrEmpty(raw) || raw == "null")
                return false;

            nickname = raw;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Nickname parse failed: " + ex.Message);
            return false;
        }
    }
}

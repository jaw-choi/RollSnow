using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;

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

    const string GuestEmailDomain = "guest.local";

    static bool IsOfflineError(System.Exception ex)
    {
        if (ex == null)
            return false;

        string message = ex.GetBaseException().Message;
        return !string.IsNullOrEmpty(message) &&
               message.IndexOf("offline", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public string CurrentUserId
    {
        get
        {
            var auth = FirebaseAuth.DefaultInstance;
            return auth != null && auth.CurrentUser != null ? auth.CurrentUser.UserId : string.Empty;
        }
    }

    public static string MakeEmail(string id)
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        return id + "@" + GuestEmailDomain;
    }

    public IEnumerator CustomSignUp(string id, string pw, Action<bool, string> onComplete)
    {
        if (onComplete == null)
            yield break;

        var auth = FirebaseAuth.DefaultInstance;
        if (auth == null)
        {
            onComplete(false, "AuthUnavailable");
            yield break;
        }

        string email = MakeEmail(id);
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            onComplete(false, "InvalidCredentials");
            yield break;
        }

        var task = auth.CreateUserWithEmailAndPasswordAsync(email, pw);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Custom sign up failed: " + task.Exception.GetBaseException().Message);
            onComplete(false, "SignUpFailed");
            yield break;
        }

        Debug.Log("Custom sign up success.");
        onComplete(true, string.Empty);
    }

    public IEnumerator CustomLogin(string id, string pw, Action<bool, string> onComplete)
    {
        if (onComplete == null)
            yield break;

        var auth = FirebaseAuth.DefaultInstance;
        if (auth == null)
        {
            onComplete(false, "AuthUnavailable");
            yield break;
        }

        string email = MakeEmail(id);
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            onComplete(false, "InvalidCredentials");
            yield break;
        }

        var task = auth.SignInWithEmailAndPasswordAsync(email, pw);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Custom login failed: " + task.Exception.GetBaseException().Message);
            onComplete(false, "LoginFailed");
            yield break;
        }

        Debug.Log("Custom login success.");
        onComplete(true, string.Empty);
    }

    public IEnumerator UpdateNickname(string nickname, Action<bool, string> onComplete)
    {
        if (onComplete == null)
            yield break;

        var auth = FirebaseAuth.DefaultInstance;
        var user = auth != null ? auth.CurrentUser : null;
        if (user == null)
        {
            onComplete(false, "NotLoggedIn");
            yield break;
        }

        if (string.IsNullOrEmpty(nickname))
        {
            onComplete(false, "InvalidNickname");
            yield break;
        }

        var firestore = FirebaseFirestore.DefaultInstance;
        var updates = new Dictionary<string, object>
        {
            { "nickname", nickname },
            { "nicknameLower", nickname.ToLowerInvariant() },
            { "updatedAt", FieldValue.ServerTimestamp }
        };

        var docRef = firestore.Collection(BackendGameData.TableName).Document(user.UserId);
        var task = docRef.SetAsync(updates, SetOptions.MergeAll);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Nickname update failed: " + task.Exception.GetBaseException().Message);
            onComplete(false, IsOfflineError(task.Exception) ? "Offline" : "UpdateFailed");
            yield break;
        }

        var profileTask = user.UpdateUserProfileAsync(new UserProfile { DisplayName = nickname });
        yield return new WaitUntil(() => profileTask.IsCompleted);

        Debug.Log("Nickname update success.");
        onComplete(true, string.Empty);
    }

    public IEnumerator CheckNickname(string nickname, Action<bool, string> onComplete)
    {
        if (onComplete == null)
            yield break;

        var auth = FirebaseAuth.DefaultInstance;
        var user = auth != null ? auth.CurrentUser : null;
        if (user == null)
        {
            onComplete(false, "NotLoggedIn");
            yield break;
        }

        string trimmed = string.IsNullOrEmpty(nickname) ? string.Empty : nickname.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            onComplete(false, "InvalidNickname");
            yield break;
        }

        var firestore = FirebaseFirestore.DefaultInstance;
        var query = firestore.Collection(BackendGameData.TableName)
            .WhereEqualTo("nicknameLower", trimmed.ToLowerInvariant())
            .Limit(1);

        var task = query.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogWarning("Nickname check failed: " + task.Exception.GetBaseException().Message);
            onComplete(false, IsOfflineError(task.Exception) ? "Offline" : "CheckFailed");
            yield break;
        }

        var snapshot = task.Result;
        if (snapshot == null || snapshot.Count == 0)
        {
            onComplete(true, string.Empty);
            yield break;
        }

        foreach (var doc in snapshot.Documents)
        {
            if (doc.Id != user.UserId)
            {
                onComplete(false, "DuplicateNickname");
                yield break;
            }
        }

        onComplete(true, string.Empty);
    }

    public IEnumerator TryGetNickname(Action<bool, string> onComplete)
    {
        if (onComplete == null)
            yield break;

        var auth = FirebaseAuth.DefaultInstance;
        var user = auth != null ? auth.CurrentUser : null;
        if (user == null)
        {
            onComplete(false, string.Empty);
            yield break;
        }

        var firestore = FirebaseFirestore.DefaultInstance;
        var docRef = firestore.Collection(BackendGameData.TableName).Document(user.UserId);

        var task = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogWarning("Get nickname failed: " + task.Exception.GetBaseException().Message);
            onComplete(false, string.Empty);
            yield break;
        }

        var snapshot = task.Result;
        if (snapshot != null && snapshot.Exists && snapshot.ContainsField("nickname"))
        {
            string nickname = snapshot.GetValue<string>("nickname");
            if (!string.IsNullOrEmpty(nickname))
            {
                onComplete(true, nickname);
                yield break;
            }
        }

        onComplete(false, string.Empty);
    }
}

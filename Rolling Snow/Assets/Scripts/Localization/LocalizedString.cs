using System;
using UnityEngine;

[Serializable]
public struct LocalizedString
{
    [TextArea] public string korean;
    [TextArea] public string english;

    public string Get(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.English:
                return string.IsNullOrEmpty(english) ? korean : english;
            default:
                return string.IsNullOrEmpty(korean) ? english : korean;
        }
    }

    public bool HasAny => !string.IsNullOrEmpty(korean) || !string.IsNullOrEmpty(english);
}

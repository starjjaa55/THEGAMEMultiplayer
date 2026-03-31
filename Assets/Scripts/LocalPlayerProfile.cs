using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class LocalPlayerProfile
{
    private const string PlayerNameKey = "PLAYER_NAME";
    public static string PlayerName { get; private set; }

    static LocalPlayerProfile()
    {
        var savedName = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        PlayerName = string.IsNullOrWhiteSpace(savedName) ? BuildDefaultName() : savedName;
    }

    public static void SetName(string newName)
    {
        var finalName = string.IsNullOrWhiteSpace(newName) ? BuildDefaultName() :
        newName.Trim();
        if (finalName.Length > 24)
        {
            finalName = finalName.Substring(0, 24);
        }
        PlayerName = finalName;
        PlayerPrefs.SetString(PlayerNameKey, PlayerName);
    }

    private static string BuildDefaultName()
    {
        return $"Player{Random.Range(1000, 9999)}";
    }
}

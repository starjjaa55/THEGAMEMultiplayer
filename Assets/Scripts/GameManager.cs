using UnityEngine;
using System.Collections.Generic;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<PlayerStats> players = new List<PlayerStats>();

    private PlayerStats currentLeader;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterPlayer(PlayerStats player)
    {
        players.Add(player);
    }

    public void UpdateLeader()
    {
        PlayerStats topPlayer = null;
        int maxKills = -1;

        foreach (var player in players)
        {
            if (player.killCount > maxKills)
            {
                maxKills = player.killCount;
                topPlayer = player;
            }
        }

        // เอามงกุฎออกจากคนเก่า
        if (currentLeader != null)
            currentLeader.SetCrown(false);

        // ใส่มงกุฎให้คนใหม่
        currentLeader = topPlayer;

        if (currentLeader != null)
            currentLeader.SetCrown(true);
    }

    internal void RegisterPlayer(PlayerSpawner playerSpawner)
    {
        throw new NotImplementedException();
    }
}
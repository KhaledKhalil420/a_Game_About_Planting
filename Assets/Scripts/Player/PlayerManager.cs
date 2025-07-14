using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;
    public static bool isPlayerDead;

    public List<GameObject> players;
    public GameObject localPlayer;
    internal float localPlayerMoney = 0;
    private float allTimeCollected;

    public GUI gui;

    public void LockPlayer()
    {
    }

    public void UnlockPlayer()
    {
    }
    
    public Transform GetClosestPlayer(Vector3 locateFrom)
    {
        GameObject closestPlayer = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(locateFrom, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        return closestPlayer.transform;
    }

    private void Awake()
    {
        instance = this;

        players = GameObject.FindGameObjectsWithTag("Player").ToList();
        players.ForEach(player => player.transform.parent = null);
        Debug.Log(GetClosestPlayer(transform.position).name);
    }
}

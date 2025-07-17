using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;
    public static bool isPlayerDead;

    public List<GameObject> players;
    public GameObject localPlayer;

    public void LockPlayer()
    {
        localPlayer.GetComponent<PlayerMovement>().playerLook.enabled = false;
        localPlayer.GetComponent<PlayerMovement>().enabled = false;
    }

    public void UnlockPlayer()
    {
        localPlayer.GetComponent<PlayerMovement>().playerLook.enabled = true;
        localPlayer.GetComponent<PlayerMovement>().enabled = true;
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
    }
}

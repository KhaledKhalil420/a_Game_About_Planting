using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static float Tickspeed = 1;

    private void UpdateTickSpeed(float newSpeed)
    {
        Tickspeed = newSpeed;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.L))
        {
            Tickspeed = 40;
        }
        else
        {
            Tickspeed = 1;
        }
    }
}

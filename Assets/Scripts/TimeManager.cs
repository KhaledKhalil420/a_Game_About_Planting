using System.Collections;
using UnityEngine;

public class Tick : MonoBehaviour
{
    public static float Tickspeed = 1;
    public static float DeltaTickSpeed => Time.deltaTime * Tickspeed;

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

using UnityEngine;

public class CameraBobbingIdle : MonoBehaviour
{
    public float bobSpeed = 1f;
    public float bobAmount = 0.05f;

    private Vector3 startPos;
    private float timer;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        timer += Time.deltaTime * bobSpeed;

        float x = Mathf.Sin(timer * 0.8f) * bobAmount;
        float y = Mathf.Sin(timer) * bobAmount;
        float z = Mathf.Cos(timer * 0.6f) * bobAmount;

        transform.localPosition = startPos + new Vector3(x, y, z);
    }
}

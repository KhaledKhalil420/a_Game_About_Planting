using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    public string uniqueID;
    public string prefabName;

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }
}

using UnityEngine;

[System.Serializable]
public class GameObjectSavable : MonoBehaviour
{
    internal GameObjectData saveData;
    public GameObject prefab;


    public GameObject ReturnPrefab()
    {
        return Resources.Load<GameObject>("Prefabs/" + saveData.prefabName);
    }

    private void Awake()
    {
        saveData = new();

        //Generate ID
        saveData.id = gameObject.GetInstanceID().ToString() + "@";

        //Set Prefab Name
        saveData.prefabName = prefab != null ? prefab.name.Replace("(Clone)", "") : "";

        //Set Object Name
        saveData.objectName = gameObject.name;
    }

    public void SaveObj()
    {
        saveData = new();

        //Generate ID
        saveData.id = gameObject.GetInstanceID().ToString() + "@";

        //Set Prefab Name
        saveData.prefabName = prefab != null ? prefab.name.Replace("(Clone)", "") : "";

        //Set Object Name
        saveData.objectName = gameObject.name;
     }
}


[System.Serializable]
public class GameObjectData
{
    public string id;
    public string objectName;
    public string prefabName;
    public Vector3 position, eulerAngles;
}


using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    internal string filePath;
    internal SaveFile file;

    private void Awake()
    {
        filePath = Application.persistentDataPath + "/world.json";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Save(); // Save game
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            Load(); // Load game
        }
    }

    public void Save()
    {
        //Make a new Save File
        file = new();

        SaveObject();
    }

    public void Load()
    {
        //Unload all old objects
        GameObjectData[] previousObjects = FindObjectsByType<GameObjectData>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        Array.ForEach(previousObjects, pre => Destroy(pre.gameObject));

        LoadObject();
    }

    #region GameObject Handling

    private void SaveObject()
    {
        foreach (var obj in FindObjectsByType<GameObjectData>(FindObjectsSortMode.InstanceID))
        {
            // Clone data into a serializable class
            var data = new GameObjectSaveData();
            data.id = obj.saveData.id;
            data.position = obj.transform.position;
            data.objectName = obj.saveData.objectName;
            data.prefabName = obj.saveData.prefabName;
            data.eulerAngles = obj.transform.eulerAngles;

            file.gameObjects.Add(data);
        }

        // Convert to JSON and write
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(filePath, json);
    }

    private void LoadObject()
    {
        string json = File.ReadAllText(filePath);
        SaveFile saveFile = JsonUtility.FromJson<SaveFile>(json);

        foreach (var data in saveFile.gameObjects)
        {
            // Get prefab
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + data.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning("Missing prefab: " + data.prefabName);
                continue;
            }

            // Spawn
            GameObjectData gameObject = Instantiate(prefab, data.position, Quaternion.Euler(data.eulerAngles)).GetComponent<GameObjectData>();
            gameObject.name = data.objectName;
            gameObject.saveData.id = data.id;
        }
    }

    #endregion
}
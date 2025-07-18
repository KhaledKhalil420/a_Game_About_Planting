using System;
using System.IO;
using System.Linq;
using Unity.Mathematics;
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
        SaveTree();
        SaveFarmland();

        // Convert to JSON and write
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(filePath, json);
    }

    public void Load()
    {
        //Unload all old objects
        GameObjectSavable[] previousObjects = FindObjectsByType<GameObjectSavable>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        Array.ForEach(previousObjects, pre => Destroy(pre.gameObject));

        LoadObject();
        LoadTree();
        LoadFarmland();
    }

    #region GameObject Handling

    private void SaveObject()
    {
        foreach (var obj in FindObjectsByType<GameObjectSavable>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
        {
            // Clone data into a serializable class
            var data = new GameObjectData();
            data.id = obj.saveData.id;
            data.position = obj.transform.position;
            data.objectName = obj.saveData.objectName;
            data.prefabName = obj.saveData.prefabName;
            data.eulerAngles = obj.transform.eulerAngles;

            file.gameObjects.Add(data);
        }
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
            GameObjectSavable gameObject = Instantiate(prefab, data.position, Quaternion.Euler(data.eulerAngles)).GetComponent<GameObjectSavable>();
            gameObject.name = data.objectName;
            gameObject.saveData.id = data.id;
        }
    }

    #endregion

    #region Breakables

    private void SaveTree()
    {
        foreach (var breakable in FindObjectsByType<Breakable>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID))
        {
            var breakableData = new BreakableData();

            breakableData.id = breakable.saveData.id;
            breakableData.health = breakable.breakableSaveData.health;

            file.breakables.Add(breakableData);
        }
    }

    private void LoadTree()
    {
        string json = File.ReadAllText(filePath);
        SaveFile saveFile = JsonUtility.FromJson<SaveFile>(json);

        foreach (var breakableData in saveFile.breakables)
        {
            var match = FindObjectsByType<Breakable>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.GetComponent<GameObjectSavable>().saveData.id == breakableData.id);

            if (match != null)
            {
                match.breakableSaveData.health = breakableData.health;
            }
        }
    }

    #endregion

    #region FarmlandSquare
    
    private void SaveFarmland()
    {

    }


    private void LoadFarmland()
    {

    }

    #endregion
}
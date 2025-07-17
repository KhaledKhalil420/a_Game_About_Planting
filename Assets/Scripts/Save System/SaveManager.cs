using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    public List<ISavable> savables = new();
    public static string savingPath;

    private void Awake()
    {
        //Set instance
        instance = this;

        //Set saving path
        savingPath = Application.persistentDataPath + "/save.json";

        #region Load Savables too list

        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Array.ForEach(objects, ob =>
        {
            if (ob.TryGetComponent(out ISavable savable))
                savables.Add(savable);
        });

        #endregion

        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F6)) LoadGame();
    }

    #region Values Saving

    public void SaveValue(GameObject gameObject, string value)
    {
        //Get id
        string id = "";
        if (gameObject.TryGetComponent(out SavableObject savableObject))
        {
            id = savableObject.objectID;
        }

        //Save value
        File.AppendAllText(savingPath, id + "$" + value.ToString() + "\n");
    }

    #endregion

    #region Value Loading

    public float LoadValueFloat(GameObject gameObject)
    {
        string id = "";
        if (gameObject.TryGetComponent(out SavableObject savableObject))
            id = savableObject.objectID;

        if (!File.Exists(savingPath)) return 0f;

        foreach (var line in File.ReadAllLines(savingPath))
        {
            if (line.StartsWith(id + "$"))
            {
                string valStr = line.Substring(id.Length + 1);
                if (float.TryParse(valStr, out float result)) return result;
            }
        }

        return 0f;
    }


    public int LoadValueInt(GameObject gameObject)
    {
        string id = "";
        if (gameObject.TryGetComponent(out SavableObject savableObject))
            id = savableObject.objectID;

        if (!File.Exists(savingPath)) return 0;

        foreach (var line in File.ReadAllLines(savingPath))
        {
            if (line.StartsWith(id + "$"))
            {
                string valStr = line.Substring(id.Length + 1);
                if (int.TryParse(valStr, out int result)) return result;
            }
        }

        return 0;
    }


    public bool LoadValueBool(GameObject gameObject)
    {
        string id = "";
        if (gameObject.TryGetComponent(out SavableObject savableObject))
            id = savableObject.objectID;

        if (!File.Exists(savingPath)) return false;

        foreach (var line in File.ReadAllLines(savingPath))
        {
            if (line.StartsWith(id + "$"))
            {
                string valStr = line.Substring(id.Length + 1);
                if (bool.TryParse(valStr, out bool result)) return result;
            }
        }

        return false;
    }


    public Vector3 LoadValueVector3(GameObject gameObject)
    {
        string id = "";
        if (gameObject.TryGetComponent(out SavableObject savableObject))
            id = savableObject.objectID;

        if (!File.Exists(savingPath)) return Vector3.zero;

        foreach (var line in File.ReadAllLines(savingPath))
        {
            if (line.StartsWith(id + "$"))
            {
                string valStr = line.Substring(id.Length + 1).Trim('(', ')');
                var parts = valStr.Split(',');

                if (parts.Length == 3
                    && float.TryParse(parts[0], out float x)
                    && float.TryParse(parts[1], out float y)
                    && float.TryParse(parts[2], out float z))
                {
                    return new Vector3(x, y, z);
                }
            }
        }

        return Vector3.zero;
    }


    public string LoadValue(GameObject gameObject)
    {
        string id = "";
        if (gameObject.TryGetComponent(out SavableObject savableObject))
            id = savableObject.objectID;

        if (!File.Exists(savingPath)) return "";

        foreach (var line in File.ReadAllLines(savingPath))
        {
            if (line.StartsWith(id + "$"))
            {
                return line.Substring(id.Length + 1);
            }
        }

        return "";
    }


    #endregion


    public void SaveGame()
    {
        foreach (ISavable savable in savables)
        {
            savable.Save();
        }
    }

    public void LoadGame()
    {
        foreach (ISavable savable in savables)
        {
            savable.Load();
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*
    How To Use Ez Save System!

        -Place all prefabs in Resources/Prefabs 
            --if Resources is null then make the file

        -Add the SavableObject component to the Object
            --Then add the name of the Prefab in the Savable Component

        -Add The [Savable] attribute to the value you want to save 
            --Note: Manager can only save, floats, bools, strings and ints

        -Optional: you can subscribe to the OnSave/OnLoad Events for more logic

        Made by the very cool Khaled Khalil
*/

public class SaveManager : MonoBehaviour
{
    [SerializeField] private bool autoSaveOnQuit = true;
    [SerializeField] private float autosaveTime = 50;
    public static SaveManager instance;

    internal string filePath;
    internal SaveFile file;

    public event Action OnSave;
    public event Action OnLoad;

    private void Awake()
    {
        instance = this;

        filePath = Application.persistentDataPath + "/save.json";
        Load();


        InvokeRepeating(nameof(Save), autosaveTime, autosaveTime);
    }

    public void Save()
    {
        //Create new save file
        file = new();
        SaveObjects();
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(filePath, json);

        OnSave?.Invoke();
    }

    public void Load()
    {
        string json = File.ReadAllText(filePath);
        file = JsonUtility.FromJson<SaveFile>(json);

        if (!File.Exists(filePath) || file.objects?.Count == 0 || file.objects == null) return;

        //Delete all old objects
        var oldObjects = FindObjectsByType<SaveableObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Select(x => x.gameObject).ToArray();
        foreach (var obj in oldObjects)
            Destroy(obj);

        //Load json file
        file = JsonUtility.FromJson<SaveFile>(json);
        if (file.objects == null) file.objects = new();

        LoadObjects();

        OnLoad?.Invoke();
    }

    #region SaveableObjects

    private void SaveObjects()
    {
        //Get all savables
        var all = FindObjectsByType<SaveableObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        foreach (var obj in all)
        {
            //Create data and set variables
            var data = new SaveableData();
            data.id = obj.uniqueID;
            data.prefab = obj.prefabName;
            data.position = obj.transform.position;
            data.rotation = obj.transform.eulerAngles;

            //Load all variables on all fields in component
            List<SaveableVariable> list = new List<SaveableVariable>();
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            //Save all [savable] variables on fields
            foreach (var f in fields)
            {
                if (f.GetCustomAttribute(typeof(Savable)) != null)
                {
                    object val = f.GetValue(obj);
                    string jsonVal = ToJson(val, f.FieldType);
                    list.Add(new SaveableVariable { name = f.Name, json = jsonVal });
                }
            }

            //Add all data to the list and save it
            data.data = list;
            file.objects.Add(data);

            //Trigger ISavable if available
            if (obj.TryGetComponent(out ISavable savable))
            {
                savable.OnSave();
            }
        }
    }

    private void LoadObjects()
    {
        foreach (var objData in file.objects)
        {
            //Get Prefab
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + objData.prefab);

            if (prefab == null)
                continue;

            //Spawn Prefab Copy and load data
            var obj = Instantiate(prefab, objData.position, Quaternion.Euler(objData.rotation));
            var saveable = obj.GetComponent<SaveableObject>();
            saveable.uniqueID = objData.id;

            //Load all fields on component
            FieldInfo[] fields = saveable.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            //Load each field by the same name on json file
            foreach (var f in fields)
            {
                if (f.GetCustomAttribute(typeof(Savable)) != null)
                {
                    var match = objData.data.FirstOrDefault(x => x.name == f.Name);
                    if (match != null)
                    {
                        object val = FromJson(match.json, f.FieldType);
                        f.SetValue(saveable, val);
                    }
                }
            }

            //Trigger ISavable if available
            if (obj.TryGetComponent(out ISavable savable))
            {
                savable.OnLoad();
            }
        }
    }

    #endregion

    #region Helpers

    private string ToJson(object val, Type type)
    {
        if (val == null) return "";

        if (type == typeof(int) || type == typeof(float) || type == typeof(bool) || type == typeof(string))
            return val.ToString();

        return JsonUtility.ToJson(val);
    }

    private object FromJson(string json, Type type)
    {
        if (string.IsNullOrEmpty(json)) return GetDefault(type);

        if (type == typeof(int))
            return int.Parse(json);
        if (type == typeof(float))
            return float.Parse(json);
        if (type == typeof(bool))
            return bool.Parse(json);
        if (type == typeof(string))
            return json;

        return JsonUtility.FromJson(json, type);
    }

    private object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    #endregion

    void OnApplicationQuit()
    {
        if(autoSaveOnQuit)
        Save();
    }
}
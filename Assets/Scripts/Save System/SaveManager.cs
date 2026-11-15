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
            --Note: Manager can save floats, bools, strings, ints, and serializable arrays/objects

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
        if (PlayerInventory.instance != null)
            PlayerInventory.instance.PrepareForSave();

        //Create new save file
        file = new();
        SaveObjects();
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(filePath, json);

        OnSave?.Invoke();
    }

    public void Load()
    {
        if (!File.Exists(filePath))
            return;

        string json = File.ReadAllText(filePath);
        file = JsonUtility.FromJson<SaveFile>(json);
        if (file == null || file.objects == null || file.objects.Count == 0)
            return;

        //Destroy all non-static saveable objects.. #justice for non statics ✊
        var oldObjects = FindObjectsByType<SaveableObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
            .Where(x => !x.IsSceneObject)
            .Select(x => x.gameObject)
            .ToArray();

        foreach (var obj in oldObjects)
            Destroy(obj);

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
            if (!obj.ShouldSave)
                continue;

            //Create data and set variables
            var data = new SaveableData();
            data.id = obj.uniqueID;
            data.prefab = obj.PrefabName;
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
            //Try find static existing object with this ID
            SaveableObject existing = FindObjectsByType<SaveableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(x => x.uniqueID == objData.id && x.IsSceneObject);

            GameObject obj;

            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/" + objData.prefab);
                if (prefab == null)
                {
                    Debug.LogWarning($"SaveManager: no prefab at Resources/Prefabs/{objData.prefab}, skipped object {objData.id}");
                    continue;
                }

                obj = Instantiate(prefab, objData.position, Quaternion.Euler(objData.rotation));
                existing = obj.GetComponent<SaveableObject>();
                existing.uniqueID = objData.id;
            }

            //Load fields
            FieldInfo[] fields = existing.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var f in fields)
            {
                if (f.GetCustomAttribute(typeof(Savable)) != null)
                {
                    var match = objData.data.FirstOrDefault(x => x.name == f.Name);
                    if (match != null)
                    {
                        object val = FromJson(match.json, f.FieldType);
                        f.SetValue(existing, val);
                    }
                }
            }

            if (obj.TryGetComponent(out ISavable savable))
                savable.OnLoad();
        }
    }

    #endregion

    #region Helpers

    private string ToJson(object val, Type type)
    {
        if (val == null) return "";

        if (type == typeof(int) || type == typeof(float) || type == typeof(bool) || type == typeof(string))
            return val.ToString();

        //Handle arrays by wrapping them in a serializable container
        if (type.IsArray)
        {
            //Create a wrapper object with the array
            var wrapperType = typeof(ArrayWrapper<>).MakeGenericType(type.GetElementType());
            var wrapper = System.Activator.CreateInstance(wrapperType);
            wrapperType.GetField("items").SetValue(wrapper, val);
            return JsonUtility.ToJson(wrapper);
        }

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

        //Handle arrays by unwrapping from serializable container //I have no idea chatGPT wrote this
        if (type.IsArray)
        {
            var wrapperType = typeof(ArrayWrapper<>).MakeGenericType(type.GetElementType());
            var wrapper = JsonUtility.FromJson(json, wrapperType);
            return wrapperType.GetField("items").GetValue(wrapper);
        }

        return JsonUtility.FromJson(json, type);
    }

    [System.Serializable]
    private class ArrayWrapper<T>
    {
        public T[] items;
    }

    private object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    #endregion

    void OnApplicationQuit()
    {
        if(autoSaveOnQuit)
            Save();
    }
}
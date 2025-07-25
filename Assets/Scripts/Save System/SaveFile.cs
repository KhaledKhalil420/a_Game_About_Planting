using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveFile
{
    public List<SaveableData> objects = new();
}

[System.Serializable]
public class SaveableData
{
    public string id;
    public string prefab;
    public Vector3 position;
    public Vector3 rotation;
    public List<SaveableVariable> data = new();
}

[System.Serializable]
public class SaveableVariable
{
    public string name;
    public string json;
}

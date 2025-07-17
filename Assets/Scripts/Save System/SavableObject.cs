using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SavableObject : MonoBehaviour, ISavable
{
    internal string objectID;
    private SaveManager saveManager;

    private void Start()
    {
        //Generate Unique savable ID
        string scene = SceneManager.GetActiveScene().name;
        string guid = Guid.NewGuid().ToString()[..8];
        objectID = $"{scene}_{gameObject.name}_{guid}";

        //Grab Save Manager instance
        saveManager = SaveManager.instance;
    }

    public void Save()
    {
        //Save Position
        saveManager.SaveValue(gameObject, transform.position.ToString());

        //Save Rotation
        saveManager.SaveValue(gameObject, transform.eulerAngles.ToString());

        //Save Scale
        saveManager.SaveValue(gameObject, transform.localScale.ToString());

        //Save Parent by ID of course
        saveManager.SaveValue(gameObject, transform.parent.ToString());
    }

    public void Load()
    {
        //Load Position
        transform.position = saveManager.LoadValueVector3(gameObject);

        //Load Rotation
        transform.eulerAngles = saveManager.LoadValueVector3(gameObject);

        //Load Scale
        transform.localScale = saveManager.LoadValueVector3(gameObject);

        //Load Parent
    }
}

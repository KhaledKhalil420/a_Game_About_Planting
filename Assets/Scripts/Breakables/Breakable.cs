using UnityEngine;

public class Breakable : GameObjectSavable
{
    internal BreakableData breakableSaveData;
    private bool isKilled = false;

    private void Awake()
    {
        saveData = new();
        breakableSaveData = new();
        
        //Generate ID
        saveData.id = gameObject.GetInstanceID().ToString() + "@";

        //Set Prefab Name
        saveData.prefabName = prefab != null ? prefab.name.Replace("(Clone)", "") : "";

        //Set Object Name
        saveData.objectName = gameObject.name;
    }

    public void Damage(int damage)
    {
        breakableSaveData.health -= damage;

        if (breakableSaveData.health <= 0 && !isKilled)
        {
            isKilled = true;
            OnDestroyed();
        }
    }

    public virtual void OnDestroyed()
    {

    }
}

[System.Serializable]
public class BreakableData : GameObjectData
{
    public int health = 100;
}
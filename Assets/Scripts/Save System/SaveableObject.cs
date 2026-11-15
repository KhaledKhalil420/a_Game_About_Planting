using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    public string uniqueID;
    public string prefabName;

    public bool isStatic = false;

    public virtual bool ShouldSave => true;

    // Falls back to the spawned object's name, so placed prefabs never save an empty name
    public virtual string PrefabName
    {
        get
        {
            if (!string.IsNullOrEmpty(prefabName))
                return prefabName;

            return name.EndsWith("(Clone)") ? name.Substring(0, name.Length - 7) : "";
        }
    }

    // Objects with no prefab can't be respawned, so they're found by their ID like static ones
    public bool IsSceneObject => isStatic || string.IsNullOrEmpty(PrefabName);

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }

#if UNITY_EDITOR
    // Gives objects in the scene a permanent ID, otherwise they get a new one every run
    private void OnValidate()
    {
        if (Application.isPlaying || UnityEditor.EditorUtility.IsPersistent(this)) return;
        if (!gameObject.scene.IsValid()) return;

        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.IsPartOfPrefabContents(gameObject)) return;

        if (!string.IsNullOrEmpty(uniqueID) && !HasDuplicateID()) return;

        uniqueID = System.Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
    }

    private bool HasDuplicateID()
    {
        foreach (var other in FindObjectsByType<SaveableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other != this && other.uniqueID == uniqueID)
                return true;
        }
        return false;
    }
#endif
}
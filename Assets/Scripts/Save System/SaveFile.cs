using System.Collections.Generic;

[System.Serializable]
public class SaveFile
{
    public List<GameObjectData> gameObjects = new();
    public List<BreakableData> breakables = new();
}

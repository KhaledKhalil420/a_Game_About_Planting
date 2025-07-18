using System.Collections.Generic;

[System.Serializable]
public class SaveFile
{
    public List<GameObjectSaveData> gameObjects = new();
    public List<BreakableData> breakables = new();
    public List<FarmlandSquareData> farmlands = new();
}

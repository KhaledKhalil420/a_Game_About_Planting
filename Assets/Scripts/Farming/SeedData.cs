using UnityEngine;

[CreateAssetMenu, System.Serializable]
public class SeedData : ScriptableObject
{
    public SeedGrowingStage[] seedGrowingStages;
    public Seed reward;
    public int sellingProfit;

    public float GetTotalTimeToGrow()
    {
        float totalTime = 0;

        for (int i = 0; i < seedGrowingStages.Length; i++)
        {
            totalTime += seedGrowingStages[i].growingDuration;
        }

        return totalTime;
    }

    public SeedGrowingStage GetLastSeed()
    {
        return seedGrowingStages[seedGrowingStages.Length - 1];
     }
}

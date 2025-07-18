using DG.Tweening;
using UnityEngine;

public partial class FarmlandSquare : GameObjectSavable, IInteractable
{
    [SerializeField] internal int soilQualityLevel = 4;
    [SerializeField] internal SeedData seedIn; //OMG HE SAID THE THING!!!
    [SerializeField] internal float waterLevel = 100;
    [SerializeField] private float growingElapsedTime = 0;
    [SerializeField] internal bool isReadyToHarvest = false;
    [SerializeField] private Transform childHolder;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip plantingSound, harvestingSound;
    [SerializeField] private Renderer renderr;
    [SerializeField] private Color drySoilColor, wateredSoilColor;
    private int _frameCounter = 0;
    private const int FRAME_INTERVAL = 100;

    private void Awake()
    {
        SaveObj();
    }

    private void Update()
    {
        UpdateSoilWater();

        float dt = Time.deltaTime;
        if (seedIn != null && !isReadyToHarvest && waterLevel > 0)
        {
            growingElapsedTime += dt * soilQualityLevel * Tick.Tickspeed;
            waterLevel -= dt / soilQualityLevel;
            if (growingElapsedTime >= seedIn.GetTotalTimeToGrow()) isReadyToHarvest = true;
        }

        _frameCounter++;
        if (_frameCounter >= FRAME_INTERVAL)
        {
            if (seedIn != null && !isReadyToHarvest && waterLevel > 0)
                UpdateGrowingStages();
            _frameCounter = 0;
        }
    }

    #region Planting and Taking seeds

    public void Interact(GameObject sender)
    {
        if (sender.TryGetComponent(out PlayerInventory inventory))
        {
            if (seedIn == null && inventory.GetHeldItem() is Seed seed)
            {
                Plant(inventory, seed);
                return;
            }
            else if (isReadyToHarvest)
            {
                Harvest(inventory);
            }
        }
    }

    private void Plant(PlayerInventory inventory, Seed seed)
    {
        inventory.GetSelectedSlot().HeldQuantity -= 1;
        inventory.GetSelectedSlot().UpdateSlot();
        seedIn = seed.seedData;

        childHolder.localScale = new(0.15f, 0.15f, 0.15f);
        DOVirtual.Vector3(childHolder.localScale, new Vector3(1, 1, 1), seed.seedData.GetTotalTimeToGrow(),
        value => childHolder.localScale = value);

        source.pitch = Random.Range(0.9f, 1.1f);
        source.PlayOneShot(plantingSound);
    }

    private void Harvest(PlayerInventory inventory)
    {
        Item reward = Instantiate(seedIn.reward);
        int getDoubleChance = Random.Range(1, 20);
        if (getDoubleChance == 2)
            reward.HeldQuantity *= 2;

        inventory.GiveItem(reward, out bool isGiven);
        if (!isGiven)
        {
            Destroy(reward.gameObject);
            return;
        }

        seedIn = null;
        isReadyToHarvest = false;
        growingElapsedTime = 0;

        if (_currentGrowingStageModel != null)
            Destroy(_currentGrowingStageModel.gameObject);

        source.pitch = Random.Range(0.9f, 1.1f);
        source.PlayOneShot(harvestingSound);
    }

    #endregion

    #region Seeds Growing

    private Transform _currentGrowingStageModel;
    private int _currentStageIndex = -1;

    private void UpdateGrowingStages()
    {
        float time = 0;
        for (int i = 0; i < seedIn.seedGrowingStages.Length; i++)
        {
            time += seedIn.seedGrowingStages[i].growingDuration;
            if (growingElapsedTime <= time)
            {
                if (_currentStageIndex == i) return;
                _currentStageIndex = i;
                if (_currentGrowingStageModel) Destroy(_currentGrowingStageModel.gameObject);
                var stage = seedIn.seedGrowingStages[i];
                _currentGrowingStageModel = Instantiate(stage.currentStageModel, transform.position, Quaternion.identity, childHolder).transform;
                _currentGrowingStageModel.localPosition = new Vector3(0, 0.15f, 0);
                _currentGrowingStageModel.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                DOVirtual.Vector3(_currentGrowingStageModel.localScale, new Vector3(1, 1, 1), 1, value => _currentGrowingStageModel.localScale = value);
                break;
            }
        }
    }

    #endregion

    #region Taking care of soil

    public void IncreaseWaterLevel(float multiplier)
    {
        if (waterLevel < 100)
            waterLevel += Time.deltaTime * multiplier;
    }

    private void UpdateSoilWater()
    {
        float t = Mathf.Clamp01(waterLevel / 100f);
        renderr.material.color = Color.Lerp(drySoilColor, wateredSoilColor, t);
    }

    public void FullyGrowSoil()
    {
        if (seedIn == null)
            return;

        DOTween.KillAll(this);

        //Grow soil
        isReadyToHarvest = true;
        growingElapsedTime = 1555;

        //Destory last model
        if (_currentGrowingStageModel != null)
            Destroy(_currentGrowingStageModel.gameObject);

        //Get new stage and spawn it
        _currentGrowingStageModel = Instantiate(seedIn.GetLastSeed().currentStageModel, transform.position, Quaternion.identity, childHolder).transform;
        _currentGrowingStageModel.localPosition = new Vector3(0, 0, 0);
        _currentGrowingStageModel.localScale = new Vector3(1, 1, 1);
        childHolder.localScale = new Vector3(1, 1, 1);

    }

    #endregion

    public SeedData GetSeedData(string seedName)
    {
        if (!seedName.Equals(""))
            return Resources.Load<SeedData>("Seeds/" + seedName);

        else return null;
     }

}
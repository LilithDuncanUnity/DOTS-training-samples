using Unity.Entities;

class CarConfigAuthoring : UnityEngine.MonoBehaviour
{
    public UnityEngine.GameObject CarPrefab;
    public float MinDefaultSpeed, MaxDefaultSpeed;
    public float MinOvertakeSpeedScale, MaxOvertakeSpeedScale;
    public float MinDistanceInFront, MaxDistanceInFront;
    public float MinMergeSpace, MaxMergeSpace;
    public float MinOvertakeEagerness, MaxOvertakeEagerness;
}

class ConfigBaker : Baker<CarConfigAuthoring>
{
    public override void Bake(CarConfigAuthoring authoring)
    {
        AddComponent(new CarConfig
        {
            CarPrefab = GetEntity(authoring.CarPrefab),
            MinDefaultSpeed = authoring.MinDefaultSpeed,
            MaxDefaultSpeed = authoring.MaxDefaultSpeed,
            MinOvertakeSpeedScale = authoring.MinOvertakeSpeedScale,
            MaxOvertakeSpeedScale = authoring.MaxOvertakeSpeedScale,
            MinDistanceInFront = authoring.MinDistanceInFront, 
            MaxDistanceInFront = authoring.MaxDistanceInFront,
            MinMergeSpace = authoring.MinMergeSpace, 
            MaxMergeSpace = authoring.MaxMergeSpace,
            MinOvertakeEagerness = authoring.MinOvertakeEagerness, 
            MaxOvertakeEagerness = authoring.MaxOvertakeEagerness
        });
    }
}

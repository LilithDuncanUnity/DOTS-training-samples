using Unity.Entities;


struct CarAICache : IComponentData
{
    public Entity CarInFront;
    public float CarInFrontSpeed;
    public float DistanceAhead;
    public float DistanceBehind;
}

struct CarAIMergeCache : IComponentData
{
    public bool CanMergeRight;
    public bool CanMergeLeft;
}
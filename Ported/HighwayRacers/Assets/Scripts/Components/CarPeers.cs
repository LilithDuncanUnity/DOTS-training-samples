using Unity.Entities;


struct CarAICache : IComponentData
{
    // the car that is in front of this car, or null if this is the only car in its lane
    public Entity CarInFront;
    public bool CanMergeRight;
    public bool CanMergeLeft;
    public float DistanceAhead;
    public float DistanceBehind;
}
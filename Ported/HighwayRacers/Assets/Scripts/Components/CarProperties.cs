using Unity.Entities;


struct CarProperties : IComponentData
{
    public float desiredSpeed;
    public float overTakePercent;
    public float minDistanceInFront;
    public float mergeSpace;
    public float overTakeEagerness;
}

using Unity.Entities;


struct CarProperties : IComponentData
{
    public float overTakePercent;
    public float leftMergeDistance;
    public float mergeSpace;
    public float overTakeEagerness;
}

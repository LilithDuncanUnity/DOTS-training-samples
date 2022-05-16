using Unity.Entities;

struct CarChangingLanes : IComponentData
{
    public int fromLane;
    public int toLane;    
}
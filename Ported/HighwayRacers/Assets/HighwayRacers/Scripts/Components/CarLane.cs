using Unity.Entities;


struct CarLane : IComponentData
{
    public int currentLane;  //Change to enum when we have the lanes available?
}
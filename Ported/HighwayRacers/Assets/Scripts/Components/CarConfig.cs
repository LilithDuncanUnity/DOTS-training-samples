using Unity.Entities;

struct CarConfig : IComponentData
{
    public Entity CarPrefab;
    public int CarCount;
}

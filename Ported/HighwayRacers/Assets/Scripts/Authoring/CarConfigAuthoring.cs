using Unity.Entities;

class CarConfigAuthoring : UnityEngine.MonoBehaviour
{
    public UnityEngine.GameObject CarPrefab;
}

class ConfigBaker : Baker<CarConfigAuthoring>
{
    public override void Bake(CarConfigAuthoring authoring)
    {
        AddComponent(new CarConfig
        {
            CarPrefab = GetEntity(authoring.CarPrefab),
        });
    }
}

using Unity.Entities;
using UnityEngine;

class CarAuthoring : UnityEngine.MonoBehaviour
{
    public Transform CarCameraPoint;
}

[TemporaryBakingType]
struct ChildrenWithRenderer : IBufferElementData
{
    public Entity Value;
}

class CarBaker : Baker<CarAuthoring>
{
    public override void Bake(CarAuthoring authoring)
    {        
        AddComponent<CarColor>();
        AddComponent<CarDirection>();
        AddComponent<CarPosition>();
        AddComponent<CarProperties>();
        AddComponent<CarSpeed>();
        AddComponent(new CarCameraPoint()
        {
            CameraPoint = GetEntity(authoring.CarCameraPoint),
        });
        var buffer = AddBuffer<ChildrenWithRenderer>().Reinterpret<Entity>();
        foreach (var renderer in GetComponentsInChildren<UnityEngine.MeshRenderer>())
        {
            buffer.Add(GetEntity(renderer));
        }
    }
}

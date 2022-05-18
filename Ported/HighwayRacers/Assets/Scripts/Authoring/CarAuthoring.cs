using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

class CarAuthoring : UnityEngine.MonoBehaviour
{
    public UnityEngine.Transform CarCameraPoint;
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
        AddComponent<CarPosition>(new CarPosition
        {
            currentLane = 0,
            distance = 0
        });
        AddComponent<CarProperties>();
        AddComponent<CarSpeed>(new CarSpeed
        {
            currentSpeed = 10.0f,
            desiredSpeed = 10.0f
        });
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

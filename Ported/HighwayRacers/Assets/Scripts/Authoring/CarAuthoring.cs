using Unity.Entities;
using UnityEngine;

class CarAuthoring : UnityEngine.MonoBehaviour
{
    public Transform CarCameraPoint;
}

class CarBaker : Baker<CarAuthoring>
{
    public override void Bake(CarAuthoring authoring)
    {        
        AddComponent<CarColor>();
        AddComponent<CarDirection>();
        AddComponent<CarLane>();
        AddComponent<CarProperties>();
        AddComponent<CarSpeed>();
        AddComponent(new CarCameraPoint()
        {
            CameraPoint = GetEntity(authoring.CarCameraPoint),
        });
    }
}
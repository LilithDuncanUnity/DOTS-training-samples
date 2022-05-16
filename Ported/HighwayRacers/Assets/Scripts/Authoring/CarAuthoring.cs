using Unity.Entities;

class CarAuthoring : UnityEngine.MonoBehaviour
{
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
    }
}
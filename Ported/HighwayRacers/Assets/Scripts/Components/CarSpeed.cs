using Unity.Entities;


struct CarSpeed : IComponentData
{
    public float desiredSpeed;
    public float currentSpeed;
}
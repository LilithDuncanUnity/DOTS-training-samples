using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial class CarMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities
            .WithAll<CarSpeed>()
            .ForEach((Entity entity, TransformAspect transform) =>
            {
                var pos = transform.Position;
                transform.Position = new float3(pos.x, pos.y, pos.z + 0.1f);              
            }).Run();
    }
}
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial class TankMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities
            .WithAll<CarSpeed>()
            .ForEach((Entity entity, TransformAspect transform) =>
            {
                var pos = transform.Position;
                pos.y = entity.Index;
                //var angle = (0.5f + noise.cnoise(pos / 10f)) * 4.0f * math.PI;
                var dir = float3.zero;
                //math.sincos(angle, out dir.x, out dir.z);
                transform.Position += /*dir * */dt * 5.0f;
                //transform.Rotation = quaternion.RotateY(angle);
            }).ScheduleParallel();
    }
}
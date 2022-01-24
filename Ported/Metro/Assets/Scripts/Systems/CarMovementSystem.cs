using Unity.Entities;
using Unity.Transforms;


public partial class CarMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var time = Time.ElapsedTime;

        Entities.WithAll<CarMovement>().ForEach((ref Translation translation, in CarMovement movement) =>
        {
            translation.Value.x = (float) ((time+movement.Offset) % 100) - 50f;
        }).ScheduleParallel();
    }
}

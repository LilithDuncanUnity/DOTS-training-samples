using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial class CarMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        TrackConfig track = SystemAPI.GetSingleton<TrackConfig>();

        Entities
            .WithAll<CarSpeed>()
            .ForEach((Entity entity, TransformAspect transform, CarSpeed carSpeed) =>
            {
//                carPosition.distance += dt * speed.currentSpeed;
//                TrackUtilities.GetCarPosition(track.highwaySize, carPosition.distance, )
                var pos = transform.Position;
                transform.Position = new float3(pos.x, pos.y, pos.z + 0.1f);

                float targetSpeed = carSpeed.defaultSpeed;
                //Get the Car in front
                //Get the distance to the car in front if it's not null

                //Check if we want to merge left?
                //Check if we want to change langes?
                //Check if we are overtaking?
                //Check if we want to merge right?

                //Check if in the process of merging

                //Prevent a crash with car in front
                //if (carInFront != null && dt > 0)
                //{
                //    float maxDistanceDiff = Mathf.Max(0, distToCarInFront - Highway.MIN_DIST_BETWEEN_CARS);
                //    velocityPosition = Mathf.Min(velocityPosition, maxDistanceDiff / dt);
                //}
            }).Run();
    }

    private void UpdateColor()
    {
        Entities
            .WithAll<CarColor>()
            .ForEach((ref CarColor carColor, in CarSpeed carSpeed) =>
            {
                //if (carVelocity > carSpeed.defaultSpeed)
                //{
                //  UpdateCar color to maxSpeedColor
                //}
                //else if (carSpeed.defaultSpeed < carVelocity)
                //{
                //  Update car color to minSpeedColor
                //}
                //else
                {
                    //Update Car color to carColor.default
                }
            }).ScheduleParallel();
    }
}

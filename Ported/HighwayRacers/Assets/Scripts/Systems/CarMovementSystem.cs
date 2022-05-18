using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial class CarMovementSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<TrackConfig>();

        base.OnCreate();        
    }
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        TrackConfig track = SystemAPI.GetSingleton<TrackConfig>();

        Dependency = Entities
            .ForEach((Entity entity, TransformAspect transform, ref CarPosition carPosition, in CarSpeed carSpeed) =>
            {
                carPosition.distance += dt * carSpeed.currentSpeed;

                TrackUtilities.GetCarPosition(track.highwaySize, carPosition.distance, carPosition.currentLane, out float posX, out float posZ, out float outRotation);
                var pos = transform.Position;
                transform.Position = new float3(posX, pos.y, posZ);
                transform.Rotation = quaternion.RotateY(outRotation);

                float targetSpeed = carSpeed.desiredSpeed;
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
            }).ScheduleParallel(Dependency);
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

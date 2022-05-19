using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial class CarMovementSystem : SystemBase
{
    private const float MIN_DIST_BETWEEN_CARS = 2f;

    protected override void OnCreate()
    {
        RequireForUpdate<TrackConfig>();

        base.OnCreate();        
    }
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        TrackConfig track = SystemAPI.GetSingleton<TrackConfig>();

        Entities
            .ForEach((Entity entity, TransformAspect transform, CarAspect carAspect, in CarChangingLanes carCC, in CarAICacheAspect car) =>
            {
                TrackUtilities.GetCarPosition(track.highwaySize, carAspect.Distance, carAspect.Lane,
                    out float posX, out float posZ, out float outRotation);

                if (carCC.FromLane != carCC.ToLane)
                {
                    float fromDistance = TrackUtilities.GetEquivalentDistance(track.highwaySize, carAspect.Distance, carCC.ToLane, carCC.FromLane);
                    TrackUtilities.GetCarPosition(track.highwaySize, fromDistance, carCC.FromLane, out float fromX, out float fromZ, out float fromRot);
                    posX = (1.0f - carCC.Progress) * fromX + carCC.Progress * posX;
                    posZ = (1.0f - carCC.Progress) * fromZ + carCC.Progress * posZ;
                    outRotation = (1.0f - carCC.Progress) * fromRot + carCC.Progress * outRotation;
                }

                var pos = transform.Position;
                transform.Position = new float3(posX, pos.y, posZ);
                transform.Rotation = quaternion.RotateY(outRotation);

                float targetSpeed = carAspect.DesiredSpeed;

                if (carAspect.CarInFront != null)
                {
                    if (car.DistanceAhead <carAspect.LeftMergeDistance)
                    {
                        var carSpeed = GetComponent<CarSpeed>(carAspect.CarInFront);
                        float carInFrontSpeed = carSpeed.currentSpeed;
                        targetSpeed = Mathf.Min(targetSpeed, carInFrontSpeed);
                    }
                }

                if (targetSpeed > carAspect.CurrentSpeed)
                {
                    carAspect.CurrentSpeed = Mathf.Min(targetSpeed, carAspect.CurrentSpeed + carAspect.Acceleration * dt);
                }
                else if (targetSpeed < carAspect.CurrentSpeed)
                {
                    carAspect.CurrentSpeed = Mathf.Max(targetSpeed, carAspect.CurrentSpeed - carAspect.Braking * dt);
                }

                //Prevent a crash with car in front
                if (carAspect.CarInFront != null && dt > 0)
                {                    
                    if (car.DistanceAhead < 0.1f)
                    {
                        Debug.Log("Oh no a crash");
                    }
                    float maxDistanceDiff = Mathf.Max(0, car.DistanceAhead - MIN_DIST_BETWEEN_CARS);
                    carAspect.CurrentSpeed = Mathf.Min(carAspect.CurrentSpeed, maxDistanceDiff / dt);
                }

                carAspect.Distance += dt * carAspect.CurrentSpeed;
            }).Run();
    }
}

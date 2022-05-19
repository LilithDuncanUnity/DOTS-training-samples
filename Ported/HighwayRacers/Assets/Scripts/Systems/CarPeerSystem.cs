using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[UpdateAfter(typeof(CarSpawningSystem))]
[BurstCompile]
partial struct CarPeerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TrackConfig>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public struct CarDistanceInfo
    {
        public Entity Car;
        public float Distance;
        public float WrappedDistance;
    }

    public struct CarDistanceList
    {
        public float LaneLength;
        public int CarCount;
        public NativeArray<CarDistanceInfo> CarDistances;
    }

    struct CarDistanceComparer : IComparer<CarDistanceInfo>
    {
        public int Compare(CarDistanceInfo a, CarDistanceInfo b)
        {
            if (a.Car == Entity.Null || b.Car == Entity.Null)
            {
                if (a.Car != b.Car)
                {
                    return a.Car != Entity.Null ? -1 : 1;
                }
                return 0;
            }
            return a.WrappedDistance.CompareTo(b.WrappedDistance);
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        TrackConfig track = SystemAPI.GetSingleton<TrackConfig>();

        //NativeArray<CarAspect> cars = CollectionHelper.CreateNativeArray<CarAspect>(track.numberOfCars, Allocator.Temp);
        NativeArray<CarDistanceList> carLanes = new NativeArray<CarDistanceList>(4, Allocator.Temp);
        for (int laneIndex = 0; laneIndex < 4; ++laneIndex)
        {
            carLanes[laneIndex] = new CarDistanceList
            {
                LaneLength = TrackUtilities.GetLaneLength(track.highwaySize, laneIndex),
                CarCount = 0,
                CarDistances = new NativeArray<CarDistanceInfo>(track.numberOfCars, Allocator.Temp)
            };
        }

        // Build list of car distances
        foreach (var car in SystemAPI.Query<CarPositionAspect>())
        {
            float distance = car.Distance;
            CarDistanceList laneInfo = carLanes[car.Lane];

            laneInfo.CarDistances[laneInfo.CarCount] = new CarDistanceInfo
            {
                Car = car.Entity,
                Distance = distance,
                WrappedDistance = TrackUtilities.WrapDistance(track.highwaySize, distance, car.Lane)
            };
            laneInfo.CarCount++;

            carLanes[car.Lane] = laneInfo;
        }

        // Sort each lane of car distances and cache the distance from the previous car and the distance to the next car
        for (int laneIndex = 0; laneIndex < 4; ++laneIndex)
        {
            CarDistanceList carLane = carLanes[laneIndex];

            carLane.CarDistances.Sort<CarDistanceInfo, CarDistanceComparer>(new CarDistanceComparer());

            for (int carIndex = 0; carIndex < carLane.CarCount; ++carIndex)
            {
                float distanceAhead = carLane.LaneLength;
                float distanceBehind = distanceAhead;
                Entity carAhead = Entity.Null;

                if (carIndex > 0)
                {
                    distanceBehind = TrackUtilities.WrapDistance(track.highwaySize, carLane.CarDistances[carIndex].Distance - carLane.CarDistances[carIndex - 1].Distance, laneIndex);
                }
                else if (carLane.CarCount > 1)
                {
                    distanceBehind = carLane.LaneLength - TrackUtilities.WrapDistance(track.highwaySize, carLane.CarDistances[carLane.CarCount - 1].Distance - carLane.CarDistances[carIndex].Distance, laneIndex);
                }

                if (carIndex < carLane.CarCount - 1)
                {
                    distanceAhead = TrackUtilities.WrapDistance(track.highwaySize, carLane.CarDistances[carIndex + 1].Distance - carLane.CarDistances[carIndex].Distance, laneIndex);
                    carAhead = carLane.CarDistances[carIndex + 1].Car;
                }
                else if (carLanes[laneIndex].CarCount > 1)
                {
                    distanceAhead = carLane.LaneLength - TrackUtilities.WrapDistance(track.highwaySize, carLane.CarDistances[carIndex].Distance - carLane.CarDistances[0].Distance, laneIndex);
                    carAhead = carLane.CarDistances[0].Car;
                }

                // Set the cached AI information to make later systems easier to write
                state.EntityManager.SetComponentData(carLane.CarDistances[carIndex].Car, new CarAICache
                {
                    CarInFront = carAhead,
                    CanMergeRight = false,
                    CanMergeLeft = false,
                    DistanceAhead = distanceAhead,
                    DistanceBehind = distanceBehind
                });
            }
        }

        // Sort each lane of car distances and cache the distance from the previous car and the distance to the next car
        for (int laneIndex = 0; laneIndex < 4; ++laneIndex)
        {
            foreach (var car in SystemAPI.Query<CarAICacheAspect>())
            {
                if (car.Lane == 0)
                    car.CanMergeRight = false;
                else
                {
                    CarDistanceList targetLane = carLanes[car.Lane - 1];
                    car.CanMergeRight = CanMergeToLane(in car, car.Lane - 1, ref targetLane, track.highwaySize);
                }


                if (car.Lane == 3)
                    car.CanMergeLeft = false;
                else
                {
                    CarDistanceList targetLane = carLanes[car.Lane + 1];
                    car.CanMergeLeft = CanMergeToLane(in car, car.Lane + 1, ref targetLane, track.highwaySize);
                }
            }
        }
    }

    bool CanMergeToLane(in CarAICacheAspect car, int targetLane, ref CarDistanceList targetLaneInfo, float lane0Length)
    {
        float distanceBack = TrackUtilities.GetEquivalentDistance(lane0Length, car.Distance - car.MergeSpace, car.Lane, targetLane);
        float distanceFront = TrackUtilities.GetEquivalentDistance(lane0Length, car.Distance + car.MinDistanceInFront, car.Lane, targetLane);

        distanceBack = TrackUtilities.WrapDistance(lane0Length, distanceBack, targetLane);
        distanceFront = TrackUtilities.WrapDistance(lane0Length, distanceFront, targetLane);

        if (distanceBack < distanceFront)
        {
            // This is silly as this list is sorted, so we should be able to leverage that better.
            foreach (var otherCar in targetLaneInfo.CarDistances)
            {
                if (otherCar.WrappedDistance > distanceBack && otherCar.WrappedDistance < distanceFront) {
                    return false;
                }
            }
        }
        else
        {
            foreach (var otherCar in targetLaneInfo.CarDistances)
            {
                if (otherCar.WrappedDistance < distanceFront || otherCar.WrappedDistance > distanceBack)
                {
                    return false;
                }
            }
        }
        return true;
    }
}

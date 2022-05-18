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
partial struct CarPeerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TrackConfig>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    struct CarComparer : IComparer<CarAspect>
    {
        public int Compare(CarAspect a, CarAspect b)
        {
            if (a.Lane != b.Lane) return b.Lane.CompareTo(a.Lane);
            else return b.Distance.CompareTo(a.Distance);
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        TrackConfig track = SystemAPI.GetSingleton<TrackConfig>();
        NativeArray<CarAspect> cars = CollectionHelper.CreateNativeArray<CarAspect>(track.numberOfCars, Allocator.Temp);
        int i = 0;

        foreach (var car in SystemAPI.Query<CarAspect>())
        {
            car.CarInFront = Entity.Null;
            cars[i] = car;
            i++;
        }

        // TODO: figure out how to sort only a subsection of the array
        if (i < cars.Length) return;

        cars.Sort<CarAspect, CarComparer>(new CarComparer());

        int lastLaneStart = 0;
        int lastLane = 0;
        for (int j = 0; j < i; j++)
        {
            var car = cars[j];
            if (lastLane != car.Lane)
            {
                lastLaneStart = j;
                lastLane = car.Lane;
            }

            // if we're at the end of the lane, go back to the start of the lane.
            if (j == i - 1 || cars[j + 1].Lane != car.Lane)
            {
                // only set if there was more than 1 in the lane
                if (j != lastLaneStart) car.CarInFront = cars[lastLaneStart].Entity;
                continue;
            }
            
            // otherwise, use the next car.
            else
            {
                car.CarInFront = cars[j + 1].Entity;
            }
        }


        // calculate can merge
        foreach (var car in cars)
        {
            if (car.Lane == 0) car.CanMergeRight = false;
            else car.CanMergeRight = CanMergeToLane(in car, car.Lane - 1, in cars, in track);


            if (car.Lane == 3) car.CanMergeLeft = false;
            else car.CanMergeLeft = CanMergeToLane(in car, car.Lane + 1, in cars, in track);
        }
    }

    bool CanMergeToLane(in CarAspect car, int lane, in NativeArray<CarAspect> cars, in TrackConfig track)
    {
        float distanceBack = TrackUtilities.GetEquivalentDistance(track.highwaySize, GetDistanceBack(in car, in track) - car.MergeSpace, car.Lane, lane);
        float distanceFront = TrackUtilities.GetEquivalentDistance(track.highwaySize, GetDistanceFront(in car, in track) - car.MergeSpace, car.Lane, lane);

        // TODO: optimize this from n2
        foreach (var other in cars)
        {
            if (car.Entity == other.Entity) continue;
            if (TrackUtilities.AreasOverlap(track.highwaySize, lane, distanceBack, distanceFront, other.Lane, GetDistanceBack(in other, in track),
                GetDistanceFront(in other, in track)))
            {
                return false;
            }
        }
        return true;
    }

    float GetDistanceBack(in CarAspect car, in TrackConfig track)
    {
        var laneLen = TrackUtilities.GetLaneLength(track.highwaySize, car.Lane);
        return (car.Distance - car.DistanceToBack)
            + Mathf.Floor((car.Distance - car.DistanceToBack) / laneLen) * laneLen;
    }

    float GetDistanceFront(in CarAspect car, in TrackConfig track)
    {
        var laneLen = TrackUtilities.GetLaneLength(track.highwaySize, car.Lane);
        return (car.Distance + car.DistanceToFront)
            + Mathf.Floor((car.Distance + car.DistanceToFront) / laneLen) * laneLen;
    }
}

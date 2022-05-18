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
            else return b.Position.CompareTo(a.Position);
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        TrackConfig config = SystemAPI.GetSingleton<TrackConfig>();
        NativeArray<CarAspect> cars = CollectionHelper.CreateNativeArray<CarAspect>(config.numberOfCars, Allocator.Temp);
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
    }
}

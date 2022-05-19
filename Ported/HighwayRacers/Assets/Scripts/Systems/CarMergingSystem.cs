using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
namespace HighwayRacers
{
    //[UpdateAfter(typeof(CarPeerSystem))]
    //public partial struct CarMergingSystem : ISystem
    //{
    //    public void OnCreate(ref SystemState state)
    //    {
    //        state.RequireForUpdate<TrackConfig>();
    //    }

    //    public void OnDestroy(ref SystemState state)
    //    {
    //    }

    //    [BurstCompile]
    //    public void OnUpdate(ref SystemState state)
    //    {
    //        foreach (var car in SystemAPI.Query<CarAspect>())
    //        {
    //            if (!car.IsMerging) QueryMerge(in car, ref state);
    //            else UpdateMerge(in car, ref state);
    //        }
    //    }

    //    private void QueryMerge(in CarAspect car, ref SystemState state)
    //    {
    //        // in front of car
    //        var speed = state.EntityManager.GetComponentData<CarSpeed>(car.Entity);
    //        // try left
    //        if (car.CanMergeLeft && car.DistanceAhead < car.LeftMergeDistance // close enough to car in front
    //                && car.OvertakeEagerness > speed.currentSpeed / car.DefaultSpeed) // car in front is slow enough
    //        {
    //            car.MergeLeft();
    //        }
    //    }

    //    private void UpdateMerge(in CarAspect car, ref SystemState state)
    //    {
    //        var track = SystemAPI.GetSingleton<TrackConfig>();
    //        car.MergeProgress += Time.deltaTime * track.switchLanesSpeed;
    //        if (car.MergeProgress >= 1f) car.ConcludeMerge();
    //    }
    //}

    [UpdateAfter(typeof(CarPeerSystem))]
    public partial struct CarDecideToMergeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TrackConfig>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var track = SystemAPI.GetSingleton<TrackConfig>();

            foreach (var car in SystemAPI.Query<CarMergingAspect>())
            {
                if (!car.IsMerging && car.MinDistanceInFront >= car.DistanceInFront)
                {
                    if (car.CanMergeLeft)
                    {
                        car.MergeLeft(track.highwaySize);
                    }
                    else if (car.CanMergeRight)
                    {
                        car.MergeRight(track.highwaySize);
                    }
                }
            }
        }
    }

    [UpdateAfter(typeof(CarDecideToMergeSystem))]
    public partial struct CarUpdateMergeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TrackConfig>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = Time.deltaTime;

            foreach (var car in SystemAPI.Query<CarMergingAspect>())
            {
                if (car.IsMerging)
                {
                    car.MergeProgress += dt;
                    if (car.MergeProgress >= 1.0f)
                    {
                        car.ConcludeMerge();
                    }
                }
            }
        }
    }


}
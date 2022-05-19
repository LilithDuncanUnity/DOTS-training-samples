using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;

public struct CarGroupingBucket
{
    public int firstIndex;
    public int carCount;
}

[UpdateAfter(typeof(CarSpawningSystem))]
partial struct CarPeerSystem : ISystem
{
    static readonly ProfilerMarker s_PrepareCarList = new ProfilerMarker("PrepareCarList");
    static readonly ProfilerMarker s_SortCarList = new ProfilerMarker("SortCarList");
    static readonly ProfilerMarker s_CalcPeers = new ProfilerMarker("CalcPeers");
    static readonly ProfilerMarker s_BucketCars = new ProfilerMarker("BucketCars");
    static readonly ProfilerMarker s_CalcMergeInfo = new ProfilerMarker("CalcMergeInfo");

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
            if (a.Lane != b.Lane) return a.Lane.CompareTo(b.Lane);
            else return a.Distance.CompareTo(b.Distance);
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        s_PrepareCarList.Begin();
        TrackConfig track = SystemAPI.GetSingleton<TrackConfig>();
        NativeArray<CarAspect> cars = CollectionHelper.CreateNativeArray<CarAspect>(track.numberOfCars, Allocator.Temp);
        int i = 0;

        foreach (var car in SystemAPI.Query<CarAspect>())
        {
            car.CarInFront = Entity.Null;
            cars[i] = car;
            i++;
        }
        s_PrepareCarList.End();

        s_SortCarList.Begin();
        // TODO: figure out how to sort only a subsection of the array
        if (i < cars.Length) return;

        cars.Sort<CarAspect, CarComparer>(new CarComparer());
        s_SortCarList.End();

        s_CalcPeers.Begin();
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
        s_CalcPeers.End();

        s_BucketCars.Begin();
        int numBucketsPerLane = (int)math.ceil(track.highwaySize / 20.0f);
        NativeArray<CarGroupingBucket> laneBuckets = new NativeArray<CarGroupingBucket>(4 * numBucketsPerLane, Allocator.Temp);
        NativeArray<float> laneLengths = new NativeArray<float>(4, Allocator.Temp);
        laneLengths[0] = TrackUtilities.GetLaneLength(track.highwaySize, 0);
        laneLengths[1] = TrackUtilities.GetLaneLength(track.highwaySize, 1);
        laneLengths[2] = TrackUtilities.GetLaneLength(track.highwaySize, 2);
        laneLengths[3] = TrackUtilities.GetLaneLength(track.highwaySize, 3);

        for (int carIndex = 0; carIndex < cars.Length; ++carIndex)
        {
            int bucketIndex = CalculateBucket(laneLengths, numBucketsPerLane, cars[carIndex].Distance, cars[carIndex].Lane);
            
            CarGroupingBucket bucket = laneBuckets[bucketIndex];
            if (bucket.firstIndex == 0 && bucket.carCount == 0) 
            {
                bucket.firstIndex = carIndex;
                bucket.carCount = 1;
            }
            else
            {
                ++bucket.carCount;
            }
            laneBuckets[bucketIndex] = bucket;
        }
        s_BucketCars.End();

        s_CalcMergeInfo.Begin();
        // calculate can merge
        foreach (var car in cars)
        {
            if (car.Lane == 0) car.CanMergeRight = false;
            else car.CanMergeRight = CanMergeToLane(in car, car.Lane - 1, in cars, in laneBuckets, numBucketsPerLane, in laneLengths);

            if (car.Lane == 3) car.CanMergeLeft = false;
            else car.CanMergeLeft = CanMergeToLane(in car, car.Lane + 1, in cars, in laneBuckets, numBucketsPerLane, in laneLengths);
        }
        s_CalcMergeInfo.End();

        laneBuckets.Dispose();
        laneLengths.Dispose();
    }

    int CalculateBucket(NativeArray<float> laneLengths, int numBucketsPerLane, float distance, int lane) 
    {
        int bucketIndex = numBucketsPerLane * lane;
        distance = TrackUtilities.WrapDistance(laneLengths[0], distance, lane);
        int laneBucketOffset = (int)math.floor(numBucketsPerLane * distance / laneLengths[lane]);
        bucketIndex += laneBucketOffset;
        return bucketIndex;
    }

    bool CanMergeToLane(in CarAspect car, int lane, in NativeArray<CarAspect> cars, in NativeArray<CarGroupingBucket> laneBuckets, int numBucketsPerLane, in NativeArray<float> laneLengths)
    {
        bool canMerge = true;

        // Get the range that needs to be open.
        float distanceBack = TrackUtilities.GetEquivalentDistance(laneLengths[0], car.Distance - car.MergeSpace, car.Lane, lane);
        float distanceForward = TrackUtilities.GetEquivalentDistance(laneLengths[0], car.Distance + car.MinDistanceInFront, car.Lane, lane);

        int startBucket = CalculateBucket(laneLengths, numBucketsPerLane, distanceBack, lane);
        int endBucket = CalculateBucket(laneLengths, numBucketsPerLane, distanceForward, lane);
        int actualEndBucket = math.min(((lane + 1) * numBucketsPerLane) - 1, endBucket);

        for (int bucketIndex = 0; bucketIndex <= actualEndBucket && canMerge; ++bucketIndex)
        {
            var bucket = laneBuckets[bucketIndex];
            for (int carIndex = bucket.firstIndex; carIndex < bucket.firstIndex + bucket.carCount; ++carIndex)
            {
                float distance = TrackUtilities.WrapDistance(laneLengths[0], cars[carIndex].Distance, cars[carIndex].Lane);
                if (distanceBack < distanceForward)
                {
                    if (distance >= distanceBack && distance <= distanceForward)
                    {
                        canMerge = false;
                        break;
                    }
                }
                else if (distance >= distanceBack || distance <= distanceForward)
                {
                    canMerge = false;
                    break;
                }
            }
        }

        if (canMerge && endBucket != actualEndBucket)
        {
            for (int bucketIndex = lane * numBucketsPerLane; bucketIndex <= endBucket; ++bucketIndex)
            {
                var bucket = laneBuckets[bucketIndex];
                for (int carIndex = bucket.firstIndex; carIndex < bucket.firstIndex + bucket.carCount; ++carIndex)
                {
                    float distance = TrackUtilities.WrapDistance(laneLengths[0], cars[carIndex].Distance, cars[carIndex].Lane);
                    if (distanceBack < distanceForward)
                    {
                        if (distance >= distanceBack && distance <= distanceForward)
                        {
                            canMerge = false;
                            break;
                        }
                    }
                    else if (distance >= distanceBack || distance <= distanceForward)
                    {
                        canMerge = false;
                        break;
                    }
                }
            }
        }

        return canMerge;

        //float distanceBack = TrackUtilities.GetEquivalentDistance(laneLengths[0], GetDistanceBack(in car, laneLengths[0]) - car.MergeSpace, car.Lane, lane);
        //float distanceFront = TrackUtilities.GetEquivalentDistance(laneLengths[0], GetDistanceFront(in car, laneLengths[0]) - car.MergeSpace, car.Lane, lane);

        //// TODO: optimize this from n2
        //foreach (var other in cars)
        //{
        //    if (car.Entity == other.Entity) continue;
        //    if (TrackUtilities.AreasOverlap(laneLengths[0], lane, distanceBack, distanceFront, other.Lane, GetDistanceBack(in other, laneLengths[0]),
        //        GetDistanceFront(in other, laneLengths[0])))
        //    {
        //        return false;
        //    }
        //}
        return true;
    }

    //float GetDistanceBack(in CarAspect car, float lane0Length)
    //{
    //    var laneLen = TrackUtilities.GetLaneLength(lane0Length, car.Lane);
    //    return (car.Distance - car.DistanceToBack)
    //        + Mathf.Floor((car.Distance - car.DistanceToBack) / laneLen) * laneLen;
    //}

    //float GetDistanceFront(in CarAspect car, float lane0Length)
    //{
    //    var laneLen = TrackUtilities.GetLaneLength(lane0Length, car.Lane);
    //    return (car.Distance + car.DistanceToFront)
    //        + Mathf.Floor((car.Distance + car.DistanceToFront) / laneLen) * laneLen;
    //}
}

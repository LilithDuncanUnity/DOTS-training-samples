using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;


[UpdateAfter(typeof(CarSpawningSystem))]
[BurstCompile]
partial struct CarBuildAICacheSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TrackConfig>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public struct CarGroupingBucket
    {
        public int firstIndex;
        public int carCount;
    }

    public struct CarDistanceInfo
    {
        public Entity Car;
        public float Distance;
        public float WrappedDistance;
        public float CurrentSpeed;
    }

    public struct CarLaneInfo
    {
        public float LaneLength;
        public int CarCount;
        public NativeArray<CarDistanceInfo> CarDistances;
        public NativeArray<CarGroupingBucket> Buckets;
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
    struct ProcessLanesJob : IJob
    {
        public CarLaneInfo Lane;
        public int LaneIndex;
        public float HighwaySize;
        public EntityCommandBuffer ECB;

        [BurstCompile]
        public static int CalculateBucket(int bucketCount, float laneLength, float wrappedDistance)
        {
            int bucketIndex = (int)math.floor(bucketCount * wrappedDistance / laneLength);
            return bucketIndex;
        }

        [BurstCompile]
        public void Execute()
        {
            Lane.CarDistances.Sort<CarDistanceInfo, CarDistanceComparer>(new CarDistanceComparer());

            for (int carIndex = 0; carIndex < Lane.CarCount; ++carIndex)
            {
                float distanceAhead = Lane.LaneLength;
                float distanceBehind = distanceAhead;
                float carAheadSpeed = 0.0f;
                Entity carInFront = Entity.Null;

                if (carIndex > 0)
                {
                    distanceBehind = TrackUtilities.WrapDistance(HighwaySize, Lane.CarDistances[carIndex].Distance - Lane.CarDistances[carIndex - 1].Distance, LaneIndex);
                }
                else if (Lane.CarCount > 1)
                {
                    distanceBehind = Lane.LaneLength - TrackUtilities.WrapDistance(HighwaySize, Lane.CarDistances[Lane.CarCount - 1].Distance - Lane.CarDistances[carIndex].Distance, LaneIndex);
                }

                if (carIndex < Lane.CarCount - 1)
                {
                    distanceAhead = TrackUtilities.WrapDistance(HighwaySize, Lane.CarDistances[carIndex + 1].Distance - Lane.CarDistances[carIndex].Distance, LaneIndex);
                    carInFront = Lane.CarDistances[carIndex + 1].Car;
                    carAheadSpeed = Lane.CarDistances[carIndex + 1].CurrentSpeed;
                }
                else if (Lane.CarCount > 1)
                {
                    distanceAhead = Lane.LaneLength - TrackUtilities.WrapDistance(HighwaySize, Lane.CarDistances[carIndex].Distance - Lane.CarDistances[0].Distance, LaneIndex);
                    carInFront = Lane.CarDistances[0].Car;
                    carAheadSpeed = Lane.CarDistances[0].CurrentSpeed;
                }

                // Set the cached AI information to make later systems easier to write
                ECB.SetComponent(Lane.CarDistances[carIndex].Car, new CarAICache
                {
                    CarInFront = carInFront,
                    CarInFrontSpeed = carAheadSpeed,
                    DistanceAhead = distanceAhead,
                    DistanceBehind = distanceBehind
                });
            }

            // Build a spatial partition bucketing the lanes into sections approximately 20 units in length
            for (int carIndex = 0; carIndex < Lane.CarCount; ++carIndex)
            {
                int bucketIndex = CalculateBucket(Lane.Buckets.Length, Lane.LaneLength, Lane.CarDistances[carIndex].WrappedDistance);
                CarGroupingBucket bucket = Lane.Buckets[bucketIndex];

                if (bucket.carCount == 0)
                {
                    bucket.firstIndex = carIndex;
                    bucket.carCount = 1;
                }
                else
                {
                    ++bucket.carCount;
                }

                Lane.Buckets[bucketIndex] = bucket;
            }
        }
    }

    [BurstCompile]
    partial struct CalcMergeInfoJob : IJobEntity
    {
        [ReadOnly] public NativeArray<CarLaneInfo> CarLanes;
        [ReadOnly] public float HighwaySize;

        void Execute([ChunkIndexInQuery] int chunkIndex, ref CarAICacheAspect car)
        {
            if (car.Lane == 0)
                car.CanMergeRight = false;
            else
            {
                CarLaneInfo targetLane = CarLanes[car.Lane - 1];
                car.CanMergeRight = CanMergeToLane(in car, car.Lane - 1, ref targetLane, HighwaySize);
            }


            if (car.Lane == 3)
                car.CanMergeLeft = false;
            else
            {
                CarLaneInfo targetLane = CarLanes[car.Lane + 1];
                car.CanMergeLeft = CanMergeToLane(in car, car.Lane + 1, ref targetLane, HighwaySize);
            }
        }

        static bool CanMergeToLane(in CarAICacheAspect car, int targetLane, ref CarLaneInfo targetLaneInfo, float lane0Length)
        {
            float distanceBack = TrackUtilities.GetEquivalentDistance(lane0Length, car.Distance - car.MergeSpace, car.Lane, targetLane);
            float distanceFront = TrackUtilities.GetEquivalentDistance(lane0Length, car.Distance + car.MinDistanceInFront, car.Lane, targetLane);

            distanceBack = TrackUtilities.WrapDistance(lane0Length, distanceBack, targetLane);
            distanceFront = TrackUtilities.WrapDistance(lane0Length, distanceFront, targetLane);

            int backBucket = ProcessLanesJob.CalculateBucket(targetLaneInfo.Buckets.Length, targetLaneInfo.LaneLength, distanceBack);
            int frontBucket = ProcessLanesJob.CalculateBucket(targetLaneInfo.Buckets.Length, targetLaneInfo.LaneLength, distanceFront);
            int firstLoopFrontBucket = frontBucket >= backBucket ? frontBucket : targetLaneInfo.Buckets.Length - 1;
            float firstLoopDistanceFront = frontBucket >= backBucket ? distanceFront : targetLaneInfo.LaneLength;

            for (int bucketIndex = backBucket; bucketIndex <= firstLoopFrontBucket; ++bucketIndex)
            {
                CarGroupingBucket bucket = targetLaneInfo.Buckets[bucketIndex];

                for (int carIndex = 0; carIndex < bucket.carCount; ++carIndex)
                {
                    float distance = targetLaneInfo.CarDistances[bucket.firstIndex + carIndex].WrappedDistance;

                    if (distance > distanceBack && distance < firstLoopDistanceFront)
                    {
                        return false;
                    }
                }
            }

            // If the merge range of distances overlaps the wrap point of the track, we need to do a pass for the remaining buckets
            if (firstLoopFrontBucket != frontBucket)  // Buckets are wrapping around
            {
                for (int bucketIndex = 0; bucketIndex <= frontBucket; ++bucketIndex)
                {
                    CarGroupingBucket bucket = targetLaneInfo.Buckets[bucketIndex];

                    for (int carIndex = 0; carIndex < bucket.carCount; ++carIndex)
                    {
                        float distance = targetLaneInfo.CarDistances[bucket.firstIndex + carIndex].WrappedDistance;

                        if (distance < distanceFront)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        TrackConfig track = SystemAPI.GetSingleton<TrackConfig>();
        int numBucketsPerLane = (int)math.ceil(track.highwaySize / 20.0f);

        //NativeArray<CarAspect> cars = CollectionHelper.CreateNativeArray<CarAspect>(track.numberOfCars, Allocator.Temp);
        NativeArray<CarLaneInfo> carLanes = new NativeArray<CarLaneInfo>(4, Allocator.TempJob);
        for (int laneIndex = 0; laneIndex < 4; ++laneIndex)
        {
            carLanes[laneIndex] = new CarLaneInfo
            {
                LaneLength = TrackUtilities.GetLaneLength(track.highwaySize, laneIndex),
                CarCount = 0,
                CarDistances = new NativeArray<CarDistanceInfo>(track.numberOfCars, Allocator.TempJob),
                Buckets = new NativeArray<CarGroupingBucket>(numBucketsPerLane, Allocator.TempJob)
            };
        }

        // Build list of car distances
        foreach (var car in SystemAPI.Query<CarPositionAspect>())
        {
            float distance = car.Distance;
            CarLaneInfo laneInfo = carLanes[car.Lane];

            laneInfo.CarDistances[laneInfo.CarCount] = new CarDistanceInfo
            {
                Car = car.Entity,
                Distance = distance,
                WrappedDistance = TrackUtilities.WrapDistance(track.highwaySize, distance, car.Lane),
                CurrentSpeed = car.CurrentSpeed
            };
            laneInfo.CarCount++;

            carLanes[car.Lane] = laneInfo;
        }

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        // Schedule Jobs here
        ProcessLanesJob lane0Job = new ProcessLanesJob
        {
            Lane = carLanes[0],
            LaneIndex = 0,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            HighwaySize = track.highwaySize
        };
        ProcessLanesJob lane1Job = new ProcessLanesJob
        {
            Lane = carLanes[1],
            LaneIndex = 1,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            HighwaySize = track.highwaySize
        }; 
        ProcessLanesJob lane2Job = new ProcessLanesJob
        {
            Lane = carLanes[2],
            LaneIndex = 2,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            HighwaySize = track.highwaySize
        }; 
        ProcessLanesJob lane3Job = new ProcessLanesJob
        {
            Lane = carLanes[3],
            LaneIndex = 3,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            HighwaySize = track.highwaySize
        };

        JobHandle lane0Handle = lane0Job.Schedule();
        JobHandle lane1Handle = lane1Job.Schedule();
        JobHandle lane2Handle = lane2Job.Schedule();
        JobHandle lane3Handle = lane3Job.Schedule();

        JobHandle allJobs = JobHandle.CombineDependencies(JobHandle.CombineDependencies(lane0Handle, lane1Handle), JobHandle.CombineDependencies(lane2Handle, lane3Handle));
        allJobs.Complete();

        CalcMergeInfoJob calcMergeJob = new CalcMergeInfoJob
        {
            CarLanes = carLanes,
            HighwaySize = track.highwaySize
        };
        JobHandle mergeJobHandle = calcMergeJob.ScheduleParallel(allJobs);
        carLanes.Dispose(mergeJobHandle);
    }
}

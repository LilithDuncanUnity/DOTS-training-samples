using System.Resources;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile]
partial struct CarSpawningSystem : ISystem
{
    private EntityQuery m_CarQuery;

    public bool NeedsRegenerating
    {
        get => _needsRegenerating;
        set => _needsRegenerating = value;
    }
    private bool _needsRegenerating;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CarConfig>();
        state.RequireForUpdate<TrackConfig>();
        m_CarQuery = state.GetEntityQuery(typeof(CarPosition));

        NeedsRegenerating = true;
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        SystemAPI.TryGetSingletonEntity<SelectedCar>(out Entity sce);
        if (sce == Entity.Null)
        {
            var e = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<SelectedCar>(e);
            state.EntityManager.SetComponentData<SelectedCar>(e, new()
            {
                Selected = Entity.Null
            });
        }

        if (NeedsRegenerating) {
            var config = SystemAPI.GetSingleton<CarConfig>();
            var trackConfig = SystemAPI.GetSingleton<TrackConfig>();

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var allocator = state.WorldUnmanaged.UpdateAllocator.ToAllocator;
            var vehicles = CollectionHelper.CreateNativeArray<Entity>(trackConfig.numberOfCars, allocator);
            ecb.Instantiate(config.CarPrefab, vehicles);

            var random = Random.CreateFromIndex(501);

            foreach (var vehicle in vehicles)
            {
                ecb.SetComponent(vehicle, new CarPosition
                {
                    distance = random.NextFloat(0, trackConfig.highwaySize),
                    currentLane = random.NextInt(4)
                });

                // setting current and desired to be the same on spawn. Current is what is modified from AI and tries to match desired
                float localDesiredSpeed = random.NextFloat(config.MinDefaultSpeed, config.MaxDefaultSpeed);

                ecb.SetComponent(vehicle, new CarSpeed
                {
                    currentSpeed = localDesiredSpeed
                });

                ecb.SetComponent(vehicle, new CarProperties
                {
                    desiredSpeed = localDesiredSpeed,
                    overTakePercent = random.NextFloat(config.MinOvertakeSpeedScale, config.MaxOvertakeSpeedScale),
                    minDistanceInFront = random.NextFloat(config.MinDistanceInFront, config.MaxDistanceInFront),
                    mergeSpace = random.NextFloat(config.MinMergeSpace, config.MaxMergeSpace),
                    overTakeEagerness = random.NextFloat(config.MinOvertakeEagerness, config.MaxOvertakeEagerness)
                });

                ecb.SetComponent(vehicle, new CarColor { defaultColor = UnityEngine.Color.white, fastColor = UnityEngine.Color.green, slowColor = UnityEngine.Color.red });
            }

            NeedsRegenerating = false;
        }
    }

    public void RespawnCars(EntityManager entityManager)
    {
        // Remove the tracks that have already been created (if any)
        NativeArray<Entity> entitiesToRemove = m_CarQuery.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entitiesToRemove)
        {
            entityManager.DestroyEntity(entity);
        }
        entitiesToRemove.Dispose();

        NeedsRegenerating = true;
    }
}

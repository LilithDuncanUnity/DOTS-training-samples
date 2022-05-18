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
                float3 startingPos = new float3(75, 0, 50 + (random.NextFloat() * 100f - 50f));
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

                var hue = random.NextFloat();

                // Helper to create any amount of colors as distinct from each other as possible.
                // The logic behind this approach is detailed at the following address:
                // https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
                hue = (hue + 0.618034005f) % 1;
                var color = UnityEngine.Color.HSVToRGB(hue, 1.0f, 1.0f);
                CarColor baseColor = new CarColor { currentColor = new float3(color.r, color.g, color.b) };
                ecb.SetComponent(vehicle, baseColor);
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
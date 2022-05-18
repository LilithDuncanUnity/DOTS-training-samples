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
    private EntityQuery m_BaseColorQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CarConfig>();
        state.RequireForUpdate<TrackConfig>();
        m_BaseColorQuery = state.GetEntityQuery(typeof(URPMaterialPropertyBaseColor));
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

        var config = SystemAPI.GetSingleton<CarConfig>();
        var trackConfig = SystemAPI.GetSingleton<TrackConfig>();

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var allocator = state.WorldUnmanaged.UpdateAllocator.ToAllocator;
        var vehicles = CollectionHelper.CreateNativeArray<Entity>(config.CarCount, allocator);
        ecb.Instantiate(config.CarPrefab, vehicles);

        var random = Random.CreateFromIndex(501);
        var queryMask = m_BaseColorQuery.GetEntityQueryMask();

        foreach (var vehicle in vehicles)
        {
            float3 startingPos = new float3(75, 0, 50 + (random.NextFloat() * 100f - 50f));
            ecb.SetComponent(vehicle, new CarPosition
            {
                distance = random.NextFloat(0, trackConfig.highwaySize),
                currentLane = random.NextInt(4)
            });

            var hue = random.NextFloat();

            // Helper to create any amount of colors as distinct from each other as possible.
            // The logic behind this approach is detailed at the following address:
            // https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
            hue = (hue + 0.618034005f) % 1;
            var color = UnityEngine.Color.HSVToRGB(hue, 1.0f, 1.0f);
            URPMaterialPropertyBaseColor baseColor = new URPMaterialPropertyBaseColor { Value = (UnityEngine.Vector4)color };
            ecb.SetComponentForLinkedEntityGroup(vehicle, queryMask, baseColor);
        }

        // This system should only run once at startup. So it disables itself after one update.
        state.Enabled = false;
    }
}
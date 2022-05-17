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
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CarConfig>();
        state.RequireForUpdate<TrackConfig>();
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
       
        foreach (var vehicle in vehicles)
        {
            float3 startingPos = new float3(75, 0, 50 + (random.NextFloat() * 100f - 50f));
            ecb.SetComponent(vehicle, new CarPosition
            {
                distance = random.NextFloat(0, trackConfig.highwaySize),
                currentLane = random.NextInt(4)
            });
        }

        // This system should only run once at startup. So it disables itself after one update.
        state.Enabled = false;
    }
}
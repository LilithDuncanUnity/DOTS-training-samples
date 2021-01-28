using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public class SpawnFromFoodSystem : SystemBase
{
    EntityQuery m_Query;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<SpawnZones>();
        RequireForUpdate(m_Query);
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var random = new Random(5678);
        var zones = GetSingleton<SpawnZones>();
        var physicsData = EntityManager.GetComponentData<PhysicsData>(zones.BeePrefab);
        Entities
            .WithName("SpawnFromFood")
            .WithAll<FoodTag>()
            .WithNone<CarrierBee>()
            .WithoutBurst()
            .WithStoreEntityQueryInField(ref m_Query)
            .ForEach((Entity e, in PhysicsData d, in Translation t) =>
            {
                if (t.Value.y <= d.floor)
                {
                    const float speed = 2500;
                    if (zones.Team1Zone.Contains(t.Value))
                    {
                        for (int i = 0; i < zones.BeesPerFood; ++i)
                        {
                            var newBee = ecb.Instantiate(zones.BeePrefab);
                            ecb.SetComponent(newBee, new Translation
                            {
                                Value = t.Value,
                            });
                            var newPhysics = physicsData;
                            newPhysics.a = UnityEngine.Random.insideUnitSphere * speed;
                            ecb.SetComponent(newBee, newPhysics);
                            ecb.AddComponent<Team1>(newBee);
                            ecb.AddComponent(newBee, new URPMaterialPropertyBaseColor
                            {
                                Value = new float4(1, 1, 0, 1),
                            });
                        }

                        ecb.DestroyEntity(e);
                    }
                    if (zones.Team2Zone.Contains(t.Value))
                    {
                        for (int i = 0; i < zones.BeesPerFood; ++i)
                        {
                            var newBee = ecb.Instantiate(zones.BeePrefab);
                            ecb.SetComponent(newBee, new Translation
                            {
                                Value = t.Value,
                            });
                            var newPhysics = physicsData;
                            newPhysics.a = UnityEngine.Random.insideUnitSphere * speed;
                            ecb.SetComponent(newBee, newPhysics);
                            ecb.AddComponent<Team2>(newBee);
                            ecb.AddComponent(newBee, new URPMaterialPropertyBaseColor
                            {
                                Value = new float4(0, 1, 1, 1),
                            });
                        }

                        ecb.DestroyEntity(e);
                    }
                }
            }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
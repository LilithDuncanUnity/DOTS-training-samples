using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class BeeAttackerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var spawner = GetSingletonEntity<GlobalData>();
        var spawnerComponent = GetComponent<GlobalData>(spawner);

        var dt = World.Time.DeltaTime;
        var sys = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        var ecb = sys.CreateCommandBuffer();
        var lookUpTraslation = GetComponentDataFromEntity<Translation>(true);
        var lookUpVelocity = GetComponentDataFromEntity<Velocity>(true);
        var lookUpBee = GetComponentDataFromEntity<Bee>(true);
        Random random = new Random(3045);

        Entities
            .WithNativeDisableContainerSafetyRestriction(lookUpTraslation)
            .WithNativeDisableContainerSafetyRestriction(lookUpVelocity)
            .WithNativeDisableContainerSafetyRestriction(lookUpBee)
            .WithAll<BeeAttackMode>()
            .WithNone<Ballistic, Decay>()
            .WithReadOnly(lookUpTraslation)
            .WithReadOnly(lookUpBee)
            .WithReadOnly(lookUpVelocity)
            .ForEach((Entity entity, ref Bee bee, in Velocity velocity, in Translation position) =>
            {
                var othervel = lookUpVelocity[bee.TargetEntity];
                var otherPos = lookUpTraslation[bee.TargetEntity];
                if (lookUpBee.HasComponent(bee.TargetEntity))
                {
                    var otherBee = lookUpBee[bee.TargetEntity];
                    if (otherBee.TargetEntity != Entity.Null && HasComponent<TargetedBy>(otherBee.TargetEntity))
                    {
                        ecb.SetComponent(otherBee.TargetEntity, new TargetedBy { Value = Entity.Null });
                    }
                }

                ecb.AddComponent(bee.TargetEntity, new Ballistic());
                
                ecb.RemoveComponent<BeeAttackMode>(entity);
                ecb.AddComponent<BeeIdleMode>(entity);
                bee.TargetEntity = Entity.Null;

                int totalGiblets = random.NextInt(5, 10);
                for (int i = 0; i < totalGiblets; ++i)
                {
                    var giblet = ecb.Instantiate(spawnerComponent.GibletPrefab);
                    ecb.SetComponent<Translation>(giblet, otherPos);
                    ecb.SetComponent<Velocity>(giblet, new Velocity
                    {
                        Value = othervel.Value + (random.NextFloat3Direction() * 2.0f)
                    });
                }
            }).Schedule();
        sys.AddJobHandleForProducer(Dependency);
    }
}
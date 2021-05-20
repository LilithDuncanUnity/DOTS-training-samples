using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(BeeUpdateGroup))]
[UpdateAfter(typeof(BeePerception))]
public class BeeGatheringSystem : SystemBase
{
    private EntityCommandBufferSystem EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        EntityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = EntityCommandBufferSystem.CreateCommandBuffer();

        // Query for bees that are close enough to a Resource target to collect the Resource
        // TODO how to stop 2 bees collecting the same Resource
        var cdfeForTranslation = GetComponentDataFromEntity<Translation>(true);
        var yellowBase = GetSingletonEntity<YellowBase>();
        var yellowBaseAABB = EntityManager.GetComponentData<Bounds>(yellowBase).Value;

        var blueBase = GetSingletonEntity<BlueBase>();
        var blueBaseAABB = EntityManager.GetComponentData<Bounds>(blueBase).Value;

        Entities
             .WithReadOnly(cdfeForTranslation)
             .WithAll<IsGathering>()
             .ForEach((Entity entity, ref TargetPosition targetPosition, in Target target, in Translation translation, in Team team) =>
             {
                 if (cdfeForTranslation.HasComponent(target.Value)) //(Value.StorageInfoFromEntity.Exists(target))
                 {
                     if (math.distancesq(translation.Value, cdfeForTranslation[target.Value].Value) < 0.025)
                     {
                         ecb.RemoveComponent<IsGathering>(entity);
                         ecb.AddComponent<IsReturning>(entity);
                         ecb.AddComponent<IsCarried>(target.Value);
                         ecb.RemoveComponent<OnCollision>(target.Value);

                         targetPosition.Value = team.Id == 0 ? yellowBaseAABB.Center : blueBaseAABB.Center;
                     }
                 }
             }).Schedule();

        EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
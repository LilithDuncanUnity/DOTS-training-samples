using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityCamera = UnityEngine.Camera;
using UnityGameObject = UnityEngine.GameObject;
using UnityInput = UnityEngine.Input;
using UnityKeyCode = UnityEngine.KeyCode;
using UnityMeshRenderer = UnityEngine.MeshRenderer;
using UnityMonoBehaviour = UnityEngine.MonoBehaviour;
using UnityRangeAttribute = UnityEngine.RangeAttribute;

public class BeePerception : SystemBase
{
    private EntityQuery m_queryResources;
    [ReadOnly]
    private ComponentDataFromEntity<IsCarried> cdfe;

    protected override void OnCreate()
    {
        // Query list of Resources available to collect
        EntityQueryDesc queryResourcesDesc = new EntityQueryDesc()
        {
            All = new ComponentType[] { typeof(IsResource)},
            None = new ComponentType[] { typeof(IsCarried), typeof(HasGravity), typeof(LifeSpan) }
        };
        m_queryResources = GetEntityQuery(queryResourcesDesc);
    }

    protected override void OnUpdate()
    {
        cdfe = GetComponentDataFromEntity<IsCarried>(true);
        var random = new Random(1234);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // TODO what about bees that are targetting Resources that have been
        // destroyed because they where returned to a hive?

        // Query for bees that are targetting Resources that are being carried by
        // another bee and clear their target Resource
        Entities
            .WithoutBurst()
            .WithNone<IsReturning>()
            .WithAll<IsBee>()
            .ForEach((Entity entity, in Target target) =>
            {
                if (cdfe.HasComponent(target.Value))
                {
                    ecb.RemoveComponent<Target>(entity);
                }
            }).Run();

        // Get list of Resources available to collect
        using (var resources = m_queryResources.ToEntityArray(Allocator.TempJob))
        {
            if (resources.Length > 0)
            {
                // Query for bees that are not dead, carrying, attacking or returning
                // and set a random Resource as the target the bee is trying to collect         
                Entities
                    .WithAll<IsBee>()
                    .WithNone<Target, IsDead>()
                    .ForEach((Entity entity) =>
                    {
                        // pick a random resource and add target component to bee
                        ecb.AddComponent(entity, new Target
                        {
                            Value = resources[random.NextInt(0, resources.Length)]
                        });
                        ecb.AddComponent(entity, new TargetPosition());
                    // tag the bee as now gathering
                    ecb.AddComponent<IsGathering>(entity);
                    }).Run();
            }
        }
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

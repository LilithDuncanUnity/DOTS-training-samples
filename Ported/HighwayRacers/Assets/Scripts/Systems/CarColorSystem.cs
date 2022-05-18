using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[UpdateAfter(typeof(CarSpawningSystem))]
public partial struct CarColorSystem : ISystem
{
    private EntityQuery m_BaseColorQuery;
    public void OnCreate(ref SystemState state)
    {
        m_BaseColorQuery = state.GetEntityQuery(typeof(URPMaterialPropertyBaseColor));
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        bool preview = false;
        foreach (var car in SystemAPI.Query<CarAspect>())
        {
            if (car.Preview) preview = true;
        }

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var queryMask = m_BaseColorQuery.GetEntityQueryMask();
        foreach (var car in SystemAPI.Query<CarAspect>())
        {
            if (car.Preview)
            {
                ecb.SetComponentForLinkedEntityGroup(car.Entity, queryMask, new URPMaterialPropertyBaseColor { Value = (Vector4)Color.magenta });
            }
            else
            {
                ecb.SetComponentForLinkedEntityGroup(car.Entity, queryMask, new URPMaterialPropertyBaseColor { Value = (Vector4)(car.Color / (preview ? 4f : 1f))});
            }
        }
    }
}
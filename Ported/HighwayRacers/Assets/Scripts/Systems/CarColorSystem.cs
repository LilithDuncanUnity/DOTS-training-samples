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
        state.RequireForUpdate<CarColor>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        bool preview = false;
        foreach (var car in SystemAPI.Query<CarAspect>())
        {
            if (car.Preview)
            {
                preview = true;
                if (car.CarInFront != Entity.Null)
                {
                    state.EntityManager.SetComponentData(car.CarInFront, new CarPreview()
                    {
                        Preview = false,
                        SecondaryPreview = true
                    });
                }
            }
            else
            {
                if (car.CarInFront != Entity.Null)
                {
                    state.EntityManager.SetComponentData(car.CarInFront, new CarPreview()
                    {
                        Preview = state.EntityManager.GetComponentData<CarPreview>(car.CarInFront).Preview,
                        SecondaryPreview = false
                    });
                }
            }
        }

        var carColors = SystemAPI.GetSingleton<CarColor>();
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var queryMask = m_BaseColorQuery.GetEntityQueryMask();
        foreach (var car in SystemAPI.Query<CarAspect>())
            {
            if (car.Preview)
            {
                ecb.SetComponentForLinkedEntityGroup(car.Entity, queryMask, new URPMaterialPropertyBaseColor { Value = (Vector4)Color.magenta });
            }
            else if (car.SecondaryPreview)
            {
                ecb.SetComponentForLinkedEntityGroup(car.Entity, queryMask, new URPMaterialPropertyBaseColor { Value = (Vector4)Color.cyan });
            }
            else
            {
                Color color = Color.white;
                if (car.CurrentSpeed > car.DesiredSpeed || (car.DistanceAhead > car.MinDistanceInFront && car.CurrentSpeed < car.DesiredSpeed))
                {
                    color = carColors.fastColor;
                }
                else if (car.DesiredSpeed > car.CurrentSpeed)
                {
                    color = carColors.slowColor;
                }
                else
                {
                    color = carColors.defaultColor;
                }
                ecb.SetComponentForLinkedEntityGroup(car.Entity, queryMask, new URPMaterialPropertyBaseColor { Value = (Vector4)(color / (preview ? 4f : 1f))});
            }
        }
    }
}

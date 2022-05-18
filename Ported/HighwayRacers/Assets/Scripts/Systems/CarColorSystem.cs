using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;

[BurstCompile]
partial struct CarMovCarColorSystemementSystem : ISystem
{
    private EntityQuery m_ColorQuery;

    public void OnCreate(ref SystemState state)
    {        
        m_ColorQuery = state.GetEntityQuery(typeof(URPMaterialPropertyBaseColor));
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var queryMask = m_ColorQuery.GetEntityQueryMask();

        URPMaterialPropertyBaseColor newColor = new URPMaterialPropertyBaseColor();

        foreach (var car in SystemAPI.Query<CarColorAspect>())
        {
            if (car.CurrentSpeed > car.DesiredSpeed)
            {
                newColor.Value = (UnityEngine.Vector4)car.FastColor;
            }
            else if (car.DesiredSpeed > car.CurrentSpeed)
            {
                newColor.Value = (UnityEngine.Vector4)car.SlowColor;
            }
            else
            {
                newColor.Value = (UnityEngine.Vector4)car.DefaultColor;
            }
            ecb.SetComponentForLinkedEntityGroup(car.Entity, queryMask, newColor);
        }
    }
}

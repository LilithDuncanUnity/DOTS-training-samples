using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
partial struct TrackGenerationSystem : ISystem
{
    public const int NUM_LANES = 4;
    public const float LANE_SPACING = 1.9f;
    public const float MID_RADIUS = 31.46f;
    public const float CURVE_LANE0_RADIUS = MID_RADIUS - LANE_SPACING * (NUM_LANES - 1) / 2f;
    public const float MIN_HIGHWAY_LANE0_LENGTH = CURVE_LANE0_RADIUS * 4;
    public const float MIN_DIST_BETWEEN_CARS = .7f;
    public const float BASE_SCALE_Y = 6;

    private TransformAspect.EntityLookup m_TransformFromEntity;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TrackConfig>();
        m_TransformFromEntity = new TransformAspect.EntityLookup(ref state, false);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Creating an EntityCommandBuffer to defer the structural changes required by instantiation.
        //var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        //var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        float angle = 0.0f;
        float3 pos = new float3(0.0f, 0.0f, 0.0f);

        m_TransformFromEntity.Update(ref state);
        TrackSectionPrefabs prefabs = default;

        foreach (var trackGenerate in SystemAPI.Query<TrackGenerateAspect>())
        {
            prefabs = trackGenerate.Sections;
        }
        TrackConfig config = SystemAPI.GetSingleton<TrackConfig>();
        float straightPieceLength = TrackUtilities.GetStraightawayLength(config.highwaySize);
        float3 linearSectionScale = new float3(1.0f, 1.0f, straightPieceLength / BASE_SCALE_Y);

        for (int sectionIndex = 0; sectionIndex < 4; ++sectionIndex) 
        { 
            Entity linearSection = state.EntityManager.Instantiate(prefabs.LinearPrefab);
            state.EntityManager.SetComponentData<Translation>(linearSection, new Translation
            {
                Value = pos
            });
            state.EntityManager.SetComponentData<Rotation>(linearSection, new Rotation
            {
                Value = quaternion.RotateY(angle)
            });
            state.EntityManager.AddComponentData<NonUniformScale>(linearSection, new NonUniformScale
            {
                Value = linearSectionScale
            });

            pos += math.mul(quaternion.RotateY(angle), new float3(0, 0, straightPieceLength));

            Entity curvedSection = state.EntityManager.Instantiate(prefabs.CurvedPrefab);
            state.EntityManager.SetComponentData<Translation>(curvedSection, new Translation
            {
                Value = pos
            });
            state.EntityManager.SetComponentData<Rotation>(curvedSection, new Rotation
            {
                Value = quaternion.RotateY(angle)
            });

            pos += math.mul(quaternion.RotateY(angle), new float3(MID_RADIUS, 0, MID_RADIUS));
            angle += math.PI / 2.0f;
        }

        state.Enabled = false;
    }
}

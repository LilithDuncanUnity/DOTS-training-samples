﻿using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//[UpdateAfter(typeof(AgentSpawnerSystem))]
[UpdateBefore(typeof(SeekSystem))]
[UpdateBefore(typeof(CellDisplaySystem))]
[UpdateAfter(typeof(WaterSpawnerSystem))]
public class AgentUpdateSystem : SystemBase
{
    private EntityQuery m_bucketQuery;
	private EntityQuery m_waterQuery;
    private EndSimulationEntityCommandBufferSystem m_endSimECB;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_endSimECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        //float elapsedTime = (float)Time.ElapsedTime;

        var ecb1 = m_endSimECB.CreateCommandBuffer().AsParallelWriter();
       /*var ecb2 = m_endSimECB.CreateCommandBuffer().AsParallelWriter();
        var ecb3 = m_endSimECB.CreateCommandBuffer().AsParallelWriter();
        var ecb4 = m_endSimECB.CreateCommandBuffer().AsParallelWriter();*/

		int watersFoundLastUpdate = m_waterQuery.CalculateEntityCount();

		int waterIndex = 0;
		NativeArray<Entity> waterEntities = new NativeArray<Entity>(watersFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		NativeArray<float3> waterLocations = new NativeArray<float3>(watersFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		NativeArray<float> waterVolumes = new NativeArray<float>(watersFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		NativeArray<bool> waterIsAvailable = new NativeArray<bool>(watersFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

		Entities
			.WithoutBurst()
			.WithStoreEntityQueryInField(ref m_waterQuery)
            .WithAll<WaterTag>()
			.ForEach((Entity entity, in Intensity volume, in LocalToWorld world) =>
            {
				waterEntities[waterIndex] = entity;
				waterLocations[waterIndex] = world.Position;
				waterVolumes[waterIndex] = volume.Value;
				waterIsAvailable[waterIndex] = (volume.Value > 0.0f);
				waterIndex++;
		    })
		.Run();

        // ensure this job runs before other jobs that need buckets.
        int bucketsFoundLastUpdate = m_bucketQuery.CalculateEntityCount(); // this will be calculated by running the query (below - see WithStoreEntityQueryInField)

        int bucketIndex = 0;
        NativeArray<Entity> bucketEntities = new NativeArray<Entity>(bucketsFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float3> bucketLocations = new NativeArray<float3>(bucketsFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<bool> bucketIsEmptyAndOnGround = new NativeArray<bool>(bucketsFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<bool> bucketIsFullAndOnGround = new NativeArray<bool>(bucketsFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        NativeArray<float> bucketFillValue = new NativeArray<float>(bucketsFoundLastUpdate, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        NativeArray<Entity> teamEntity = new NativeArray<Entity>(20 , Allocator.TempJob, NativeArrayOptions.UninitializedMemory);// hardcoded to 2 teams.
        NativeArray<float3> teamLineStartLocation = new NativeArray<float3>(20 , Allocator.TempJob, NativeArrayOptions.UninitializedMemory);// hardcoded to 2 teams.

        Entities
            .WithoutBurst()
            .WithStoreEntityQueryInField(ref m_bucketQuery)
            .WithAll<Bucket>()
            .ForEach((Entity e, in CarryableObject c, in Intensity fillValue, in Translation t) =>
        {
            bucketEntities[bucketIndex] = e;
            bucketLocations[bucketIndex] = new float3(t.Value.x, 0, t.Value.z);
            bucketFillValue[bucketIndex] = fillValue.Value;
            bucketIsFullAndOnGround[bucketIndex] = fillValue.Value > 0.0f && c.CarryingEntity == Entity.Null;
            bucketIsEmptyAndOnGround[bucketIndex] = fillValue.Value - float.Epsilon <= 0.0f && c.CarryingEntity == Entity.Null;
			bucketIndex++;
        }).Run();
        //}).Schedule(Dependency); // nope. modifying index.

        // find teams
        int entityIndex = 0;
        Entities.WithoutBurst().ForEach((Entity e, in Team team) =>
            {
                teamEntity[entityIndex] = e;
                teamLineStartLocation[entityIndex++] = team.PickupLocation;
            }
        ).Run();

        const float arrivalThresholdSq = 0.5f; // square length.
        const float bucketFillRate = 0.1f; // amount to add to bucket volume per frame

        // scooper updates
        Entities
            .WithReadOnly(waterEntities)
			.WithReadOnly(waterLocations)
            .WithReadOnly(waterVolumes)
            .WithReadOnly(waterIsAvailable)
            .WithReadOnly(bucketEntities)
            .WithReadOnly(bucketLocations)
            //.WithReadOnly(bucketFillValue)
            .WithReadOnly(bucketIsEmptyAndOnGround)
            .WithReadOnly(teamEntity)
            .WithReadOnly(teamLineStartLocation)
            .ForEach((Entity e, int entityInQueryIndex, ref Translation t, ref SeekPosition seekComponent, ref Agent agent, in AgentTags.ScooperTag agentTag) =>
        {
            /*
             * Scooper Actions:
             * GET_BUCKET -> GOTO_PICKUP_LOCATION -> FILL_BUCKET -> GOTO_DROPOFF_LOCATION -> DROP_BUCKET
             */
            int bucketEntityIndex = 0; // index of found bucket in temp storage arrays
            Entity bucketEntity = Entity.Null;
            switch (agent.ActionState)
            {
                case (byte) AgentAction.START:
                    agent.ActionState = (byte) AgentAction.GET_BUCKET;
                    break; // would be nice if it could drop into the next state without changing the switch to an if

                case (byte) AgentAction.GET_BUCKET:
                    // find nearest empty bucket
                    FindNearestAndSetSeekTarget(t.Value, bucketLocations, bucketIsEmptyAndOnGround, true, ref seekComponent); // look for nearest empty bucket
                    agent.ActionState = (byte) AgentAction.GOTO_PICKUP_LOCATION; // go to that bucket
                    break;

                case (byte) AgentAction.GOTO_PICKUP_LOCATION:
                    if (math.lengthsq(seekComponent.TargetPos - t.Value) < arrivalThresholdSq)
                    {
                        // target is a bucket, in theory.
                        // pick up bucket (Agent.CarriedEntity = Bucket)
                        if (FindNearestIndex(t.Value, bucketLocations, bucketIsEmptyAndOnGround, true,
                            out bucketEntityIndex)) // look for nearest empty bucket
                        {
                            // check that the bucket is still in the expected location (another bot may have stolen it)
                            if (math.lengthsq((bucketLocations[bucketEntityIndex] - t.Value)) < arrivalThresholdSq)
                            {
                                bucketEntity = bucketEntities[bucketEntityIndex];

                                // the bucket is still here
                                // update carryable component to track carrying entity.
                                ecb1.SetComponent<CarryableObject>(entityInQueryIndex, bucketEntity,
                                    new CarryableObject {CarryingEntity = e});

                                // set new target (go to water to fill the bucket)
                                FindNearestAndSetSeekTarget(t.Value, waterLocations, waterIsAvailable, true,
                                    ref seekComponent); // look for nearest available water

                                agent.ActionState = (byte) AgentAction.FILL_BUCKET;
                                agent.CarriedEntity = bucketEntity;
                            }
                            else
                            {
                                // nearest bucket has moved location since we first looked - navigate to current nearest bucket instead
                                seekComponent.TargetPos =
                                    bucketLocations
                                        [bucketEntityIndex]; // FindNearestIndex + this = equivalent to FindNearestAndSetSeekTarget
                            }
                        }
                    }
                    break;

                case (byte) AgentAction.FILL_BUCKET:
                    if (math.lengthsq(seekComponent.TargetPos - t.Value) < arrivalThresholdSq) // arrived at water target
                    {
                        // find bucket being carried by this entity.
                        for (int i = 0; i < bucketEntities.Length; ++i)
                        {
                            if (bucketEntities[i] == agent.CarriedEntity)
                            {
                                int waterEntityIndex;
                                if ((bucketFillValue[i] < Bucket.MaxVolume) &&
                                    FindNearestIndex(t.Value, waterLocations, waterIsAvailable, true, out waterEntityIndex))
                                {
                                    // empty water source
                                    ecb1.SetComponent<Intensity>(entityInQueryIndex, waterEntities[waterEntityIndex],
                                        new Intensity {Value = math.max(waterVolumes[waterEntityIndex] - bucketFillRate, 0.0f)});

                                    // fill bucket
                                    ecb1.SetComponent<Intensity>(entityInQueryIndex, agent.CarriedEntity,
                                        new Intensity {Value = math.min(bucketFillValue[i] + bucketFillRate, Bucket.MaxVolume)});
                                }
                                else
                                {
                                    // pick a dropoff location - should be the same as current pos?
                                    // or perhaps the start of the line
                                    // or the fire itself
                                    //seekComponent.TargetPos = new float3(20,0,20);

                                    // find this agent's team
                                    int teamIndex = 0;
                                    for (int j = 0; j < teamEntity.Length; ++j)
                                    {
                                        if (teamEntity[j] == agent.Team)
                                        {
                                            teamIndex = j;
                                            break;
                                        }
                                    }

                                    // set the start of the team's line as the target location
                                    seekComponent.TargetPos = teamLineStartLocation[teamIndex];

                                    agent.ActionState = (byte) AgentAction.GOTO_DROPOFF_LOCATION;
                                }
                            }
                        }
                    }
                    break;

                case (byte) AgentAction.GOTO_DROPOFF_LOCATION:
                    if (math.lengthsq(seekComponent.TargetPos - t.Value) < arrivalThresholdSq)
                    {
                        agent.ActionState = (byte) AgentAction.DROP_BUCKET;
                    }
                    break;

                case (byte) AgentAction.DROP_BUCKET:
                    // drop bucket
                    ecb1.SetComponent<CarryableObject>(entityInQueryIndex, agent.CarriedEntity, new CarryableObject { CarryingEntity = Entity.Null } );
                    agent.CarriedEntity = Entity.Null; // nb - this will be out of sync with bucket status for one frame (bucket will be updated after simulation end)

                    // need to update bucket position to reflect being dropped.
                    agent.ActionState = (byte) AgentAction.GET_BUCKET;
                    break;

                case (byte)AgentAction.IDLE:
                    break;

                default:
                    Debug.Assert(false, "ScooperBot entered unsupported state");
                    break;
            }

            // update any carried buckets.
            if (agent.CarriedEntity != Entity.Null)
            {
                ecb1.SetComponent<Translation>(entityInQueryIndex, agent.CarriedEntity, new Translation { Value = new float3(t.Value.x, t.Value.y + 0.5f, t.Value.z)} );
            }

        }).ScheduleParallel();
//        m_endSimECB.AddJobHandleForProducer(scooperECBJobHandle);


        // full bucket passer updates
        Entities
            .WithReadOnly(bucketIsEmptyAndOnGround)
            .WithStructuralChanges() // To create WaterDrop entity
            .ForEach((Entity e, int entityInQueryIndex, ref Translation t, ref SeekPosition seekComponent, ref Agent agent, in AgentTags.FullBucketPasserTag agentTag) =>
            {
                // If the agent carry something pass it to the next agent
                if (agent.CarriedEntity != Entity.Null)
                {
                    if (agent.NextAgent == Entity.Null) // last agent of the line
                    {
                        var bucketEntity = agent.CarriedEntity;

                        // drop it into the fire
                        EntityManager.SetComponentData(bucketEntity, new CarryableObject { CarryingEntity = Entity.Null} );
                        EntityManager.SetComponentData(bucketEntity, new Intensity() { Value = 0 } );

                        // Create a water drop AOE
                        var waterDrop = EntityManager.CreateEntity(typeof(WaterDrop));
                        EntityManager.SetComponentData(waterDrop, new WaterDrop() { X = (int)t.Value.x, Z = (int)t.Value.z, Range = 2, Strength = 80} );

                        agent.CarriedEntity = Entity.Null;
                        agent.ActionState = (byte) AgentAction.IDLE;
                    }
                    else
                    {
                        // Goto next agent and pass the bucket when near it
                        agent.ActionState = (byte) AgentAction.PASS_BUCKET;
                        var targetAgentPosition = EntityManager.GetComponentData<Translation>(agent.NextAgent);
                        seekComponent.TargetPos = targetAgentPosition.Value;

                        if (math.lengthsq(seekComponent.TargetPos - t.Value) < arrivalThresholdSq)
                        {
                            var targetAgent = EntityManager.GetComponentData<Agent>(agent.NextAgent);

                            EntityManager.SetComponentData(agent.CarriedEntity, new CarryableObject { CarryingEntity = agent.NextAgent} );

                            targetAgent.CarriedEntity = agent.CarriedEntity;
                            EntityManager.SetComponentData(agent.NextAgent, targetAgent);

                            agent.CarriedEntity = Entity.Null;


                            // TMP
                            EntityManager.SetComponentData(targetAgent.CarriedEntity, new Translation() { Value = targetAgentPosition.Value} );
                        }
                    }
                }
                else
                {
                    if (agent.PreviousAgent == Entity.Null) // first agent of the line try to pick full buckets
                    {
                        Entity bucketEntity = Entity.Null;
                        int bucketEntityIndex;
                        FindNearestIndex(t.Value, bucketLocations, bucketIsFullAndOnGround, true, out bucketEntityIndex); // look for nearest full bucket

                        // check that the bucket is near
                        if (math.lengthsq((bucketLocations[bucketEntityIndex] - t.Value)) < arrivalThresholdSq)
                        {
                            bucketEntity = bucketEntities[bucketEntityIndex];

                            var alreadyCarried = EntityManager.GetComponentData<CarryableObject>(bucketEntity).CarryingEntity != Entity.Null;
                            if (!alreadyCarried)
                            {
                                agent.CarriedEntity = bucketEntity;
                                EntityManager.SetComponentData(bucketEntity, new CarryableObject { CarryingEntity = e} );
                                agent.ActionState = (byte) AgentAction.PASS_BUCKET;
                            }
                        }
                        else
                        {
                            // Stay in the line
                            agent.ActionState = (byte) AgentAction.IDLE;
                        }
                    }
                    else
                    {
                        // Stay in the line
                        agent.ActionState = (byte) AgentAction.IDLE;
                    }
                }
            }).Run();

        
         // Empty bucket passer updates
         Entities
            .WithReadOnly(bucketIsEmptyAndOnGround)
            .WithoutBurst()
            .ForEach((Entity e, int entityInQueryIndex, ref Translation t, ref SeekPosition seekComponent, ref Agent agent, in AgentTags.EmptyBucketPasserTag agentTag) =>
            {
                // If the agent carry something pass it to the previous agent
                if (agent.CarriedEntity != Entity.Null)
                {
                    if (agent.PreviousAgent == Entity.Null) // last agent of the line
                    {
                        var bucketEntity = agent.CarriedEntity;

                        // drop it in on the ground for the thrower
                        EntityManager.SetComponentData(bucketEntity, new CarryableObject { CarryingEntity = Entity.Null} );

                        agent.CarriedEntity = Entity.Null;
                        agent.ActionState = (byte) AgentAction.IDLE;
                    }
                    else
                    {
                        // Goto previous agent and pass the bucket when near it
                        agent.ActionState = (byte) AgentAction.PASS_BUCKET;
                        var targetAgentPosition = EntityManager.GetComponentData<Translation>(agent.PreviousAgent);
                        seekComponent.TargetPos = targetAgentPosition.Value;

                        if (math.lengthsq(seekComponent.TargetPos - t.Value) < arrivalThresholdSq)
                        {
                            var targetAgent = EntityManager.GetComponentData<Agent>(agent.PreviousAgent);

                            EntityManager.SetComponentData(agent.CarriedEntity, new CarryableObject { CarryingEntity = agent.PreviousAgent} );

                            targetAgent.CarriedEntity = agent.CarriedEntity;
                            EntityManager.SetComponentData(agent.PreviousAgent, targetAgent);

                            agent.CarriedEntity = Entity.Null;


                            // TMP
                            EntityManager.SetComponentData(targetAgent.CarriedEntity, new Translation() { Value = targetAgentPosition.Value} );
                        }
                    }
                }
                else
                {
                    if (agent.NextAgent == Entity.Null) // first agent of the line try to pick empty buckets
                    {
                        Entity bucketEntity = Entity.Null;
                        int bucketEntityIndex;
                        FindNearestIndex(t.Value, bucketLocations, bucketIsEmptyAndOnGround, true, out bucketEntityIndex); // look for nearest empty bucket

                        // check that the bucket is near
                        if (math.lengthsq((bucketLocations[bucketEntityIndex] - t.Value)) < arrivalThresholdSq)
                        {
                            bucketEntity = bucketEntities[bucketEntityIndex];

                            var alreadyCarried = EntityManager.GetComponentData<CarryableObject>(bucketEntity).CarryingEntity != Entity.Null;
                            if (!alreadyCarried)
                            {
                                agent.CarriedEntity = bucketEntity;
                                EntityManager.SetComponentData(bucketEntity, new CarryableObject { CarryingEntity = e} );
                                agent.ActionState = (byte) AgentAction.PASS_BUCKET;
                            }
                        }
                        else
                        {
                            // Stay in the line
                            agent.ActionState = (byte) AgentAction.IDLE;
                        }
                    }
                    else
                    {
                        // Stay in the line
                        agent.ActionState = (byte) AgentAction.IDLE;
                    }
                }
            }).Run();


        m_endSimECB.AddJobHandleForProducer(Dependency);

        //JobHandle.CombineDependencies(Dependency, scooperECBJobHandle);
        //JobHandle.CombineDependencies(Dependency, throwerECBJobHandle);
        //JobHandle.CombineDependencies(Dependency, fullBucketECBJobHandle);
        //JobHandle.CombineDependencies(Dependency, emptyBucketECBJobHandle);

        // wait for jobs to finish before disposing array data
        //Dependency.Complete();

        waterEntities.Dispose(Dependency);
        waterLocations.Dispose(Dependency);
        waterVolumes.Dispose(Dependency);
        waterIsAvailable.Dispose(Dependency);
        bucketEntities.Dispose(Dependency);
        bucketIsEmptyAndOnGround.Dispose(Dependency);
        bucketLocations.Dispose(Dependency);
        bucketFillValue.Dispose(Dependency);
        bucketIsFullAndOnGround.Dispose(Dependency);
        teamEntity.Dispose(Dependency);
        teamLineStartLocation.Dispose(Dependency);
    }

    static bool FindNearestIndex(float3 currentPos, NativeArray<float3> objectLocation, NativeArray<bool> objectFilter, bool filterMatch, out int objectIndex)
    {
        float nearestDistanceSquared = float.MaxValue;
        int nearestIndex = 0;
        bool foundIndex = false;
        for (int i = 0; i < objectLocation.Length; ++i)
        {
            if (objectFilter[i] == filterMatch)
            {
                float squareLen = math.lengthsq(currentPos - objectLocation[i]);

                if (squareLen < nearestDistanceSquared /*&& squareLen > 5.0f*/)
                {
                    nearestDistanceSquared = squareLen;
                    nearestIndex = i;
                    foundIndex = true;
                }
            }
        }

        objectIndex = nearestIndex;
        return foundIndex;
    }

    static void FindNearestAndSetSeekTarget(float3 currentPos, NativeArray<float3> objectLocation, NativeArray<bool> objectFilter, bool filterMatch, ref SeekPosition seekComponent)
    {
        float nearestDistanceSquared = float.MaxValue;
        int nearestIndex = 0;
        bool foundIndex = false;
        for (int i = 0; i < objectLocation.Length; ++i)
        {
            if (objectFilter[i] == filterMatch)
            {
                float squareLen = math.lengthsq(currentPos - objectLocation[i]);

                if (squareLen < nearestDistanceSquared /*&& squareLen > 5.0f*/)
                {
                    nearestDistanceSquared = squareLen;
                    nearestIndex = i;
                    foundIndex = true;
                }
            }
        }

        if (foundIndex)
        {
            float3 loc = objectLocation[nearestIndex];
            seekComponent.TargetPos = new float3(loc.x, loc.y, loc.z);
        }
    }
}

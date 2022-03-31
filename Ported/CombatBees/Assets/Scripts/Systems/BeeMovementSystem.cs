using Components;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Mathf = UnityEngine.Mathf;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(ParticleSystemFixed))]
[UpdateAfter(typeof(Systems.TargetSystem))]
public partial class BeeMovementSystemFixed : SystemBase
{
    static readonly float flightJitter = 200f;
    static readonly float damping = 0.1f;
    static readonly float speedStretch = 0.2f;
    static readonly float teamAttraction = 5f;
    static readonly float teamRepulsion = 4f;

    static readonly float chaseForce = 50f;
    static readonly float attackDistance = 4f;
    static readonly float attackForce = 500f;
    static readonly float hitDistance = 0.5f;
    static readonly float grabDistance = 0.5f;

    EntityQuery[] teamTargets;
    EntityCommandBufferSystem beginSimulationEntityCommandBufferSystem;
    EntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        teamTargets = new EntityQuery[2]
        {
            EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<TeamShared>()),
            EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TeamShared>())
        };
        teamTargets[0].SetSharedComponentFilter(new TeamShared { TeamId = 0 });
        teamTargets[1].SetSharedComponentFilter(new TeamShared { TeamId = 1 });

        beginSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var particles = GetSingleton<ParticleSettings>();
        var deltaTime = Time.DeltaTime;


        var team0 = teamTargets[0].ToComponentDataArray<Translation>(Allocator.TempJob);
        var team1 = teamTargets[1].ToComponentDataArray<Translation>(Allocator.TempJob);

        var beginFrameEcb = beginSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var endFrameEcb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        var gsv = GlobalSystemVersion;

        // Run attraction/repulsion gather. Uses read access of Translation from input arrays and only writes attraction data.
        // Could we run this on a lower cadence?
        var attractionJob = Entities
            .WithReadOnly(team0)
            .WithDisposeOnCompletion(team0)
            .WithReadOnly(team1)
            .WithDisposeOnCompletion(team1)
            .ForEach((Entity entity, ref AttractionRepulsion attractionRepulsion, in Team team) =>
            {
                var random = Random.CreateFromIndex(gsv ^ (uint)entity.Index);

                if (team.TeamId == 0)
                {
                    if (team0.Length > 0)
                    {
                        attractionRepulsion.AttractionPos = team0[random.NextInt(team0.Length)].Value;
                        attractionRepulsion.RepulsionPos = team0[random.NextInt(team0.Length)].Value;
                    }
                }
                else if (team1.Length > 0)
                {
                    attractionRepulsion.AttractionPos = team1[random.NextInt(team1.Length)].Value;
                    attractionRepulsion.RepulsionPos = team1[random.NextInt(team1.Length)].Value;
                }
            }).ScheduleParallel(Dependency);

        var translateJob = Entities
            .ForEach((ref Translation translation, ref BeeMovement movement, in TargetType targetType) =>
            {

                translation.Value += movement.Velocity * deltaTime;
                UpdateBorders(ref movement.Velocity, ref translation.Value, targetType.Value == TargetType.Type.Goal);

            }).ScheduleParallel(Dependency);

        Dependency = Unity.Jobs.JobHandle.CombineDependencies(attractionJob, translateJob);

        Dependency = Entities
            .WithNone<BeeLifetime>()
            .ForEach((Entity entity,
                int entityInQueryIndex,
                ref BeeMovement bee,
                ref TargetType targetType,
                in Translation translation,
                in AttractionRepulsion attraction,
                in TargetEntity targetEntity,
                in Team team) =>
            {
                var random = Random.CreateFromIndex(gsv ^ (uint)entity.Index);

                var velocity = bee.Velocity;
                var position = translation.Value;
                UpdateJitterAndTeamVelocity(ref random, ref velocity, in position, in attraction, deltaTime);

                bee.IsAttacking = 0;
          
                if (targetType.Value == TargetType.Type.Enemy)
                {
                    if (!HasComponent<Team>(targetEntity.Value))
                    {
                        targetType.Value = TargetType.Type.None;
                    }
                    else
                    {
                        var delta = targetEntity.Position - position;
                        float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                        if (sqrDist > attackDistance * attackDistance)
                        {
                            velocity += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
                        }
                        else
                        {
                            velocity += delta * (attackForce * deltaTime / Mathf.Sqrt(sqrDist));
                            bee.IsAttacking = 1;
                            if (sqrDist < hitDistance * hitDistance)
                            {
                                ParticleSystem.SpawnParticle(beginFrameEcb, entityInQueryIndex, particles.Particle, ref random,
                                    targetEntity.Position, ParticleComponent.ParticleType.Blood, bee.Velocity * .35f, 2f, 6);

                                endFrameEcb.RemoveComponent<Attackable>(entityInQueryIndex, entity);
                                endFrameEcb.AddComponent<BeeLifetime>(entityInQueryIndex, entity, new BeeLifetime
                                {
                                    Value = 1f
                                });
                                targetType.Value = TargetType.Type.None;
                            }
                        }
                    }
                }
                else if (targetType.Value == TargetType.Type.Resource)
                {
                    if (!HasComponent<Components.Resource>(targetEntity.Value)
                    || (HasComponent<ResourceOwner>(targetEntity.Value)
                        && GetComponent<ResourceOwner>(targetEntity.Value).Owner != entity))
                    {
                        targetType.Value = TargetType.Type.None;
                    }
                    else
                    {
                        var delta = targetEntity.Position - position;
                        float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                        if (sqrDist > grabDistance * grabDistance)
                        {
                            velocity += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
                        }
                        else
                        {
                            endFrameEcb.AddComponent<ResourceOwner>(entityInQueryIndex, targetEntity.Value,
                                new ResourceOwner() { Owner = entity });
                            endFrameEcb.SetComponent<Components.Resource>(entityInQueryIndex, targetEntity.Value,
                                new Components.Resource() { OwnerPosition = position - new float3(0, PlayField.resourceHeight, 0) });
                            targetType.Value = TargetType.Type.Goal;
                        }
                    }
                }
                else if (targetType.Value == TargetType.Type.Goal)
                {
                    if (!HasComponent<Components.Resource>(targetEntity.Value))
                    {
                        targetType.Value = TargetType.Type.None;
                    }
                    else
                    {
                        var delta = new float3(-PlayField.size.x * .45f + PlayField.size.x * .9f * team.TeamId, 0f,
                            position.z) - position;
                        float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                        if (sqrDist > grabDistance * grabDistance)
                        {
                            velocity += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
                        }
                        else
                        {
                            targetType.Value = TargetType.Type.None;

                            endFrameEcb.RemoveComponent<ResourceOwner>(entityInQueryIndex, targetEntity.Value);
                            endFrameEcb.AddComponent<KinematicBody>(entityInQueryIndex, targetEntity.Value,
                                new KinematicBody() { landPosition = -PlayField.size.y * 0.5f });
                        }
                    }
                }

                bee.Velocity = velocity;

            }).ScheduleParallel(Dependency);


        Dependency = Entities
           .ForEach((ref MovementSmoothing smoothing, in Translation translation, in BeeMovement movement) =>
           {
               // only used for smooth rotation:
               float3 oldSmoothPos = smoothing.SmoothPosition;
               if (movement.IsAttacking == 0)
               {
                   smoothing.SmoothPosition = math.lerp(smoothing.SmoothPosition, translation.Value, deltaTime * /*rotationStiffness*/5);
               }
               else
               {
                   smoothing.SmoothPosition = translation.Value;
               }
               smoothing.SmoothDirection = smoothing.SmoothPosition - oldSmoothPos;

           }).ScheduleParallel(Dependency);


        Dependency = Entities
            .ForEach((Entity entity,
            int entityInQueryIndex,
             ref BeeLifetime life,
             ref BeeMovement movement,
            ref Translation translation) =>
            {
                var random = Random.CreateFromIndex(gsv ^ (uint)entity.Index);

                if (random.NextFloat(1f) < (life.Value - .5f) * .5f)
                {
                    ParticleSystem.SpawnParticle(beginFrameEcb, entityInQueryIndex, particles.Particle, ref random, translation.Value, ParticleComponent.ParticleType.Blood, float3.zero);
                }

                movement.Velocity.y += PlayField.gravity * deltaTime;
                translation.Value += movement.Velocity * deltaTime;

                life.Value -= deltaTime / 10f;
                if (life.Value < 0f)
                {
                    beginFrameEcb.DestroyEntity(entityInQueryIndex, entity);
                }
            }).ScheduleParallel(Dependency);

        beginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }

    private static void UpdateJitterAndTeamVelocity(ref Random random, ref float3 velocity, in float3 position,
        in AttractionRepulsion attraction, float deltaTime)
    {
        velocity += random.NextFloat3Direction() * (flightJitter * deltaTime);
        velocity *= 1f - damping;

        var delta = attraction.AttractionPos - position;
        float dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
        if (dist > 0f)
        {
            velocity += delta * (teamAttraction * deltaTime / dist);
        }

        delta = attraction.RepulsionPos - position;
        dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
        if (dist > 0f)
        {
            velocity -= delta * (teamRepulsion * deltaTime / dist);
        }
    }

    private static void UpdateBorders(ref float3 velocity, ref float3 position, bool isHoldingResource)
    {
        if (Mathf.Abs(position.x) > PlayField.size.x * .5f)
        {
            position.x = PlayField.size.x * .5f * Mathf.Sign(position.x);
            velocity.x *= -0.5f;
            velocity.y *= .8f;
            velocity.z *= .8f;
        }

        if (Mathf.Abs(position.z) > PlayField.size.z * .5f)
        {
            position.z = PlayField.size.z * .5f * Mathf.Sign(position.z);
            velocity.z *= -0.5f;
            velocity.x *= .8f;
            velocity.y *= .8f;
        }
        float resMod = isHoldingResource ? PlayField.resourceHeight : 0;
        if (Mathf.Abs(position.y) > PlayField.size.y * .5f - resMod)
        {
            position.y = (PlayField.size.y * .5f - resMod) * Mathf.Sign(position.y);
            velocity.y *= -0.5f;
            velocity.z *= .8f;
            velocity.x *= .8f;
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(ParticleSystemFixed))]
[UpdateAfter(typeof(Systems.TargetSystem))]
public partial class BeeMovementSystem : SystemBase
{
    static readonly float speedStretch = 0.2f;

    protected override void OnUpdate()
    {
        Dependency = Entities
            .WithNone<BeeLifetime>()
            .ForEach((ref NonUniformScale scale, ref Rotation rotation, in BeeMovement movement, in MovementSmoothing smoothing) =>
            {
                var velocity = movement.Velocity;

                var size = movement.Size;
                var scl = new float3(size, size, size);
                float stretch = Mathf.Max(1f, math.length(velocity) * speedStretch);
                scl.z *= stretch;
                scl.x /= (stretch - 1f) / 5f + 1f;
                scl.y /= (stretch - 1f) / 5f + 1f;
                scale.Value = scl;

                if (!smoothing.SmoothDirection.Equals(float3.zero))
                {
                    rotation.Value = quaternion.LookRotation(smoothing.SmoothDirection, new float3(0, 1, 0));
                }

            }).ScheduleParallel(Dependency);

        Dependency = Entities
            .ForEach((ref NonUniformScale scale, ref Rotation rotation, in BeeLifetime life, in BeeMovement movement, in MovementSmoothing smoothing) =>
            {
                var velocity = movement.Velocity;

                var size = movement.Size;
                var scl = new float3(size, size, size);
                scale.Value = scl * Mathf.Sqrt(life.Value);                

                if (!smoothing.SmoothDirection.Equals(float3.zero))
                {
                    rotation.Value = quaternion.LookRotation(smoothing.SmoothDirection, new float3(0, 1, 0));
                }

            }).ScheduleParallel(Dependency);
    }
}
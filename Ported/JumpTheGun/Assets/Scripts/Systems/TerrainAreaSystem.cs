using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Color = UnityEngine.Color;

public partial class TerrainAreaSystem : SystemBase
{
    EntityQuery checkForBricks;
    protected override void OnCreate()
    {
        checkForBricks = GetEntityQuery(typeof (Brick));
    }
    protected override void OnUpdate()
    {
        if (checkForBricks.IsEmpty)
        {
            CreateBoxes();
        }
    }

    public void CreateBoxes(){
        ClearBoxes();
        // Create query for EntityPrefabHolder
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // The PRNG (pseudorandom number generator) from Unity.Mathematics is a struct
        // and can be used in jobs. For simplicity and debuggability in development,
        // we'll initialize it with a constant. (In release, we'd want a seed that
        // randomly varies, such as the time from the user's system clock.)
        var random = new Random((uint) System.DateTime.Now.Ticks);

        Entities
            .ForEach((Entity entity, in EntityPrefabHolder prefabHolder, in TerrainData terrainData) =>
            {
                // TODO : Experiment with it
                //ecb.DestroyEntity(entity);

                // Create a grid entity
                Entity gridEntity = ecb.CreateEntity();
                DynamicBuffer<OccupiedElement> occupiedGrid = ecb.AddBuffer<OccupiedElement>(gridEntity);
                DynamicBuffer<EntityElement> brickGrid = ecb.AddBuffer<EntityElement>(gridEntity);
                occupiedGrid.EnsureCapacity(terrainData.TerrainWidth * terrainData.TerrainLength);
                brickGrid.EnsureCapacity(terrainData.TerrainWidth * terrainData.TerrainLength);

                for (int j = 0; j < terrainData.TerrainLength; ++j)
                {
                    for (int i = 0; i < terrainData.TerrainWidth; ++i)
                    {
                        var instance = ecb.Instantiate(prefabHolder.BrickEntityPrefab);
                        // Set scale
                        float height = random.NextFloat(terrainData.MinTerrainHeight, terrainData.MaxTerrainHeight);
                        float3 scale = new float3(1, height, 1);
                        // Set position
                        float3 pos = new float3(i * Constants.SPACING, height / 2 + Constants.Y_OFFSET, j * Constants.SPACING);
                        ecb.SetComponent(instance, new Translation
                        {
                            Value = pos
                        });
                        ecb.SetComponent(instance, new NonUniformScale
                        {
                            Value = scale
                        });
                        ecb.SetComponent(instance, new Brick
                        {
                            height = height
                        });

                        occupiedGrid.Add(false);
                        brickGrid.Add(instance);
                    }
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    public void ClearBoxes()
    {
        // Create query for all the bricks
        // Destroy
    }
}
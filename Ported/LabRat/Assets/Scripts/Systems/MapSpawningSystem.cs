
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public partial class MapSpawningSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var configEntity = GetSingletonEntity<Config>();
        var config = GetComponent<Config>(configEntity);
        var random = new Unity.Mathematics.Random(config.MapSeed);

        var playersQuery = GetEntityQuery(typeof(Player));
        var players = playersQuery.ToEntityArray(Allocator.TempJob);
        

        Entities
            .ForEach((Entity entity, in MapSpawner spawner) =>
            {
                ecb.DestroyEntity(entity);
                
                // warm up the random generator
                for (int i = 0; i < 1000; i++)
                    random.NextFloat();
                
                for (int y = 0; y < spawner.MapHeight; ++y)
                {
                    for (int x = 0; x < spawner.MapWidth; ++x)
                    {
                        var tile = ecb.Instantiate(spawner.TilePrefab);
                        ecb.SetComponent(tile, new Translation
                        {
                            Value = new float3(x, -0.5f, y)
                        });
                        var walls = (y == 0 ? DirectionEnum.North : spawner.WallFrequency > random.NextFloat()? DirectionEnum.North : DirectionEnum.None) |
                                    (y == spawner.MapHeight - 1 ? DirectionEnum.South : spawner.WallFrequency > random.NextFloat()? DirectionEnum.South : DirectionEnum.None) |
                                    (x == 0 ? DirectionEnum.West : spawner.WallFrequency > random.NextFloat()? DirectionEnum.West : DirectionEnum.None) |
                                    (x == spawner.MapWidth - 1 ? DirectionEnum.East : spawner.WallFrequency > random.NextFloat()? DirectionEnum.West : DirectionEnum.None);
                        
                        ecb.SetComponent(tile, new Tile
                        {
                            Coords = new int2(x, y)
                        });
                        ecb.SetComponent(tile, new Direction {
                            Value = walls
                        });
                        ecb.SetComponent(tile, new URPMaterialPropertyBaseColor
                        {
                            Value = (x & 1) == (y & 1)? spawner.TileEvenColor : spawner.TileOddColor
                        });

                        if ((walls & DirectionEnum.North) == DirectionEnum.North)
                        {
                            var wall = ecb.Instantiate(spawner.WallPrefab);
                            ecb.SetComponent(wall, new Translation
                            {
                                Value = new float3(x, 0, y-0.5f)
                            });
                            ecb.SetComponent(wall, new Rotation
                            {
                                Value = quaternion.Euler(0, math.PI * 0.5f, 0)
                            });
                        }
                        
                        if ((walls & DirectionEnum.South) == DirectionEnum.South)
                        {
                            var wall = ecb.Instantiate(spawner.WallPrefab);
                            ecb.SetComponent(wall, new Translation
                            {
                                Value = new float3(x, 0, y+0.5f)
                            });
                            ecb.SetComponent(wall, new Rotation
                            {
                                Value = quaternion.Euler(0, math.PI * 0.5f, 0)
                            });
                        }

                        if ((walls & DirectionEnum.West) == DirectionEnum.West)
                        {
                            var wall = ecb.Instantiate(spawner.WallPrefab);
                            ecb.SetComponent(wall, new Translation
                            {
                                Value = new float3(x - 0.5f, 0, y)
                            });
                        }

                        if ((walls & DirectionEnum.East) == DirectionEnum.East)
                        {
                            var wall = ecb.Instantiate(spawner.WallPrefab);
                            ecb.SetComponent(wall, new Translation
                            {
                                Value = new float3(x + 0.5f, 0, y)
                            });
                        }
                    }
                }

                foreach (var playerEntity in players)
                {
                    var player = GetComponent<Player>(playerEntity);
                    var exit = ecb.Instantiate(config.ExitPrefab);
                    var coords = random.NextInt2(int2.zero, new int2(spawner.MapWidth, spawner.MapHeight));
                    ecb.SetComponent(exit, new Translation
                    {
                        Value = new float3(coords.x, 0, coords.y)
                    });
                    ecb.SetComponent(exit, new Tile
                    {
                        Coords = coords
                    });
                    ecb.SetComponent(exit, new PlayerOwned
                    {
                        Owner = playerEntity
                    });
                    ecb.SetComponent(exit, new URPMaterialPropertyBaseColor
                    {
                        Value = player.Color
                    });
                }
            }).Run();
        
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

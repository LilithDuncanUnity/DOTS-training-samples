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

public class ArrowSpawnerSystem : SystemBase
{
    EntityCommandBufferSystem m_EcbSystem;
    protected override void OnCreate()
    {
        m_EcbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var gameConfig = GetSingleton<GameConfig>();
        var translationData = GetComponentDataFromEntity<Translation>();

        var ecb = m_EcbSystem.CreateCommandBuffer();
        var cellArray = World.GetExistingSystem<BoardSpawner>().cells;
        
        
        Entities.ForEach((in PlayerInput playerInput) => {

            if (playerInput.isMouseDown && playerInput.TileIndex < cellArray.Length && playerInput.TileIndex >= 0)
            {
                Entity cellEntity = cellArray[playerInput.TileIndex];
                var cellTranslation = translationData[cellEntity];

                Entity arrowEntity = ecb.Instantiate(gameConfig.ArrowPrefab);
                
                ecb.SetComponent(arrowEntity, cellTranslation);

                Rotation rotation = new Rotation();
                Cardinals direction = playerInput.ArrowDirection;
                ecb.AddComponent(arrowEntity, new Direction(direction));
                ecb.AddComponent(arrowEntity, new Arrow());
                switch (direction)
                {
                    default:
                    case Cardinals.North:
                        rotation.Value = quaternion.RotateY(math.radians(180));
                        break;
                    case Cardinals.South:
                        break;
                    case Cardinals.East:
                        rotation.Value = quaternion.RotateY(math.radians(270));
                        break;
                    case Cardinals.West:
                        rotation.Value = quaternion.RotateY(math.radians(90));
                        break;
                }
                
                ecb.SetComponent(arrowEntity, rotation);

            }
        }).Schedule();
        
        m_EcbSystem.AddJobHandleForProducer(Dependency);
    }
}
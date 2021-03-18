﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
public class WaterAuthoring : MonoBehaviour
    ,IConvertGameObjectToEntity
{
    private static readonly float4 WATER_COLOUR = new float4(0.0f, 0.62122846f, 1.0f, 1.0f);
    public void Convert(Entity entity, EntityManager dstManager
        , GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<River>(entity);
        dstManager.AddComponentData(entity, new URPMaterialPropertyBaseColor()
        {
            Value = WATER_COLOUR
        });
        dstManager.AddComponentData(entity, new Volume()
        {
            Value = 1.0f
        });  
    }
}
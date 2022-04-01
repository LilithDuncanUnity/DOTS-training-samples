using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public static class GridUtility
{
    public static int GetTileIndex(int x, int z, int width)
    {
        if (x < 0 || z < 0) return -1;
        if (x >= width || z >= width) return -1;
        
        return (z * width) + x;
    }
    
    public static int2 GetTileCoordinate(int index , int width)
    {
        //if (index < 0 || index >= width * width) return -1;
        
        int x = index % width;
        int z = index / width;
        return new int2(x, z);
    }
    
    public static int2 PlotTileCoordFromWorldPosition(float3 worldPosition, int gridSideWidth)
    {
        float offset = (gridSideWidth - 1) * 0.5f;

        int2 coord;
        coord.x =(int) math.remap(-offset, offset, 0f, gridSideWidth - 1, worldPosition.x);
        coord.y =(int) math.remap(-offset, offset, 0f, gridSideWidth - 1, worldPosition.z);

        UnityEngine.Debug.Log($"world position: {worldPosition} | tile coordinate: {coord}");
        return coord;
    }
    
    public static float2 PlotTileWorldPosition2DFromIndex(int tileIndex, int gridSideWidth)
    {
        float offset = (gridSideWidth - 1) * 0.5f;

        int2 tileCoord = GetTileCoordinate(tileIndex,gridSideWidth);
            
        return new float2(tileCoord.x - offset, tileCoord.y - offset);
    }
    
    public static float3 PlotTileWorldPositionFromIndex(int tileIndex, int gridSideWidth)
    {
        float offset = (gridSideWidth - 1) * 0.5f;

        int2 tileCoord = GetTileCoordinate(tileIndex,gridSideWidth);
            
        return new float3(tileCoord.x, 0f, tileCoord.y) - offset;
    }
    
    public static void CreateAdjacentTileArray(ref NativeArray<int2> array, int radius)
    {
        //radius 1 -> 8  : 3*3-1
        //radius 2 -> 24 : 5*5-1 : (r+r+1)^2-1
        
        int diameter = (2 * radius + 1);
        int arrayLength = diameter * diameter - 1;
        
        array = new NativeArray<int2>(arrayLength, Allocator.Persistent);

        int i = 0;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                
                array[i] = new int2(x,y);
                i++;
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct CellMapHelper
{
    float2 worldLowerLeft;
    float2 worldUpperRight;

    DynamicBuffer<CellMap> cellmap;
    int gridSize;

    public CellMapHelper(DynamicBuffer<CellMap> _cellmap, int _gridSize, float _worldSize)
    {
        cellmap = _cellmap;
        gridSize = _gridSize;

        // World centered at 0,0
        var halfSize = _worldSize / 2;
        worldLowerLeft = new float2(-halfSize, -halfSize);
        worldUpperRight = new float2(halfSize, halfSize);
    }

    public void InitCellMap()
    {
        cellmap.Length = gridSize * gridSize;
    }

    public void InitBorders()
    {
        for(int i = 0; i < gridSize; ++i)
        {
            Set(i, 0, CellState.IsObstacle);
            Set(i, gridSize -1, CellState.IsObstacle);
            Set(0, i, CellState.IsObstacle);
            Set(gridSize -1, i, CellState.IsObstacle);
        }
    }

    /// <summary>
    /// Returns the nearest index to a point in world space. The originOffset is used
    /// to align the [0,0] index with the start of the grid (since the grid origin in
    /// worldspace might be [-5.0, -5.0] for example if the plane of size 5 is at [0,0])
    /// </summary>
    /// <param name="xy"></param>
    /// <param name="originOffset"></param>
    /// <returns></returns>
    public int GetNearestIndex(float2 xy)
    {
        if(xy.x < worldLowerLeft.x || xy.y < worldLowerLeft.y || xy.x > worldUpperRight.x || xy.y > worldUpperRight.y)
        {
            //TBD: Warnings are disabled currently because ant move might jump out past the boundary cell
            //Debug.LogError("[Cell Map] Trying to get index out of range");
            return -1;
        }

        float2 gridRelativeXY = xy - worldLowerLeft;

        float2 gridIndexScaleFactor = (worldUpperRight - worldLowerLeft) / gridSize;
        float2 gridIndexXY = gridRelativeXY/gridIndexScaleFactor;

        return ConvertGridIndex2Dto1D(gridIndexXY);
    }

    public CellState GetCellStateFrom2DPos(float2 xy)
    {
        int cellIndex = GetNearestIndex(xy);
        if (cellIndex < 0 || cellIndex > cellmap.Length)
        {
            //Debug.LogError(string.Format("[Cell Map] Position is outside cell map {0}, {1}", xy.x, xy.y));
            return CellState.IsObstacle;
        }

        return cellmap[cellIndex].state;
    }

    public int ConvertGridIndex2Dto1D(float2 index)
        => Mathf.RoundToInt(index.y) * gridSize + Mathf.RoundToInt(index.x);

    public void Set(int x, int y, CellState state)
    {
        cellmap.ElementAt(y * gridSize + x).state = state;
    }

    public CellState Get(int x, int y)
    {
        return cellmap[y * gridSize + x].state;
    }
}
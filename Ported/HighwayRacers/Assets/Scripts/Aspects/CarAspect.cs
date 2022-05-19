using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public readonly partial struct CarAspect : IAspect<CarAspect>
{
    private readonly RefRO<CarSpeed> m_Speed;
    private readonly RefRW<CarPosition> m_Position;
    private readonly RefRW<CarAICache> m_Peers;
    private readonly RefRO<CarProperties> m_Properties;
    private readonly RefRO<CarPreview> m_Preview;
    private readonly RefRW<CarChangingLanes> m_ChangingLanes;
    public readonly Entity Entity;
    public int Lane
    {
        get => m_Position.ValueRO.currentLane;
        set => m_Position.ValueRW.currentLane = value;
    }

    public float CurrentSpeed => m_Speed.ValueRO.currentSpeed;
    public float DesiredSpeed => m_Properties.ValueRO.desiredSpeed;

    public float Distance => m_Position.ValueRO.distance;

    public bool Preview => m_Preview.ValueRO.Preview;
    public bool SecondaryPreview => m_Preview.ValueRO.SecondaryPreview;

    public float MergeSpace => m_Properties.ValueRO.mergeSpace;

    public Entity CarInFront
    {
        get => m_Peers.ValueRO.CarInFront;
        set => m_Peers.ValueRW.CarInFront = value;
    }

    public bool CanMergeLeft
    {
        get => m_Peers.ValueRO.CanMergeLeft;
        set => m_Peers.ValueRW.CanMergeLeft = value;
    }
    public bool CanMergeRight
    {
        get => m_Peers.ValueRO.CanMergeRight;
        set => m_Peers.ValueRW.CanMergeRight = value;
    }
    public float LeftMergeDistance => m_Properties.ValueRO.leftMergeDistance;
    public float OvertakeEagerness => m_Properties.ValueRO.overTakeEagerness;
    public float DefaultSpeed => m_Properties.ValueRO.defaultSpeed;

    public float DistanceAhead => m_Peers.ValueRO.DistanceAhead;

    public bool IsMerging => m_ChangingLanes.ValueRO.FromLane != m_ChangingLanes.ValueRO.ToLane;

    public float MergeProgress {
        get => m_ChangingLanes.ValueRO.Progress;
        set => m_ChangingLanes.ValueRW.Progress = value;
    }
    public int MergingFrom => m_ChangingLanes.ValueRO.FromLane;

    public void MergeLeft()
    {
        m_ChangingLanes.ValueRW.Progress = 0;
        m_ChangingLanes.ValueRW.FromLane = Lane;
        m_ChangingLanes.ValueRW.ToLane = Lane + 1;
        Lane = Lane + 1;
    }

    public void ConcludeMerge()
    {
        m_ChangingLanes.ValueRW.FromLane = m_ChangingLanes.ValueRO.ToLane;
        m_ChangingLanes.ValueRW.Progress = 0;
    }
}

public readonly partial struct CarPositionAspect : IAspect<CarPositionAspect>
{
    public readonly Entity Entity;
    private readonly RefRO<CarPosition> m_Position;

    public int Lane => m_Position.ValueRO.currentLane;
    public float Distance => m_Position.ValueRO.distance;
}

public readonly partial struct CarAICacheAspect : IAspect<CarAICacheAspect>
{
    public readonly Entity Entity;
    private readonly RefRO<CarPosition> m_Position;
    private readonly RefRW<CarAICache> m_Peers;
    private readonly RefRO<CarProperties> m_Properties;

    public int Lane => m_Position.ValueRO.currentLane;
    public float Distance => m_Position.ValueRO.distance;

    public bool CanMergeLeft
    {
        get => m_Peers.ValueRO.CanMergeLeft;
        set => m_Peers.ValueRW.CanMergeLeft = value;
    }
    public bool CanMergeRight
    {
        get => m_Peers.ValueRO.CanMergeRight;
        set => m_Peers.ValueRW.CanMergeRight = value;
    }

    public float MinDistanceInFront => m_Properties.ValueRO.minDistanceInFront;
    public float MergeSpace => m_Properties.ValueRO.mergeSpace;
}

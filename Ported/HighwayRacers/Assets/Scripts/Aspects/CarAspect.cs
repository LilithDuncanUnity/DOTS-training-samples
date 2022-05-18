using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public readonly partial struct CarAspect : IAspect<CarAspect>
{
    private readonly RefRO<CarSpeed> m_Speed;
    private readonly RefRO<CarPosition> m_Position;
    private readonly RefRW<CarPeers> m_Peers;
    private readonly RefRO<CarProperties> m_Properties;
    public readonly Entity Entity;
    public int Lane => m_Position.ValueRO.currentLane;


    public float Distance => m_Position.ValueRO.distance;

    public float DistanceToBack => m_Peers.ValueRO.DistanceToBack;

    public float DistanceToFront => m_Peers.ValueRO.DistanceToFront;

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
}

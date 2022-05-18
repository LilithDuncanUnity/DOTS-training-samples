using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public readonly partial struct CarAspect : IAspect<CarAspect>
{
    private readonly RefRO<CarSpeed> m_Speed;
    private readonly RefRO<CarPosition> m_Position;
    private readonly RefRW<CarPeers> m_Peers;
    private readonly RefRO<CarProperties> m_Properties;
    private readonly RefRO<CarPreview> m_Preview;
    private readonly RefRO<CarColor> m_Color;
    public readonly Entity Entity;
    public int Lane => m_Position.ValueRO.currentLane;

    public float CurrentSpeed => m_Speed.ValueRO.currentSpeed;
    public float DesiredSpeed => m_Properties.ValueRO.desiredSpeed;
    public Color DefaultColor => m_Color.ValueRO.defaultColor;
    public Color FastColor => m_Color.ValueRO.fastColor;
    public Color SlowColor => m_Color.ValueRO.slowColor;


    public float Distance => m_Position.ValueRO.distance;

    public bool Preview => m_Preview.ValueRO.Preview;

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

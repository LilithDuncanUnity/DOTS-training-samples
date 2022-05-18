using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public readonly partial struct CarAspect : IAspect<CarAspect>
{
    private readonly RefRO<CarSpeed> m_Speed;
    private readonly RefRO<CarPosition> m_Position;
    private readonly RefRW<CarPeers> m_Peers;
    public readonly Entity Entity;

    public float Position => m_Position.ValueRO.distance;
    public int Lane => m_Position.ValueRO.currentLane;

    public Entity CarInFront
    {
        get => m_Peers.ValueRO.CarInFront;
        set => m_Peers.ValueRW.CarInFront = value;
    }
}

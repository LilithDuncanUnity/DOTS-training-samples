using Unity.Entities;
using UnityEngine;

public readonly partial struct CarColorAspect : IAspect<CarColorAspect>
{
    private readonly RefRO<CarSpeed> m_Speed;
    private readonly RefRO<CarProperties> m_Properties;
    private readonly RefRO<CarColor> m_Color;    
    public readonly Entity Entity;

    public float CurrentSpeed => m_Speed.ValueRO.currentSpeed;
    public float DesiredSpeed => m_Properties.ValueRO.desiredSpeed;
    public Color DefaultColor => m_Color.ValueRO.defaultColor;
    public Color FastColor => m_Color.ValueRO.fastColor;
    public Color SlowColor => m_Color.ValueRO.slowColor;

}

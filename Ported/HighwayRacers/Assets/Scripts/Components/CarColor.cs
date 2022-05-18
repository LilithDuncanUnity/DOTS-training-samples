using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct CarColor : IComponentData
{
    public Color defaultColor;
    public Color fastColor;
    public Color slowColor;
}

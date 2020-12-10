﻿using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct BarSpawner : IComponentData
{
    public Entity barPrefab;
    public float4 color;
}
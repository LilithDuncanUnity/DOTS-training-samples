using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

public struct CameraConfig : IComponentData {
    public Entity MainCamera;
    public Entity CarCamera;
}

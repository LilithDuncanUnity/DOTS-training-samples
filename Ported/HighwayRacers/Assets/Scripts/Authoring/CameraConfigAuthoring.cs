using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CameraConfigAuthoring : MonoBehaviour
{
    public Camera MainCamera;
    public Camera CarCamera;
}

class MainCameraBaker : Baker<CameraConfigAuthoring>
{
    public override void Bake(CameraConfigAuthoring authoring)
    {
        AddComponent(new CameraConfig()
        {
            MainCamera = GetEntity(authoring.MainCamera),
            CarCamera = GetEntity(authoring.CarCamera)
        });
    }
}

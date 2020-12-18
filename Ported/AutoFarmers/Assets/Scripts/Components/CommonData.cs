﻿using Unity.Entities;

public struct CommonData : IComponentData
{
    public int FarmerCounter;
    public int DroneCounter;
    
    public int DroneMoney;
    public int FarmerMoney;
    
    public float MoveSmoothForDrones;
}

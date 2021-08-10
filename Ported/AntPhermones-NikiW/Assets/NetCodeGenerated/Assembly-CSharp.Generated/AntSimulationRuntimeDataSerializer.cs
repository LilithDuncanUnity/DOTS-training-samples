//THIS FILE IS AUTOGENERATED BY GHOSTCOMPILER. DON'T MODIFY OR ALTER.
using System;
using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using Unity.NetCode.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

namespace Assembly_CSharp.Generated
{
    [BurstCompile]
    public struct AntSimulationRuntimeDataGhostComponentSerializer
    {
        static GhostComponentSerializer.State GetState()
        {
            // This needs to be lazy initialized because otherwise there is a depenency on the static initialization order which breaks il2cpp builds due to TYpeManager not being initialized yet
            if (!s_StateInitialized)
            {
                s_State = new GhostComponentSerializer.State
                {
                    GhostFieldsHash = 10278782975620710441,
                    ExcludeFromComponentCollectionHash = 0,
                    ComponentType = ComponentType.ReadWrite<AntSimulationRuntimeData>(),
                    ComponentSize = UnsafeUtility.SizeOf<AntSimulationRuntimeData>(),
                    SnapshotSize = UnsafeUtility.SizeOf<Snapshot>(),
                    ChangeMaskBits = ChangeMaskBits,
                    SendMask = GhostComponentSerializer.SendMask.Interpolated | GhostComponentSerializer.SendMask.Predicted,
                    SendToOwner = SendToOwnerType.All,
                    SendForChildEntities = 1,
                    VariantHash = 0,
                    CopyToSnapshot =
                        new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(CopyToSnapshot),
                    CopyFromSnapshot =
                        new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(CopyFromSnapshot),
                    RestoreFromBackup =
                        new PortableFunctionPointer<GhostComponentSerializer.RestoreFromBackupDelegate>(RestoreFromBackup),
                    PredictDelta = new PortableFunctionPointer<GhostComponentSerializer.PredictDeltaDelegate>(PredictDelta),
                    CalculateChangeMask =
                        new PortableFunctionPointer<GhostComponentSerializer.CalculateChangeMaskDelegate>(
                            CalculateChangeMask),
                    Serialize = new PortableFunctionPointer<GhostComponentSerializer.SerializeDelegate>(Serialize),
                    Deserialize = new PortableFunctionPointer<GhostComponentSerializer.DeserializeDelegate>(Deserialize),
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    ReportPredictionErrors = new PortableFunctionPointer<GhostComponentSerializer.ReportPredictionErrorsDelegate>(ReportPredictionErrors),
                    #endif
                };
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                s_State.NumPredictionErrorNames = GetPredictionErrorNames(ref s_State.PredictionErrorNames);
                #endif
                s_StateInitialized = true;
            }
            return s_State;
        }
        private static bool s_StateInitialized;
        private static GhostComponentSerializer.State s_State;
        public static GhostComponentSerializer.State State => GetState();
        public struct Snapshot
        {
            public float colonyPos_x;
            public float colonyPos_y;
            public float foodPosition_x;
            public float foodPosition_y;
            public uint hasSpawnedAnts;
        }
        public const int ChangeMaskBits = 3;
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        public static void CopyToSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData, snapshotOffset + snapshotStride*i);
                ref var component = ref GhostComponentSerializer.TypeCast<AntSimulationRuntimeData>(componentData, componentStride*i);
                ref var serializerState = ref GhostComponentSerializer.TypeCast<GhostSerializerState>(stateData, 0);
                snapshot.colonyPos_x = component.colonyPos.x;
                snapshot.colonyPos_y = component.colonyPos.y;
                snapshot.foodPosition_x = component.foodPosition.x;
                snapshot.foodPosition_y = component.foodPosition.y;
                snapshot.hasSpawnedAnts = component.hasSpawnedAnts?1u:0;
            }
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        public static void CopyFromSnapshot(IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                var deserializerState = GhostComponentSerializer.TypeCast<GhostDeserializerState>(stateData, 0);
                ref var snapshotInterpolationData = ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(snapshotData, snapshotStride*i);
                ref var snapshotBefore = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotInterpolationData.SnapshotBefore, snapshotOffset);
                ref var snapshotAfter = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotInterpolationData.SnapshotAfter, snapshotOffset);
                //Compute the required owner mask for the components and buffers by retrievieng the ghost owner id from the data for the current tick.
                if (snapshotInterpolationData.GhostOwner > 0)
                {
                    var requiredOwnerMask = snapshotInterpolationData.GhostOwner == deserializerState.GhostOwner
                        ? SendToOwnerType.SendToOwner
                        : SendToOwnerType.SendToNonOwner;
                    if ((deserializerState.SendToOwner & requiredOwnerMask) == 0)
                        continue;
                }
                deserializerState.SnapshotTick = snapshotInterpolationData.Tick;
                float snapshotInterpolationFactorRaw = snapshotInterpolationData.InterpolationFactor;
                float snapshotInterpolationFactor = snapshotInterpolationFactorRaw;
                ref var component = ref GhostComponentSerializer.TypeCast<AntSimulationRuntimeData>(componentData, componentStride*i);
                component.colonyPos = new float2(snapshotBefore.colonyPos_x, snapshotBefore.colonyPos_y);
                component.foodPosition = new float2(snapshotBefore.foodPosition_x, snapshotBefore.foodPosition_y);
                component.hasSpawnedAnts = snapshotBefore.hasSpawnedAnts != 0;

            }
        }


        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        public static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<AntSimulationRuntimeData>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<AntSimulationRuntimeData>(backupData, 0);
            component.colonyPos.x = backup.colonyPos.x;
            component.colonyPos.y = backup.colonyPos.y;
            component.foodPosition.x = backup.foodPosition.x;
            component.foodPosition.y = backup.foodPosition.y;
            component.hasSpawnedAnts = backup.hasSpawnedAnts;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.PredictDeltaDelegate))]
        public static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline1 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline1Data);
            ref var baseline2 = ref GhostComponentSerializer.TypeCast<Snapshot>(baseline2Data);
            snapshot.hasSpawnedAnts = (uint)predictor.PredictInt((int)snapshot.hasSpawnedAnts, (int)baseline1.hasSpawnedAnts, (int)baseline2.hasSpawnedAnts);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.CalculateChangeMaskDelegate))]
        public static void CalculateChangeMask(IntPtr snapshotData, IntPtr baselineData, IntPtr bits, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask;
            changeMask = (snapshot.colonyPos_x != baseline.colonyPos_x) ? 1u : 0;
            changeMask |= (snapshot.colonyPos_y != baseline.colonyPos_y) ? (1u<<0) : 0;
            changeMask |= (snapshot.foodPosition_x != baseline.foodPosition_x) ? (1u<<1) : 0;
            changeMask |= (snapshot.foodPosition_y != baseline.foodPosition_y) ? (1u<<1) : 0;
            changeMask |= (snapshot.hasSpawnedAnts != baseline.hasSpawnedAnts) ? (1u<<2) : 0;
            GhostComponentSerializer.CopyToChangeMask(bits, changeMask, startOffset, 3);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeDelegate))]
        public static void Serialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamWriter writer, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
                writer.WritePackedFloatDelta(snapshot.colonyPos_x, baseline.colonyPos_x, compressionModel);
            if ((changeMask & (1 << 0)) != 0)
                writer.WritePackedFloatDelta(snapshot.colonyPos_y, baseline.colonyPos_y, compressionModel);
            if ((changeMask & (1 << 1)) != 0)
                writer.WritePackedFloatDelta(snapshot.foodPosition_x, baseline.foodPosition_x, compressionModel);
            if ((changeMask & (1 << 1)) != 0)
                writer.WritePackedFloatDelta(snapshot.foodPosition_y, baseline.foodPosition_y, compressionModel);
            if ((changeMask & (1 << 2)) != 0)
                writer.WritePackedUIntDelta(snapshot.hasSpawnedAnts, baseline.hasSpawnedAnts, compressionModel);
        }
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        public static void Deserialize(IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref NetworkCompressionModel compressionModel, IntPtr changeMaskData, int startOffset)
        {
            ref var snapshot = ref GhostComponentSerializer.TypeCast<Snapshot>(snapshotData);
            ref var baseline = ref GhostComponentSerializer.TypeCast<Snapshot>(baselineData);
            uint changeMask = GhostComponentSerializer.CopyFromChangeMask(changeMaskData, startOffset, ChangeMaskBits);
            if ((changeMask & (1 << 0)) != 0)
                snapshot.colonyPos_x = reader.ReadPackedFloatDelta(baseline.colonyPos_x, compressionModel);
            else
                snapshot.colonyPos_x = baseline.colonyPos_x;
            if ((changeMask & (1 << 0)) != 0)
                snapshot.colonyPos_y = reader.ReadPackedFloatDelta(baseline.colonyPos_y, compressionModel);
            else
                snapshot.colonyPos_y = baseline.colonyPos_y;
            if ((changeMask & (1 << 1)) != 0)
                snapshot.foodPosition_x = reader.ReadPackedFloatDelta(baseline.foodPosition_x, compressionModel);
            else
                snapshot.foodPosition_x = baseline.foodPosition_x;
            if ((changeMask & (1 << 1)) != 0)
                snapshot.foodPosition_y = reader.ReadPackedFloatDelta(baseline.foodPosition_y, compressionModel);
            else
                snapshot.foodPosition_y = baseline.foodPosition_y;
            if ((changeMask & (1 << 2)) != 0)
                snapshot.hasSpawnedAnts = reader.ReadPackedUIntDelta(baseline.hasSpawnedAnts, compressionModel);
            else
                snapshot.hasSpawnedAnts = baseline.hasSpawnedAnts;
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        [BurstCompile]
        [MonoPInvokeCallback(typeof(GhostComponentSerializer.ReportPredictionErrorsDelegate))]
        public static void ReportPredictionErrors(IntPtr componentData, IntPtr backupData, ref UnsafeList<float> errors)
        {
            ref var component = ref GhostComponentSerializer.TypeCast<AntSimulationRuntimeData>(componentData, 0);
            ref var backup = ref GhostComponentSerializer.TypeCast<AntSimulationRuntimeData>(backupData, 0);
            int errorIndex = 0;
            errors[errorIndex] = math.max(errors[errorIndex], math.distance(component.colonyPos, backup.colonyPos));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], math.distance(component.foodPosition, backup.foodPosition));
            ++errorIndex;
            errors[errorIndex] = math.max(errors[errorIndex], (component.hasSpawnedAnts != backup.hasSpawnedAnts) ? 1 : 0);
            ++errorIndex;
        }
        public static int GetPredictionErrorNames(ref FixedString512 names)
        {
            int nameCount = 0;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("colonyPos"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("foodPosition"));
            ++nameCount;
            if (nameCount != 0)
                names.Append(new FixedString32(","));
            names.Append(new FixedString64("hasSpawnedAnts"));
            ++nameCount;
            return nameCount;
        }
        #endif
    }
}
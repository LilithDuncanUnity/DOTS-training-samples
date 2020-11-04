using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Magneto.Track.Jobs
{
#if !DISABLE_BURST
    [BurstCompile]
#endif
    public struct CachedNeighbourIndexOffsetJob : IJob
    {
        [WriteOnly] public NativeArray<int3> W_Buffer;
        
        public void Execute()
        {
            int dirIndex = 0;
            for (int x=-1;x<=1;x++) {
                for (int y=-1;y<=1;y++) {
                    for (int z=-1;z<=1;z++) {
                        if (x!=0 || y!=0 || z!=0) {
                            W_Buffer[dirIndex] = new int3(x,y,z);
                            dirIndex++;
                        }
                    }
                }
            }
        }
    }
}
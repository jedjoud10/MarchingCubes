using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
namespace ProceduralTerrain
{
    //Create the voxel data for the marching cube job
    [BurstCompile]
    public struct MarchingCubeVoxelJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<float> voxels;//Density voxels
        public TerrainMainData terrainGenerationData;
        public float3 chunkPosition;
        public float scale;
        public int resolution;
        public void Execute(int index)
        {
            voxels[index] = MarchingCubeHelper.Density(scale * MarchingCubeHelper.ArrToVoxel(index, resolution) + chunkPosition - (new float3(1, 1, 1) * scale), terrainGenerationData);
        }
    }
}
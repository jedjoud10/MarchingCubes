using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
namespace ProceduralTerrain
{
    //A Job that generates a MarchingCubes mesh for a single chunk
    [BurstCompile]
    public struct MarchingCubeJob : IJobParallelFor
    {
        [ReadOnly]
        public TerrainMainData terrainGenerationData;
        [ReadOnly]
        public int LOD;
        [ReadOnly]
        public int resolution;
        [ReadOnly]
        public float scale;
        [ReadOnly]
        public NativeArray<float> voxels;
        public float3 chunkPosition;


        [NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeArray<Vector3> vertices;
        //[NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeList<int>.ParallelWriter triangles;
        [NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeArray<Color> colors;

        [ReadOnly]
        public NativeArray<int> triangulationTable;
        [ReadOnly]
        public NativeArray<float3> edgeTable;
        [ReadOnly]
        public NativeArray<float3> edgeTable2;
        //Generate mesh
        public void Execute(int index)
        {
            float3 pos = MarchingCubeHelper.ArrToVoxel(index, resolution);
            int caseNum = 0;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos), resolution + 2)] < 0) ? 1 : 0);
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(0, 1, 0)), resolution + 2)] < 0) ? 1 : 0) * 2;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 1, 0)), resolution + 2)] < 0) ? 1 : 0) * 4;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 0, 0)), resolution + 2)] < 0) ? 1 : 0) * 8;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(0, 0, 1)), resolution + 2)] < 0) ? 1 : 0) * 16;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(0, 1, 1)), resolution + 2)] < 0) ? 1 : 0) * 32;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 1, 1)), resolution + 2)] < 0) ? 1 : 0) * 64;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 0, 1)), resolution + 2)] < 0) ? 1 : 0) * 128;


            int currentTriangleIndex;
            NativeList<int> trianglesTemp = new NativeList<int>(0, Allocator.Temp);
            bool empty = true;
            for (int i = 0; i < 15; i++)
            {
                currentTriangleIndex = triangulationTable[caseNum * 15 + i];
                if (currentTriangleIndex != -1)
                {
                    empty = false;
                    //triangles.AddNoResize(currentTriangleIndex + index * 12);
                    trianglesTemp.Add(currentTriangleIndex + index * 12);//Make triangle face
                }
            }
            triangles.AddRangeNoResize(trianglesTemp);
            float3 vertex;
            if (empty) return;
            for (int i = 0; i < 12; i++)
            {
                if (trianglesTemp.Contains(i + index * 12))
                {
                    float3 gradient = CalculateVoxelGradientFromGrid(pos + new float3(1, 1, 1));
                    float lerp = 0.5f;
                    //lerp = Mathf.InverseLerp(MarchingCubeHelper.Density(edgeTable[i] + pos, terrainGenerationData), MarchingCubeHelper.Density(edgeTable2[i] + pos, terrainGenerationData), 0);
                    lerp = Mathf.InverseLerp(voxels[MarchingCubeHelper.VoxelToArr(edgeTable[i] + pos, resolution + 2)], voxels[MarchingCubeHelper.VoxelToArr(edgeTable2[i] + pos, resolution + 2)], 0);
                    vertex = math.lerp(edgeTable[i], edgeTable2[i], lerp) + pos;

                    vertices[index * 12 + i] = vertex * scale;//Add vertex at correct position
                    colors[index * 12 + i] = MarchingCubeHelper.Color(vertex * scale + chunkPosition, gradient, terrainGenerationData);
                    //colors[index * 12 + i] = MarchingCubeHelper.Float4ToColor(new float4(math.normalize(gradient), 0));
                }
                else
                {
                    vertices[index * 12 + i] = new float3(-1, -1, -1);//We dont need to make this unused vertex compute its position
                    colors[index * 12 + i] = Color.clear;
                }
            }
        }
        //Get the voxel gradient at a specific 3D point
        public float3 CalculateVoxelGradient(float3 point)
        {
            float3 normal = new Vector3();
            normal.x = MarchingCubeHelper.Density(point + new float3(1, 0, 0), terrainGenerationData) - MarchingCubeHelper.Density(point - new float3(1, 0, 0), terrainGenerationData);
            normal.y = MarchingCubeHelper.Density(point + new float3(0, 1, 0), terrainGenerationData) - MarchingCubeHelper.Density(point - new float3(0, 1, 0), terrainGenerationData);
            normal.z = MarchingCubeHelper.Density(point + new float3(0, 0, 1), terrainGenerationData) - MarchingCubeHelper.Density(point - new float3(0, 0, 1), terrainGenerationData);
            return normal;
        }
        //Get the voxel gradient using the voxel grid
        public float3 CalculateVoxelGradientFromGrid(float3 point)
        {
            float3 normal = new Vector3();
            normal.x = voxels[MarchingCubeHelper.VoxelToArr(point + new float3(1, 0, 0), resolution + 2)] - voxels[MarchingCubeHelper.VoxelToArr(point - new float3(1, 0, 0), resolution + 2)];
            normal.y = voxels[MarchingCubeHelper.VoxelToArr(point + new float3(0, 1, 0), resolution + 2)] - voxels[MarchingCubeHelper.VoxelToArr(point - new float3(0, 1, 0), resolution + 2)];
            normal.z = voxels[MarchingCubeHelper.VoxelToArr(point + new float3(0, 0, 1), resolution + 2)] - voxels[MarchingCubeHelper.VoxelToArr(point - new float3(0, 0, 1), resolution + 2)];
            return normal;
        }
    }
}
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
//A Job that generates a MarchingCubes mesh for a single chunk
[BurstCompile]
public struct MarchingCubeJob : IJobParallelFor
{
    public TerrainGenerationData terrainGenerationData;
    public TerrainColorData terrainColorData;
    public int LOD;
    public int resolution;
    public float scale;
    [ReadOnly]
    public NativeArray<float> voxels;
    public float3 chunkPosition;
    [NativeDisableParallelForRestriction]
    public NativeArray<float3> vertices;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> triangles;
    [NativeDisableParallelForRestriction]
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

        if (!terrainGenerationData.usePregeneratedVoxelData)
        {
            caseNum += ((MarchingCubeHelper.Density(scale * (pos) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0);
            caseNum += ((MarchingCubeHelper.Density(scale * (pos + new float3(0, 1, 0)) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0) * 2;
            caseNum += ((MarchingCubeHelper.Density(scale * (pos + new float3(1, 1, 0)) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0) * 4;
            caseNum += ((MarchingCubeHelper.Density(scale * (pos + new float3(1, 0, 0)) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0) * 8;
            caseNum += ((MarchingCubeHelper.Density(scale * (pos + new float3(0, 0, 1)) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0) * 16;
            caseNum += ((MarchingCubeHelper.Density(scale * (pos + new float3(0, 1, 1)) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0) * 32;
            caseNum += ((MarchingCubeHelper.Density(scale * (pos + new float3(1, 1, 1)) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0) * 64;
            caseNum += ((MarchingCubeHelper.Density(scale * (pos + new float3(1, 0, 1)) + chunkPosition, terrainGenerationData) < 0) ? 1 : 0) * 128;
        }
        else
        {
            //pos += new float3(1, 1, 1);
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos), resolution+2)] < 0) ? 1 : 0);
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(0, 1, 0)), resolution+2)] < 0) ? 1 : 0) * 2;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 1, 0)), resolution+2)] < 0) ? 1 : 0) * 4;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 0, 0)), resolution+2)] < 0) ? 1 : 0) * 8;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(0, 0, 1)), resolution+2)] < 0) ? 1 : 0) * 16;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(0, 1, 1)), resolution+2)] < 0) ? 1 : 0) * 32;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 1, 1)), resolution+2)] < 0) ? 1 : 0) * 64;
            caseNum += ((voxels[MarchingCubeHelper.VoxelToArr((pos + new float3(1, 0, 1)), resolution+2)] < 0) ? 1 : 0) * 128;
        }
        int currentTriangleIndex;
        NativeList<int> trianglesTemp = new NativeList<int>(0, Allocator.Temp);
        for (int i = 0; i < 16; i++)
        {
            currentTriangleIndex = triangulationTable[caseNum * 16 + i];
            if (currentTriangleIndex != -1)
            {
                trianglesTemp.Add(currentTriangleIndex);//Make triangle face
                triangles[index * 15 + i] = currentTriangleIndex + index * 12;
            }
        }
        float3 vertex;
        for (int i = 0; i < 12; i++)
        {
            if (trianglesTemp.Contains(i))
            {
                float3 gradient = CalculateVoxelGradientFromGrid(pos + new float3(1, 1, 1));
                if (terrainGenerationData.interpolation)
                {
                    //Use interpolation when placing the current vertex
                    float lerp = 0.5f;
                    if (terrainGenerationData.usePregeneratedVoxelData)
                    {
                        lerp = Mathf.InverseLerp(voxels[MarchingCubeHelper.VoxelToArr(edgeTable[i] + pos, resolution + 2)], voxels[MarchingCubeHelper.VoxelToArr(edgeTable2[i] + pos, resolution + 2)], 0);
                        vertex = (math.lerp(edgeTable[i], edgeTable2[i], lerp) + pos);
                        gradient = math.lerp(CalculateVoxelGradientFromGrid(edgeTable[i] + pos), CalculateVoxelGradientFromGrid(edgeTable2[i] + pos), lerp);
                    }
                    else
                    {
                        lerp = Mathf.InverseLerp(MarchingCubeHelper.Density(scale * (edgeTable[i] + pos) + chunkPosition, terrainGenerationData), MarchingCubeHelper.Density(scale * (edgeTable2[i] + pos) + chunkPosition, terrainGenerationData), 0);
                        vertex = (math.lerp(edgeTable[i], edgeTable2[i], lerp) + pos);
                        if (terrainGenerationData.useVertexPosAsGradientSource) gradient = CalculateVoxelGradient(vertex * scale + chunkPosition);
                    }
                    //vertex = (math.lerp(edgeTable[i], edgeTable2[i], Mathf.InverseLerp(MarchingCubeHelper.Density(scale * (edgeTable[i] + pos) + chunkPosition, terrainGenerationData), MarchingCubeHelper.Density(scale * (edgeTable2[i] + pos) + chunkPosition, terrainGenerationData), 0)) + pos);
                    vertices[index * 12 + i] = vertex * scale;//Add vertex at correct position
                    colors[index * 12 + i] = MarchingCubeHelper.ColorDensity(vertex * scale + chunkPosition, gradient, terrainColorData);
                }
                else
                {
                    //Do not use interpolation when placing the current vertex
                    vertex = (math.lerp(edgeTable[i], edgeTable2[i], 0.5f)) + pos;
                    if (terrainGenerationData.useVertexPosAsGradientSource && !terrainGenerationData.usePregeneratedVoxelData)
                    {
                        gradient = CalculateVoxelGradient(vertex * scale + chunkPosition);
                    }                    
                    vertices[index * 12 + i] = vertex * scale;
                    colors[index * 12 + i] = MarchingCubeHelper.ColorDensity(vertex * scale + chunkPosition, gradient, terrainColorData);
                }
            }
            else
            {
                vertices[index * 12 + i] = - Vector3.one;//We dont need to make this unused vertex compute its position
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
        normal.x = voxels[MarchingCubeHelper.VoxelToArr(point + new float3(1, 0, 0), resolution+2)] - voxels[MarchingCubeHelper.VoxelToArr(point - new float3(1, 0, 0), resolution+2)];
        normal.y = voxels[MarchingCubeHelper.VoxelToArr(point + new float3(0, 1, 0), resolution+2)] - voxels[MarchingCubeHelper.VoxelToArr(point - new float3(0, 1, 0), resolution+2)];
        normal.z = voxels[MarchingCubeHelper.VoxelToArr(point + new float3(0, 0, 1), resolution+2)] - voxels[MarchingCubeHelper.VoxelToArr(point - new float3(0, 0, 1), resolution+2)];
        return normal;
    }
}
[Serializable]
//Noise
public struct NoiseData
{
    public float mainScale;
    public Vector3 scale;
    public float height;
    public float lacunarity;
    public float persistence;
    public int octaves;
}
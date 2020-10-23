using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
//A singular chunk
public class MarchingCubeChunk : MonoBehaviour
{
    private TerrainGenerationData terrainGenerationData;
    private TerrainColorData terrainColorData;
    private TerrainGenerator terrain;
    public Vector3Int chunkPosition;
    // Start is called before the first frame update
    void Start()
    {

    }

    private NativeArray<float3> vertices;
    private NativeList<Vector3> finalVertices;
    private NativeArray<int> triangles;
    private NativeList<int> finalTriangles;
    private NativeArray<float> voxels;
    private NativeArray<Color> colors;
    private NativeList<Color> finalColors;
    private NativeArray<int> triTable;
    private NativeArray<float3> edgeTable;
    private NativeArray<float3> edgeTable2;
    private bool completed = false;
    private JobHandle jobHandle, optimizeHandle, voxelHandle;
    //Generates the MarchingCube mesh
    public void GenerateMesh(TerrainGenerationData _terrainGenerationData, TerrainColorData _terrainColorData, Vector3Int chunkPos, TerrainGenerator _terrain, int[] _triTable, float3[] _edgeTable, float3[] _edgeTable2, int LOD = 0, bool resetMesh = true, bool immediate = false)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        if(resetMesh) GetComponent<MeshFilter>().sharedMesh = new Mesh();
        completed = false;
        terrainGenerationData = _terrainGenerationData;
        terrainColorData = _terrainColorData;
        terrain = _terrain;
        chunkPosition = chunkPos;
        int resolution = terrainGenerationData.resolution - LOD;
        voxels = new NativeArray<float>((resolution+2) * (resolution+2) * (resolution+2), Allocator.Persistent);
        vertices = new NativeArray<float3>(12 * resolution * resolution * resolution, Allocator.Persistent);
        finalVertices = new NativeList<Vector3>(Allocator.Persistent);
        triangles = new NativeArray<int>(16 * resolution * resolution * resolution, Allocator.Persistent);
        finalTriangles = new NativeList<int>(Allocator.Persistent);
        colors = new NativeArray<Color>(12 * resolution * resolution * resolution, Allocator.Persistent);
        finalColors = new NativeList<Color>(Allocator.Persistent);
        triTable = new NativeArray<int>(_triTable.Length, Allocator.Persistent);
        edgeTable = new NativeArray<float3>(12, Allocator.Persistent);
        edgeTable2 = new NativeArray<float3>(12, Allocator.Persistent);
        edgeTable.CopyFrom(_edgeTable);
        edgeTable2.CopyFrom(_edgeTable2);
        triTable.CopyFrom(_triTable);
        if (_terrainGenerationData.usePregeneratedVoxelData)
        {
            MarchingCubeVoxelJob voxelJob = new MarchingCubeVoxelJob()
            {
                voxels = voxels,
                chunkPosition = new float3(chunkPosition.x * 10, chunkPosition.y * 10, chunkPosition.z * 10),
                scale = (float)10 / (float)resolution,
                resolution = resolution+2,
                terrainGenerationData = _terrainGenerationData
            };
            voxelHandle = voxelJob.Schedule((resolution + 2) * (resolution + 2) * (resolution + 2), 64);
        }
        MarchingCubeJob job = new MarchingCubeJob()
        {
            edgeTable = edgeTable,
            edgeTable2 = edgeTable2,
            scale = (float)10 / (float)resolution,
            LOD = LOD,
            voxels = voxels,
            resolution = resolution,
            triangulationTable = triTable,
            chunkPosition = new float3(chunkPosition.x * 10, chunkPosition.y * 10, chunkPosition.z * 10),
            vertices = vertices,
            triangles = triangles,
            colors = colors,
            terrainGenerationData = _terrainGenerationData,
            terrainColorData = _terrainColorData
        };
        if (_terrainGenerationData.usePregeneratedVoxelData) 
        {
            jobHandle = job.Schedule(resolution * resolution * resolution, 64, voxelHandle);
        }
        else
        {
            jobHandle = job.Schedule(resolution * resolution * resolution, 64);
        }

        
        MarchingCubeOptimizeJob optimize = new MarchingCubeOptimizeJob()
        {
            vertices = vertices,
            finalVertices = finalVertices,
            triangles = triangles,
            finalTriangles = finalTriangles,
            colors = colors,
            finalColors = finalColors
        };

        optimizeHandle = optimize.Schedule(jobHandle);

        if (immediate) 
        {
            CompleteChunkJob();
            stopwatch.Stop();
            UnityEngine.Debug.Log(stopwatch.ElapsedMilliseconds);
        }
    }
    // Update is called once per frame
    void Update()
    {        
        if(optimizeHandle.IsCompleted && !completed && Time.frameCount % UnityEngine.Random.Range(1, 120) == 0) 
        {
            CompleteChunkJob();
        }        
    }   
    //Force complete the chunk job, update the mesh and dispose the native containers
    private void CompleteChunkJob() 
    {
        optimizeHandle.Complete();

        Mesh mesh = new Mesh();
        mesh.vertices = finalVertices.ToArray();
        mesh.triangles = finalTriangles.ToArray();
        mesh.colors = finalColors.ToArray();
        mesh.Optimize();
        //mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        //Dispose the NativeContainers
        vertices.Dispose();
        finalVertices.Dispose();
        triangles.Dispose();
        finalTriangles.Dispose();
        colors.Dispose();
        finalColors.Dispose();

        triTable.Dispose();
        edgeTable.Dispose();
        edgeTable2.Dispose();
        completed = true;
        terrain.ChunkGenerated(this);
    }
}
public struct ChunkData 
{
    
}
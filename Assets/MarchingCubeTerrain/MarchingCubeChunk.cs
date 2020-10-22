using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace ProceduralTerrain
{
    //A singular chunk
    public class MarchingCubeChunk : MonoBehaviour
    {
        private TerrainMainData terrainGenerationData;
        private TerrainGenerator terrain;
        public Vector3Int chunkPosition;
        // Start is called before the first frame update
        void Start()
        {

        }

        private NativeArray<Vector3> vertices;
        private NativeList<Vector3> finalVertices;
        private NativeList<int> triangles;
        private NativeList<int> newVerts;
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
        public void GenerateMesh(TerrainMainData _terrainGenerationData, Vector3Int chunkPos, TerrainGenerator _terrain, int[] _triTable, float3[] _edgeTable, float3[] _edgeTable2, int LOD = 0, bool resetMesh = true, bool inEditor = false)
        {
            _terrain.ChunkGenerates(this);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (resetMesh)
            {
                GetComponent<MeshFilter>().sharedMesh = new Mesh();
                GetComponent<MeshCollider>().sharedMesh = new Mesh();
            }
            //Setup
            completed = false;
            terrainGenerationData = _terrainGenerationData;
            terrain = _terrain;
            chunkPosition = chunkPos;
            int resolution = terrainGenerationData.resolution - LOD;
            //Create native containers
            voxels = new NativeArray<float>((resolution + 2) * (resolution + 2) * (resolution + 2), Allocator.Persistent);
            vertices = new NativeArray<Vector3>(12 * resolution * resolution * resolution, Allocator.Persistent);
            finalVertices = new NativeList<Vector3>(12 * resolution * resolution * resolution, Allocator.Persistent);
            triangles = new NativeList<int>(15 * resolution * resolution * resolution, Allocator.Persistent);
            finalTriangles = new NativeList<int>(15 * resolution * resolution * resolution, Allocator.Persistent);
            colors = new NativeArray<Color>(12 * resolution * resolution * resolution, Allocator.Persistent);
            finalColors = new NativeList<Color>(12 * resolution * resolution * resolution, Allocator.Persistent);
            triTable = new NativeArray<int>(_triTable.Length, Allocator.Persistent);
            edgeTable = new NativeArray<float3>(12, Allocator.Persistent);
            edgeTable2 = new NativeArray<float3>(12, Allocator.Persistent);

            newVerts = new NativeList<int>(0, Allocator.Persistent);

            //Copying data
            edgeTable.CopyFrom(_edgeTable);
            edgeTable2.CopyFrom(_edgeTable2);
            triTable.CopyFrom(_triTable);

            //Creation of the jobs
            MarchingCubeVoxelJob voxelJob = new MarchingCubeVoxelJob()
            {
                voxels = voxels,
                chunkPosition = new float3(chunkPosition.x * 10, chunkPosition.y * 10, chunkPosition.z * 10),
                scale = (float)10 / (float)resolution,
                resolution = resolution + 2,
                terrainGenerationData = _terrainGenerationData
            };
            voxelHandle = voxelJob.Schedule((resolution + 2) * (resolution + 2) * (resolution + 2), 32);
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
                triangles = triangles.AsParallelWriter(),
                colors = colors,
                terrainGenerationData = _terrainGenerationData
            };
            jobHandle = job.Schedule(resolution * resolution * resolution, 32, voxelHandle);


            MarchingCubeOptimizeJob optimize = new MarchingCubeOptimizeJob()
            {
                vertices = vertices,
                finalVertices = finalVertices,
                colors = colors,
                finalColors = finalColors,
                triangles = triangles,
                finalTriangles = finalTriangles
            };
            optimizeHandle = optimize.Schedule(jobHandle);


            //If in editor
            if (inEditor)
            {
                CompleteChunkJob();
                stopwatch.Stop();
                UnityEngine.Debug.Log(stopwatch.ElapsedMilliseconds);
            }
        }
        // Update is called once per frame
        void Update()
        {
            if (optimizeHandle.IsCompleted && !completed && Time.frameCount % UnityEngine.Random.Range(1, 120) == 0)
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
            voxels.Dispose();
            vertices.Dispose();
            finalVertices.Dispose();
            triangles.Dispose();
            finalTriangles.Dispose();
            colors.Dispose();
            finalColors.Dispose();
            newVerts.Dispose();

            triTable.Dispose();
            edgeTable.Dispose();
            edgeTable2.Dispose();
            completed = true;
            terrain.ChunkGenerated(this);
        }
        //Gizmos
        private void OnDrawGizmos()
        {
            Gizmos.color = completed ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position + new Vector3(5, 5, 5), new Vector3(10, 10, 10));
        }
    }
    public struct ChunkData
    {

    }
}
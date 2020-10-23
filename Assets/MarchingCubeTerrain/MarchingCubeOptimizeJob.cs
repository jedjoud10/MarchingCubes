using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
//A Job that optimizes the rendering of MarchingCubes meshes
[BurstCompile]
public struct MarchingCubeOptimizeJob : IJob
{
    public NativeArray<float3> vertices;
    public NativeList<Vector3> finalVertices;
    public NativeArray<int> triangles;
    public NativeList<int> finalTriangles;
    public NativeArray<Color> colors;
    public NativeList<Color> finalColors;
    public void Execute()
    {
        /*
        var verts = vertices.ToArray();
        Dictionary<Vector3, int> duplicateHashTable = new Dictionary<Vector3, int>();
        List<int> newVerts = new List<int>();
        int[] map = new int[verts.Length];

        //create mapping and find duplicates, dictionaries are like hashtables, mean fast
        for (int i = 0; i < verts.Length; i++)
        {
            if (!duplicateHashTable.ContainsKey(verts[i]))
            {
                duplicateHashTable.Add(verts[i], newVerts.Count);
                map[i] = newVerts.Count;
                newVerts.Add(i);
            }
            else
            {
                map[i] = duplicateHashTable[verts[i]];
            }
        }

        // create new vertices
        var verts2 = new Vector3[newVerts.Count];
        for (int i = 0; i < newVerts.Count; i++)
        {
            int a = newVerts[i];
            verts2[i] = verts[a];
        }
        // map the triangle to the new vertices
        var tris = triangles.ToArray();
        for (int i = 0; i < tris.Length; i++)
        {
            tris[i] = map[tris[i]];
        }
        triangles = new NativeArray<int>(tris, Allocator.Temp);
        vertices = new NativeArray<Vector3>(verts2, Allocator.Temp);
        */        
        NativeHashMap<Vector3, int> duplicateHashTable = new NativeHashMap<Vector3, int>(0, Allocator.Temp);
        NativeList<int> newVerts = new NativeList<int>(0, Allocator.Temp);
        NativeArray<int> map = new NativeArray<int>(vertices.Length, Allocator.Temp);
        
        //create mapping and find duplicates, dictionaries are like hashtables, mean fast
        for (int i = 0; i < vertices.Length; i++)
        {
            if (!duplicateHashTable.ContainsKey(vertices[i]))
            {
                duplicateHashTable.Add(vertices[i], newVerts.Length);
                map[i] = newVerts.Length;
                newVerts.Add(i);
            }
            else
            {
                map[i] = duplicateHashTable[vertices[i]];
            }
        }

        // create new vertices
        for (int i = 0; i < newVerts.Length; i++)
        {
            int a = newVerts[i];
            finalVertices.Add(vertices[a]);
            finalColors.Add(colors[a]);
        }
        // map the triangle to the new vertices
        bool isEmpty = true;
        for (int i = 0; i < triangles.Length; i++)
        {
            if (i < 12)
            {
                finalTriangles.Add(map[triangles[i]]);
                isEmpty &= triangles[i] == 0;
            }
            else if (triangles[i] != 0)
            {
                finalTriangles.Add(map[triangles[i]]);
                isEmpty = false;
            }
        }
        
        //finalTriangles.RemoveRangeWithBeginEnd(0, 12);
        if (finalTriangles.Length == 12 && finalVertices.Length == 1) 
        {
            finalTriangles.Clear();
        }
        
    }
}
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace ProceduralTerrain {
    public class TerrainEditor : MonoBehaviour
    {
        public new Transform camera;
        private TerrainGenerator terrain;
        public float size;
        public Color color;
        public float strengh;
        // Start is called before the first frame update
        void Start()
        {
            terrain = FindObjectOfType<TerrainGenerator>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
                {
                    if (hit.collider.GetComponent<MarchingCubeChunk>() == null) return;
                    List<MarchingCubeChunk> chunks = terrain.FindChunks(size, hit.point);
                    foreach (var chunk in chunks)
                    {
                        UpdateChunkVoxel(hit.point, size, -strengh * Time.deltaTime, color, chunk);
                    }
                }
            }
            if (Input.GetMouseButton(1))
            {
                RaycastHit hit;
                if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
                {
                    if (hit.collider.GetComponent<MarchingCubeChunk>() == null) return;
                    List<MarchingCubeChunk> chunks = terrain.FindChunks(size, hit.point);
                    foreach (var chunk in chunks)
                    {
                        UpdateChunkVoxel(hit.point, size, strengh * Time.deltaTime, color, chunk);
                    }
                }
            }
        }
        //Update chunk
        private void UpdateChunkVoxel(Vector3 position, float _size, float _strengh, Color _color, MarchingCubeChunk chunk)
        {
            if (chunk == null) return;
            Vector3 relativePosition = position - chunk.chunkPosition * 10;
            //The edit that will be passed to the VoxelJob
            TerrainEdit edit;
            edit.position = relativePosition;
            edit.strengh = _strengh;
            edit.size = _size;
            edit.color = _color;

            terrain.RegenerateChunkImmediate(chunk, edit);
        }
    }
}
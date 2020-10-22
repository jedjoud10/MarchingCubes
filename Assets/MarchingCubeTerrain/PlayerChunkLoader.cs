using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace ProceduralTerrain
{
    //Loads/Generates new chunks and unloads chunks
    public class PlayerChunkLoader : MonoBehaviour
    {
        private Vector3Int currentChunkPos;
        private Vector3Int lastChunkPos = new Vector3Int(1, 1, 1);
        public int renderDistance = 2;
        public TerrainGenerator terrain;
        public List<Vector3Int> loadedChunks = new List<Vector3Int>();
        public List<Vector3Int> lastLoadedChunks = new List<Vector3Int>();
        // Start is called before the first frame update
        void Start()
        {
            terrain = FindObjectOfType<TerrainGenerator>();
            terrain.GenerateChunkPool(renderDistance);
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 position = transform.position;
            currentChunkPos = new Vector3Int(Mathf.FloorToInt(position.x / 10), Mathf.FloorToInt(position.y / 10), Mathf.FloorToInt(position.z / 10));
            if (lastChunkPos != currentChunkPos && terrain.data.procedurallyGenerate)
            {
                lastChunkPos = currentChunkPos;
                for (int x = -renderDistance; x < renderDistance; x++)
                {
                    for (int z = -renderDistance; z < renderDistance; z++)
                    {
                        for (int y = -renderDistance; y < renderDistance; y++)
                        {
                            loadedChunks.Add((new Vector3Int(x, y, z) + currentChunkPos));
                            terrain.ChunkLoadUpdate(Vector3.Distance(new Vector3Int(x, y, z) + currentChunkPos, currentChunkPos), new Vector3Int(x, y, z) + currentChunkPos);
                        }
                    }
                }

                foreach (var oldChunk in lastLoadedChunks)
                {
                    if (!loadedChunks.Contains(oldChunk))
                    {
                        terrain.UnloadChunk(oldChunk);
                    }
                }
                foreach (var newChunk in loadedChunks)
                {
                    if (!lastLoadedChunks.Contains(newChunk))
                    {
                        terrain.LoadChunk(newChunk);
                    }
                }

                lastLoadedChunks = new List<Vector3Int>(loadedChunks);
                loadedChunks.Clear();
            }
            //transform.Translate(Vector3.forward * Time.deltaTime * 10f);
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(currentChunkPos * 10 + new Vector3(5, 5, 5), new Vector3(10, 10, 10));
        }
        private void OnGUI()
        {
            string biomeName = MarchingCubeHelper.BiomeNameAtWorldPoint(transform.position, terrain.data).ToString();
            GUI.Label(new Rect(0, 0, 800, 100), "Biome: " + biomeName);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using UnityEditor;
using System.Net;
using JetBrains.Annotations;

namespace ProceduralTerrain
{
    //Marching cube helper class
    public static class MarchingCubeHelper
    {
        //----Noise functions----\\
        #region Noise Functions
        //Biome blend function
        public static float4 BiomeBlend(float3 point, TerrainMainData data) 
        {
            if (data.biomeBlend.ble_overwrite) return data.biomeBlend.ble_overwriteValue;
            point *= data.biomeBlend.ble_scale;

            float biomeBlend1 = CellularNoise2Octaves(point, data.biomeBlend.ble_noise);
            float biomeBlend2 = CellularNoise2Octaves(point, data.biomeBlend.ble_noise2);
            float biomeBlend3 = CellularNoise2Octaves(point, data.biomeBlend.ble_noise3);
            //biomeBlend = 1 - noise.cellular(point * data.biomeBlend.ble_scale).y;
            float4 finalBiomeBlend = new float4(biomeBlend1, biomeBlend2, biomeBlend3, 0);


            finalBiomeBlend += data.biomeBlend.ble_offset;

            finalBiomeBlend = 1 / (1 + math.pow(data.biomeBlend.ble_pow, new float4(-finalBiomeBlend * data.biomeBlend.ble_pow + data.biomeBlend.ble_pow * 0.5f)));
            
            return math.saturate(finalBiomeBlend);
        }
        //Pick a biome name from the BiomeBlend biomedensity function
        public static BiomeNames BiomeNameAtWorldPoint(float3 point, TerrainMainData data) 
        {
            point *= (10 / (float)data.resolution) * data.scale;
            BiomeNames name = BiomeNames.Normal;
            float4 biomeBlend = BiomeBlend(point, data);
            if (biomeBlend.x > 0.5f) name = BiomeNames.Desert;
            if (biomeBlend.y > 0.5f) name = BiomeNames.Rocky;
            return name;
        }
        //Density function for the marching cube algorithm
        public static float Density(float3 point, TerrainMainData data)
        {
            float density = 0;
            point *= data.scale;


            float4 biomeBlend = BiomeBlend(point, data);


            //---Biomes---\\

            density += math.lerp(math.lerp(NormalDensity(point, data.biome_normal), DesertDensity(point, data.biome_desert), biomeBlend.x), RockyDensity(point, data.biome_rocky), biomeBlend.y);
            
            return density;
        }        
        //Main biome
        private static float NormalDensity(float3 point, NormalBiome biome) 
        {
            //return noise.snoise(point * 0.2f) + data.offset;
            float density = (point.y - biome.gen_floorHeight) * biome.gen_floorFactor;

            density += CellularNoiseOctaves(point, biome.gen_noise1);

            return density + biome.gen_offset;
        }
        //Desert biome
        private static float DesertDensity(float3 point, DesertBiome biome) 
        {
            float density = (point.y - biome.gen_floorHeight) * biome.gen_floorFactor;

            density += SimplexNoiseOctaves(point, biome.gen_noise1);

            return density + biome.gen_offset;
        }
        //Rocky biome
        private static float RockyDensity(float3 point, RockyBiome biome) 
        {
            float density = (point.y - biome.gen_floorHeight) * biome.gen_floorFactor;

            density += CellularNoise2Octaves(point, biome.gen_noise1);

            return density + biome.gen_offset;
        }
        //Signed distance field function for boxo from https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm
        public static float sdBox(float3 p, float3 b)
        {
            float3 q = math.abs(p) - b;
            return math.length(math.max(q, 0.0f)) + math.min(math.max(q.x, math.max(q.y, q.z)), 0.0f);
        }
        //Simple noise
        public static float SimpleNoise01(float3 point)
        {
            return math.saturate((noise.snoise(point) + 1) / 2);
        }
        //Transform color to float4
        public static float4 ColorToFloat4(Color c)
        {
            return new float4(c.r, c.g, c.b, c.a);
        }
        //Transform float4 to color
        public static Color Float4ToColor(float4 c)
        {
            return new Color(c.x, c.y, c.z, c.w);
        }
        //Cellular fractal noise
        public static float CellularNoiseOctaves(float3 point, NoiseData noiseData)
        {
            float density = 0;
            point += noiseData.offset;
            for (int i = 0; i < noiseData.octaves; i++)
            {
                density += (1 - noise.cellular(point * noiseData.scale * noiseData.mainScale * math.pow(noiseData.lacunarity, i)).x) * math.pow(noiseData.persistence, i);
            }
            return density * noiseData.height;
        }
        //Cellular fractal noise (type 2)
        public static float CellularNoise2Octaves(float3 point, NoiseData noiseData)
        {
            float density = 0;
            point += noiseData.offset;
            for (int i = 0; i < noiseData.octaves; i++)
            {
                density += (1 - noise.cellular(point * noiseData.scale * noiseData.mainScale * math.pow(noiseData.lacunarity, i)).y) * math.pow(noiseData.persistence, i);
            }
            return density * noiseData.height;
        }
        //Simplex fractal noise
        public static float SimplexNoiseOctaves(float3 point, NoiseData noiseData)
        {
            float density = 0;
            point += noiseData.offset;
            for (int i = 0; i < noiseData.octaves; i++)
            {
                density += (noise.snoise(point * noiseData.scale * noiseData.mainScale * math.pow(noiseData.lacunarity, i))) * math.pow(noiseData.persistence, i);
            }
            return density * noiseData.height;
        }
        //Simplex fractal 3D noise
        public static float3 Simplex3DNoiseOctaves(float3 point, NoiseData3D noiseData)
        {
            float3 density = 0;
            point += noiseData.offset;
            for (int i = 0; i < noiseData.octaves; i++)
            {
                density += (Simplex3DNoise(point * noiseData.scale * noiseData.mainScale * math.pow(noiseData.lacunarity, i), noiseData)) * math.pow(noiseData.persistence, i);
            }
            return density * noiseData.height;
        }
        //Cellular fractal 3D noise
        public static float3 Cellular3DnoiseOctaves(float3 point, NoiseData3D noiseData)
        {
            float3 density = 0;
            point += noiseData.offset;
            for (int i = 0; i < noiseData.octaves; i++)
            {
                density.x += (1 - noise.cellular(point * noiseData.x.scale * noiseData.mainScale  * math.pow(noiseData.lacunarity, i)).x + noiseData.x.offset) * math.pow(noiseData.persistence, i) * noiseData.x.height;
                density.y += (1 - noise.cellular(point * noiseData.y.scale * noiseData.mainScale * math.pow(noiseData.lacunarity, i)).x + noiseData.y.offset) * math.pow(noiseData.persistence, i) * noiseData.y.height;
                density.z += (1 - noise.cellular(point * noiseData.z.scale * noiseData.mainScale * math.pow(noiseData.lacunarity, i)).x + noiseData.z.offset) * math.pow(noiseData.persistence, i) * noiseData.z.height;
            }
            return density * noiseData.height;
        }
        //Simplex 3D noise
        public static float3 Simplex3DNoise(float3 point, NoiseData3D noiseData)
        {
            return new float3(noise.snoise(point * noiseData.x.scale + noiseData.x.offset) * noiseData.x.height, noise.snoise(point * noiseData.y.scale + noiseData.y.offset) * noiseData.y.height, noise.snoise(point * noiseData.z.scale + noiseData.z.offset) * noiseData.z.height);
        }
        #endregion
        #region Color Functions
        public static Color Color(float3 point, float3 gradient, TerrainMainData data) 
        {
            gradient = math.normalize(gradient);
            float4 color = new float4();
            point *= data.scale;  
            float4 biomeBlend = BiomeBlend(point, data);
            //---Biomes---\\
            color = math.lerp(math.lerp(NormalColor(point, gradient, data.biome_normal), DesertColor(point, gradient, data.biome_desert), biomeBlend.x), RockyColor(point, gradient, data.biome_rocky), biomeBlend.y);//Main biome color


            if (data.debug) color = new float4(gradient, 0);
            //Clamping
            color = math.saturate(color);
            return Float4ToColor(color);
        }
        //Normal biome
        private static float4 NormalColor(float3 point, float3 gradient, NormalBiome biome)
        {            
            return math.lerp(ColorToFloat4(biome.col_dirt), ColorToFloat4(biome.col_grass), math.saturate(math.pow(math.saturate(gradient.y + biome.col_dirtOffset), biome.col_dirtPow)));
        }
        //Desert biome
        private static float4 DesertColor(float3 point, float3 gradient, DesertBiome biome)
        {
            return ColorToFloat4(biome.col_main - (biome.col_main * (SimpleNoise01(point * biome.col_mainNoiseScale) + biome.col_mainOffset)) * biome.col_mainNoiseInfluence);
        }
        //Rocky biome
        private static float4 RockyColor(float3 point, float3 gradient, RockyBiome biome) 
        {
            return ColorToFloat4(biome.col_main - (biome.col_main * (noise.snoise(new float2(point.y * biome.col_mainNoiseScale)) + biome.col_mainOffset)) * biome.col_mainNoiseInfluence);
        }
        #endregion
        //Array int to voxel pos
        public static float3 ArrToVoxel(int index, int resolution)
        {
            // N(ABC) -> N(A) x N(BC)
            int y = index / (resolution * resolution);   // x in N(A)
            int w = index % (resolution * resolution);  // w in N(BC)

            // N(BC) -> N(B) x N(C)
            int z = w / resolution;        // y in N(B)
            int x = w % resolution;        // z in N(C)
            return new float3(x, y, z);
        }
        //Voxel pos to array int
        public static int VoxelToArr(float3 pos, int resolution)
        {
            return (int)math.round((pos.y * (float)resolution * (float)resolution) + (pos.z * (float)resolution) + pos.x);
        }
    }
    //Noise
    [Serializable]
    public struct NoiseData
    {
        public float mainScale;
        public float3 offset;
        public Vector3 scale;
        public float height;
        public float lacunarity;
        public float persistence;
        public int octaves;
    }
    //3D Noise
    [Serializable]
    public struct NoiseData3D
    {
        public float mainScale;
        public float3 offset;
        public Vector3 scale;
        public float height;
        public float lacunarity;
        public float persistence;
        public int octaves;
        public NoiseDataSimple x, y, z;
    }
    //Simple noise
    [Serializable]
    public struct NoiseDataSimple
    {
        public float scale;
        public float height;
        public float offset;
    }
}
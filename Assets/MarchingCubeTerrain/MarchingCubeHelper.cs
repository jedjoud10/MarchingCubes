using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

//Marching cube helper class
public static class MarchingCubeHelper
{
    //----Noise functions----\\
    #region Noise Functions
    //Density function for the marching cube algorithm
    public static float Density(float3 point, TerrainGenerationData data)
    {
        point *= data.scale;
        float density = (point.y - data.floorHeight) * data.floorFactor;
        //return density + data.offset;
        density += SimplexNoiseOctaves(point, data.noise1);
        density += CellularNoiseOctaves(point, data.noise2);
        density += CellularNoise2Octaves(point, data.noise3);
        /*
        if (density < -10) 
        { 
            density = CellularNoise2Octaves(point, data.noise3);
        }    
        */
        return density + data.offset;
    }
    //Color function
    public static Color ColorDensity(float3 point, float3 gradient, TerrainColorData colorData)
    {
        gradient = math.normalize(gradient);
        //Blends between grass and dirt
        float grassBlend = math.saturate(math.pow(math.dot(gradient, new float3(0, 1, 0)) + colorData.offset, colorData.pow));
        //Blends between dirt and stone
        float dirtBlend = math.saturate(math.pow(math.dot(gradient, new float3(0, 1, 0)) + colorData.offset2, colorData.pow2));
        float4 grassColor = ColorToFloat4(colorData.grass) * ((noise.snoise(point * colorData.grassNoiseScale) * colorData.grassNoiseMul) + colorData.grassNoiseOffset);
        float4 color;


        //color = math.lerp(ColorToFloat4(colorData.dirt), ColorToFloat4(colorData.grass), math.clamp(grassBlend, 0, 1));
        color = math.lerp(math.lerp(ColorToFloat4(colorData.stone), ColorToFloat4(colorData.dirt), dirtBlend), grassColor, grassBlend);
        //Clamping
        //color = new float4(gradient, 1);
        color = math.clamp(color, float4.zero, new float4(1, 1, 1, 1));
        return Float4ToColor(color);
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
        for (int i = 0; i < noiseData.octaves; i++)
        {
            density += (noise.snoise(point * noiseData.scale * noiseData.mainScale * math.pow(noiseData.lacunarity, i))) * math.pow(noiseData.persistence, i);
        }
        return density * noiseData.height;
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
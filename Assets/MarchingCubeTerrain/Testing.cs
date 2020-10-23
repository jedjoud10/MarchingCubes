using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Testing : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int index = 0;
        for (int y = 0; y < 2; y++)
        {
            for (int z = 0; z < 2; z++)
            {
                for (int x = 0; x < 2; x++)
                {
                    Debug.Log("Real: " + new float3(x, y, z) + " Estimate: " + MarchingCubeHelper.ArrToVoxel(MarchingCubeHelper.VoxelToArr(new float3(x, y, z), 2), 2));
                    index++;
                }
            }
        }
    }
}

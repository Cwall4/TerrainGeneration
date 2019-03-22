using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {

	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre) {
		float[,] values = Noise.GenerateNoiseMap (width, height, settings.noiseSettings, sampleCentre);

		AnimationCurve heightCurve_threadsafe = new AnimationCurve (settings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;
        //Debug.Log("Pre-erosion array size: " + values.GetLength(0) + ", " + values.GetLength(1));
        // This is a sloppy way to get the heightMap's values "eroded" before evaluation and finding min/max values.
        values = WaterErosion(values);
        //Debug.Log("Post-erosion array size: " + values.GetLength(0) + ", " + values.GetLength(1));

        for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				values [i, j] *= heightCurve_threadsafe.Evaluate (values [i, j]) * settings.heightMultiplier;

				if (values [i, j] > maxValue) {
					maxValue = values [i, j];
				}
				if (values [i, j] < minValue) {
					minValue = values [i, j];
				}
			}
		}

        return new HeightMap(values, minValue, maxValue);
    }

    public static float[,] GenerateZeroMap(int x, int y)
    {
        float[,] zeroMap = new float[x, y];

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                zeroMap[i, j] = 0;
            }
        }

        return zeroMap;
    }

    public static Vector2[,] Downhill(float[,] noiseMap)
    {
        Vector2[,] downs = new Vector2[noiseMap.GetLength(0), noiseMap.GetLength(1)];
        for (int x = 0; x < downs.GetLength(0); x++)
        {
            for (int y = 0; y < downs.GetLength(1); y++)
            {
                // This should set the edges' downs to -2, and set the rest to -1 before checking neighbours and reassigning best to the lowest neighbour.
                if (x == 0 | y == 0 | x == downs.GetLength(0) - 1 | y == downs.GetLength(1) - 1)
                {
                    downs[x, y] = new Vector2(-2, -2);
                    // Debug.Log("downs" + "[" + x + ", " + y + "]: " + downs[x, y]);
                }
                else
                {
                    Vector2 best = new Vector2(-1, -1);
                    float besth = noiseMap[x, y];
                    // This should find all 9 nodes in a 3 x 3 square around the given node, and exclude the central one.
                    for (int i = -1; i < 2; i++)
                    {
                        for (int j = -1; j < 2; j++)
                        {
                            if (i != 0 | j != 0)
                            {
                                if (x + i >= 0 && y + j >= 0 && x + i < downs.GetLength(0) - 1 && y + j < downs.GetLength(1) - 1)
                                {
                                    if (noiseMap[x + i, y + j] < besth)
                                    {
                                        besth = noiseMap[x + i, y + j];
                                        best = new Vector2(x + i, y + j);
                                    }
                                }
                            }
                        }
                    }
                    downs[x, y] = best;
                }
            }
        }
        return downs;
    }

    // Couldn't find what this is used for.
    public static Vector2[,] FindSinks(float[,] noiseMap)
    {
        Vector2[,] dh = Downhill(noiseMap);

        for (int x = 0; x < dh.GetLength(0); x++)
        {
            for (int y = 0; y < dh.GetLength(1); y++)
            {
                if (x == 0 | y == 0 | x == dh.GetLength(0) - 1 | y == dh.GetLength(1) - 1)
                {
                    // downs[x, y] = new Vector2(-2, -2);
                    // Debug.Log("downs" + "[" + x + ", " + y + "]: " + downs[x, y]);
                }
                else
                {
                }
            }
        }

        return dh;
    }

    public static float[,] WaterErosion(float[,] noiseMap)
    {
        float[,] erodedHeightMap = GenerateZeroMap(noiseMap.GetLength(0), noiseMap.GetLength(1));

        // Start by finding water depressions and filling sinks using Planchon-Darboux algorithm.

        float epsilon = 0.00001f;
        var infinity = 15f;

        bool loop = true;
        int maxLoops = 24;
        int numLoops = 0;

        // I'm using this integer to shrink the number of vertices to erode.
        int testShrink = 0;

        for (int x = 0; x < erodedHeightMap.GetLength(0); x++)
        {
            for (int y = 0; y < erodedHeightMap.GetLength(1); y++)
            {
                // This should set the edges to the values from the noiseMap, and set the rest to "infinity", which should be an arbitrarily high ceiling.
                if (x == 0 | y == 0 | x == erodedHeightMap.GetLength(0) - 1 | y == erodedHeightMap.GetLength(1) - 1)
                {
                    erodedHeightMap[x, y] = noiseMap[x, y];
                    // Debug.Log("erodedHeightMap" + "[" + x + ", " + y + "]: " + erodedHeightMap[x, y]);
                } else
                {
                    erodedHeightMap[x, y] = infinity;
                }
            }
        }

        while (loop)
        // for (int n = 0; n < maxLoops; n++)
        {
            // This should just be a while(true) loop without this for loop, but I'm trying to debug what's causing Unity to crash.
            // numLoops++;
            bool changed = false;
            for (int x = 0; x < erodedHeightMap.GetLength(0) - testShrink; x++)
            {
                for (int y = 0; y < erodedHeightMap.GetLength(1) - testShrink; y++)
                {
                    if (erodedHeightMap[x, y] == noiseMap[x, y])
                    {
                        continue;
                    }

                    // This should find all 9 nodes in a 3 x 3 square around the given node, and exclude the central one.
                    for (int i = -1; i < 2; i++)
                    {
                        for (int j = -1; j < 2; j++)
                        {
                            if (i != 0 | j != 0)
                            {
                                if (x + i >= 0 && y + j >= 0 && x + i < erodedHeightMap.GetLength(0) - 1 && y + j < erodedHeightMap.GetLength(1) - 1)
                                {
                                    float neighbourHeight = erodedHeightMap[x + i, y + j] + epsilon;

                                    if (noiseMap[x, y] >= neighbourHeight)
                                    {
                                        erodedHeightMap[x, y] = noiseMap[x, y];
                                        changed = true;
                                        break;
                                    }
                                    if ((erodedHeightMap[x, y] > neighbourHeight) && (neighbourHeight > noiseMap[x, y]))
                                    {
                                        erodedHeightMap[x, y] = neighbourHeight;
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // if (numLoops >= maxLoops) { changed = false; }
            // Debug.Log("erodedHeightMap.GetLength(0): " + erodedHeightMap.GetLength(0));

            if (!changed) return erodedHeightMap;
        }

        return erodedHeightMap;
    }

}

public struct HeightMap {
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] values, float minValue, float maxValue)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}


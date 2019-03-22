using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomNoise : MonoBehaviour {

    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    int width = 256;

    int height = 256;

    float SampleCustomNoise(int x, int y)
    {
        return Mathf.PerlinNoise(x, y);
    }

    public void DrawTexture(Texture2D texture)
    {

        Debug.Log("Drawing custom noise texture.");
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        textureRender.gameObject.SetActive(true);
        // meshFilter.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        float[,] values = new float[width, height];

        float maxValue = 0;

        float minValue = 0;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] = SampleCustomNoise(i, j);

                if (values[i, j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue)
                {
                    minValue = values[i, j];
                }
            }
        }

        HeightMap heightMap = new HeightMap(values, minValue, maxValue);
        DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class perlinTest : MonoBehaviour
{
    public ComputeShader compute;
    public RenderTexture perlinTexture;
    public float slice;

    public int numPoints;

    private void Start()
    {
        makeRenderTexture();
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        slice += Time.deltaTime/50;
        slice = slice % 255;
        makeRenderTexture();
        Graphics.Blit(perlinTexture, destination);
    }

    void makeRenderTexture()
    {
        perlinTexture = new RenderTexture(256, 256, 24);
        perlinTexture.enableRandomWrite = true;
        perlinTexture.Create();

        compute.SetTexture(0, "Result", perlinTexture);
        ComputeBuffer b = CreatePerlinNoiseBuffer(numPoints, "grid");
        compute.SetInt("resolution", 256);
        compute.SetInt("numPoints", numPoints);
        compute.SetFloat("slice", slice);

        compute.Dispatch(0, perlinTexture.width / 8, perlinTexture.height / 8, 1);
        b.Release();
    }

    ComputeBuffer CreatePerlinNoiseBuffer(int numPoints, string bufferName)
    {
        Vector3[] gradients = new Vector3[numPoints * numPoints * numPoints];
        for (int i = 0; i < gradients.Length; i++)
        {
            gradients[i] = Random.onUnitSphere;

        }
        
        return CreateBuffer(gradients, sizeof(float) * 3, bufferName);

    }
    ComputeBuffer CreateBuffer(System.Array data, int stride, string name, int kernal = 0)
    {
        ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
        buffer.SetData(data);
        compute.SetBuffer(0, name, buffer);
        return buffer;
    }


}

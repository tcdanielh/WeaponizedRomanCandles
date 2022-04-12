using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader perlinCompute;
    public RenderTexture perlinTexture;
    public ComputeShader slicer;
    public RenderTexture slice;

    void makeRenderTexture()
    {
        perlinTexture = new RenderTexture(256, 256, 0);
        perlinTexture.volumeDepth = 256;
        perlinTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        perlinTexture.enableRandomWrite = true;
        perlinTexture.Create();

        perlinCompute.SetTexture(0, "Result", perlinTexture);
        ComputeBuffer b = CreatePerlinNoiseBuffer(256, "grid");
        perlinCompute.SetInt("resolution", 256);

        float[] minMaxData = new float[2];
        ComputeBuffer minMaxBuffer = CreateBuffer(minMaxData, sizeof(float), "minMax", 0);

        perlinCompute.Dispatch(0, perlinTexture.width / 8, perlinTexture.height / 8, 256 / 8);

        perlinCompute.SetBuffer(1,"minMax", minMaxBuffer);
        perlinCompute.SetTexture(1, "Result", perlinTexture);
        minMaxBuffer.Release();
        b.Release();
    }

    void takeSlice(int s)
    {
        slice = new RenderTexture(256, 256, 1);
        slice.enableRandomWrite = true;
        slice.Create();
        slicer.SetTexture(0, "Result", slice);
        slicer.SetTexture(0, "volume", perlinTexture);
        slicer.SetFloat("resolution", 256);
        slicer.SetInt("layer", s);
        int numThreadGroups = Mathf.CeilToInt(perlinTexture.width / 8);
        slicer.Dispatch(0, numThreadGroups, numThreadGroups, 1);
    }

    private void Start()
    {
       // makeRenderTexture();
        //takeSlice(1);
    }

    ComputeBuffer CreatePerlinNoiseBuffer(int numPoints, string bufferName)
    {
        Vector3[] gradients = new Vector3[numPoints * numPoints * numPoints];
        for (int i = 0; i < gradients.Length; i++)
        {
            gradients[i] = Random.onUnitSphere;

        }
        perlinCompute.SetInt("numPoints", numPoints);
        return CreateBuffer(gradients, sizeof(float) * 3, bufferName);
        
    }
    ComputeBuffer CreateBuffer(System.Array data, int stride, string name, int kernal = 0)
    {
        ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
        buffer.SetData(data);
        perlinCompute.SetBuffer(kernal, name, buffer);
        return buffer;
    }

    public RenderTexture getPerlinTexture()
    {
        if (perlinTexture == null) makeRenderTexture();
        return perlinTexture;
    }

    //private void OnRenderImage(RenderTexture source, RenderTexture destination)
    //{
    //    if (perlinTexture == null) { 
    //        makeRenderTexture(); 
    //    }
    //    if (slice == null) takeSlice(10);
    //    Graphics.Blit(slice, destination);
    //}
}

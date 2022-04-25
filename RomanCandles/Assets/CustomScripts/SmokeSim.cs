using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSim : MonoBehaviour
{
    //public Texture3D smokeDensity; //desnity read
    public RenderTexture smokeDensity; //density read
    public RenderTexture smokeDensityWrite; //density write
    public ComputeShader impulseShader;
    public float impulseRadius;
    public float time;
    public int size;

    //other buffer variables go here
    // Start is called before the first frame update

    void Start()
    {
        size = 256;

        impulseRadius = 100.0f;

        time = 0.0f;

        smokeDensity = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        smokeDensity.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        smokeDensity.volumeDepth = size;
        smokeDensity.enableRandomWrite = true;
        smokeDensity.Create();

        smokeDensityWrite = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        smokeDensityWrite.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        smokeDensityWrite.volumeDepth = size;
        smokeDensityWrite.enableRandomWrite = true;
        smokeDensityWrite.Create();

        
        

        /*
        int size = 256;        

        TextureFormat format = TextureFormat.RGBA32;
        TextureWrapMode wrapMode =  TextureWrapMode.Clamp;

        // Create the texture and apply the configuration
        smokeDensity = new Texture3D(size, size, size, format, false);
        smokeDensity.wrapMode = wrapMode;

        //create render texture
        smokeDensityWrite = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        smokeDensityWrite.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        smokeDensityWrite.volumeDepth = size;
        smokeDensityWrite.enableRandomWrite = true;
        smokeDensityWrite.Create();

        //apply impulse
        int kernelHandle = impulseShader.FindKernel("Impulse");
        impulseShader.SetTexture(kernelHandle, "Result", smokeDensityWrite);
        impulseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);


        Texture3D dest = new Texture3D(size, size, size, format, false);
        dest.Apply(false);
        Graphics.CopyTexture(smokeDensityWrite, dest);
        //smokeDensity = dest;

        //smokeDensity.SetPixels(smokeDensityWrite);
        smokeDensity.Apply();
        */
    }

    void FixedUpdate()
    {
        Vector4 impulsePosition = new Vector4(9.0f, 9.0f, 11.0f, 0.0f);

        Vector4 textureSize = new Vector4(size, size, size, 0.0f);


        //apply impulse
        int kernelHandle = impulseShader.FindKernel("Impulse");
        impulseShader.SetTexture(kernelHandle, "Result", smokeDensity);
        impulseShader.SetFloat("time", time);
        impulseShader.SetFloat("radius", impulseRadius);
        impulseShader.SetVector("position", impulsePosition);
        impulseShader.SetVector("size", textureSize);
        impulseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);

        time += 0.001f;

    }
    /*
    void Start()
    {
        int size = 64;

        TextureFormat format = TextureFormat.RGBA32;
        TextureWrapMode wrapMode =  TextureWrapMode.Clamp;

        // Create the texture and apply the configuration
        smokeDensity = new Texture3D(size, size, size, format, false);
        smokeDensity.wrapMode = wrapMode;

        Texture3D texture = new Texture3D(size, size, size, format, false);
        texture.wrapMode = wrapMode;

        // Create a 3-dimensional array to store color data
        Color[] colors = new Color[size * size * size];

        // Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
        float inverseResolution = 1.0f / (size - 1.0f);
        for (int z = 0; z < size; z++)
        {
            int zOffset = z * size * size;
            for (int y = 0; y < size; y++)
            {
                int yOffset = y * size;
                for (int x = 0; x < size; x++)
                {
                    colors[x + yOffset + zOffset] = new Color(0,
                        0, 0);
                }
            }
        }

        // Copy the color values to the texture
        texture.SetPixels(colors);

        smokeDensity.SetPixels(colors);

        // Apply the changes to the texture and upload the updated texture to the GPU
        texture.Apply(); 

        smokeDensity.Apply();
    }

    // Update is called once per frame

    float t = 0.0f;
    void FixedUpdate()
    {
        //physics updates (i.e. calls to the compute shaders) go here

        int size = 64;


        // Create a 3-dimensional array to store color data
        Color[] colors = new Color[size * size * size];

        // Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
        float inverseResolution = 1.0f / (size - 1.0f);
        for (int z = 0; z < size; z++)
        {
            int zOffset = z * size * size;
            for (int y = 0; y < size; y++)
            {
                int yOffset = y * size;
                for (int x = 0; x < size; x++)
                {
                    colors[x + yOffset + zOffset] = new Color(x * inverseResolution * t,
                        y * inverseResolution * t, z * inverseResolution * t, 1.0f);
                }
            }
        }

        // Copy the color values to the texture
        smokeDensity.SetPixels(colors);

        // Apply the changes to the texture and upload the updated texture to the GPU
        smokeDensity.Apply();
        t += 0.01f;
    }
    */

}

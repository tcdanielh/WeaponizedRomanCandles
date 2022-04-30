using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSim : MonoBehaviour
{
    public RenderTexture smokeDensity; //density read
    public RenderTexture smokeDensityWrite; //density write
    public ComputeShader impulseShader;
    public float impulseRadius;
    public float time;
    public int size;

    Vector4 impulsePosition; 


    public ComputeShader diffuseShader;


    public float StepTime = 0.2f;
    float lastUpdate = 0.0f;

    //other buffer variables go here
    // Start is called before the first frame update

    void Start()
    {
        size = 256;

        impulseRadius = 90.0f;

        impulsePosition = new Vector4(9.0f, 9.0f, 13.0f, 0.0f);

        time = 0.0f;

        smokeDensity = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        smokeDensity.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        smokeDensity.volumeDepth = size;
        smokeDensity.enableRandomWrite = true;
        smokeDensity.Create();

        smokeDensityWrite = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        smokeDensityWrite.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        smokeDensityWrite.volumeDepth = size;
        smokeDensityWrite.enableRandomWrite = true;
        smokeDensityWrite.Create();  

        Vector4 textureSize = new Vector4(size, size, size, 0.0f);


        AddDensity(impulsePosition, textureSize, impulseRadius);

      
    }

    void FixedUpdate()
    {                
        Vector4 textureSize = new Vector4(size, size, size, 0.0f);   

        
        if(lastUpdate > StepTime) {
             //diffuse smoke
            int kernelHandle = diffuseShader.FindKernel("Diffuse");
            diffuseShader.SetTexture(kernelHandle, "Result", smokeDensityWrite);
            diffuseShader.SetTexture(kernelHandle, "Prev", smokeDensity);
            diffuseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);

            RenderTexture temp = smokeDensity;
            smokeDensity = smokeDensityWrite;
            smokeDensityWrite = temp;
            lastUpdate = 0.0f;
        }
        
    
        lastUpdate += Time.deltaTime;

    }

    public void AddDensity(Vector4 position, Vector4 textureSize, float radius) {
        //apply impulse
        int kernelHandle = impulseShader.FindKernel("Impulse");
        impulseShader.SetTexture(kernelHandle, "Result", smokeDensity);
        impulseShader.SetFloat("radius", radius);
        impulseShader.SetVector("position", position);
        impulseShader.SetVector("size", textureSize);
        impulseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSim : MonoBehaviour
{
    public RenderTexture[] smokeDensity;
    public RenderTexture[] velocity;
    public RenderTexture[] temperature;
    public RenderTexture[] pressure;
    public ComputeShader impulseShader, advectShader, buoyancyShader;

    public static int READ = 0;
    public static int WRITE = 1;
    public Vector4 textureSize;
    public int size;

    public float impulseRadius, impulsePower;
    public Vector4 impulsePosition;
    

    public float buoyancy, tempAmbient, k, dissipation;

    //other buffer variables go here
    // Start is called before the first frame update

    public float StepTime = 0.2f;
    float lastUpdate = 0.0f;

    void Start()
    {
        size = 512;
        textureSize = new Vector4(size, size, size, 0.0f);

        smokeDensity = new RenderTexture[2];
        velocity = new RenderTexture[2];
        temperature = new RenderTexture[2];
        pressure = new RenderTexture[2];

        for (int i = 0; i < smokeDensity.Length; i++){
            smokeDensity[i] = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            smokeDensity[i].dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            smokeDensity[i].volumeDepth = size;
            smokeDensity[i].enableRandomWrite = true;
            smokeDensity[i].wrapMode = TextureWrapMode.Clamp;
            smokeDensity[i].Create();

            velocity[i] = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            velocity[i].dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            velocity[i].volumeDepth = size;
            velocity[i].enableRandomWrite = true;
            velocity[i].wrapMode = TextureWrapMode.Clamp;
            velocity[i].Create();

            temperature[i] = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            temperature[i].dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            temperature[i].volumeDepth = size;
            temperature[i].enableRandomWrite = true;
            temperature[i].wrapMode = TextureWrapMode.Clamp;
            temperature[i].Create();

            
            pressure[i] = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            pressure[i].dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            pressure[i].volumeDepth = size;
            pressure[i].enableRandomWrite = true;
            pressure[i].wrapMode = TextureWrapMode.Clamp;
            pressure[i].Create();
            
        }
        
        //debugging values
        impulsePosition = new Vector4(9.0f, 9.0f, 13.0f, 0.0f);
        impulseRadius = 7.0f;//90.0f;//7.0f;//6.0f;
        impulsePower = 20.0f;//40.0f;
        buoyancy = 10.0f;
        tempAmbient = 30.0f;
        dissipation = 0.97f;
        k = 10000000.0f;
        AddDensity(impulsePosition, textureSize, impulseRadius, impulsePower);
        ApplyForce(impulsePosition, textureSize, impulseRadius, impulsePower, .1f);
        AddTemperature(impulsePosition, textureSize, impulseRadius, impulsePower / 80);
         
    }

    void FixedUpdate()
    {     
     
        float dt = Time.deltaTime;
        if(lastUpdate > StepTime) {
            //AdvectDensity(textureSize, dissipation, dt);
            //ApplyBuoyancy(buoyancy, tempAmbient, dt);
            lastUpdate = 0.0f;
        }
        lastUpdate += Time.deltaTime;
        
        

    }

    public void AddDensity(Vector4 position, Vector4 textureSize, float radius, float power) {
        int kernelHandle = impulseShader.FindKernel("AddDensity");
        impulseShader.SetTexture(kernelHandle, "Prev", smokeDensity[READ]);
        impulseShader.SetTexture(kernelHandle, "Result", smokeDensity[WRITE]);
        impulseShader.SetFloat("radius", radius);
        impulseShader.SetFloat("power", power);
        impulseShader.SetVector("position", position);
        impulseShader.SetVector("size", textureSize);
        impulseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
        Switch(smokeDensity);
    }

    public void AddTemperature(Vector4 position, Vector4 textureSize, float radius, float power) {
        int kernelHandle = impulseShader.FindKernel("AddDensity");
        impulseShader.SetTexture(kernelHandle, "Prev", temperature[READ]);
        impulseShader.SetTexture(kernelHandle, "Result", temperature[WRITE]);
        impulseShader.SetFloat("radius", radius);
        impulseShader.SetFloat("power", power);
        impulseShader.SetVector("position", position);
        impulseShader.SetVector("size", textureSize);
        impulseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
        Switch(temperature);
    }


    public void ApplyForce(Vector4 position, Vector4 textureSize, float radius, float force, float dt) {
        int kernelHandle = impulseShader.FindKernel("ApplyForce");
        impulseShader.SetTexture(kernelHandle, "Prev", velocity[READ]);
        impulseShader.SetTexture(kernelHandle, "Result", velocity[WRITE]);
        impulseShader.SetFloat("radius", radius);
        impulseShader.SetFloat("force", force);
        impulseShader.SetFloat("dt", dt);
        impulseShader.SetVector("position", position);
        impulseShader.SetVector("size", textureSize);
        impulseShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
        Switch(velocity);
    }

    public void ApplyBuoyancy(float buoyancy, float tempAmbient, float dt) {
        int kernelHandle = buoyancyShader.FindKernel("ApplyBuoyancy");
        buoyancyShader.SetTexture(kernelHandle, "Prev", velocity[READ]);
        buoyancyShader.SetTexture(kernelHandle, "Result", velocity[WRITE]);
        buoyancyShader.SetTexture(kernelHandle, "Temperature", temperature[READ]);
        buoyancyShader.SetTexture(kernelHandle, "Density", smokeDensity[READ]);
        buoyancyShader.SetFloat("tempAmbient", tempAmbient);
        buoyancyShader.SetFloat("buoyancy", buoyancy);
        buoyancyShader.SetFloat("k", k);
        buoyancyShader.SetFloat("dt", dt);
        //impulseShader.SetVector("size", textureSize);
        buoyancyShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
        Switch(velocity);
    }

    public void AdvectDensity(Vector4 textureSize, float dissipation, float dt) {
        int kernelHandle = advectShader.FindKernel("AdvectDensity");
        advectShader.SetTexture(kernelHandle, "Prev", smokeDensity[READ]);
        advectShader.SetTexture(kernelHandle, "Result", smokeDensity[WRITE]);
        advectShader.SetTexture(kernelHandle, "Velocity", velocity[READ]);
        advectShader.SetVector("size", textureSize);
        advectShader.SetFloat("dt", dt);
        advectShader.SetFloat("dissipation", dissipation);
        advectShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
        Switch(smokeDensity);
    }

    void Switch(RenderTexture[] textures) {
        RenderTexture temp = textures[READ];
        textures[READ] = textures[WRITE];
        textures[WRITE] = temp; 
    }
}
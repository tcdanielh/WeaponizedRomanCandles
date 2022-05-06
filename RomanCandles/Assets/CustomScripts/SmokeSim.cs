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

    public float StepTime = 5.0f;
    float lastUpdate = 0.0f;

    public int jacobiIterations;

    [SerializeField] Vector3 smokeGridCells;
    [SerializeField] Vector3 gridMin;
    [SerializeField] float cellSize;

    bool wind;

    public void SmokeStart()
    {
        gridMin = GetComponent<ScreenWriter>().gridMinPoint;
        smokeGridCells = GetComponent<ScreenWriter>().smokeGridCells;
        cellSize = GetComponent<ScreenWriter>().SmokeCellSideLength;
        //size = 512;
        size = Mathf.CeilToInt(Mathf.Max(smokeGridCells.x, Mathf.Max( smokeGridCells.y, smokeGridCells.z)));
        textureSize = new Vector4(size, size, size, 0.0f);

        smokeDensity = new RenderTexture[2];
        velocity = new RenderTexture[2];
        temperature = new RenderTexture[2];
        pressure = new RenderTexture[2];
        
        print(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32));

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
        //impulsePosition = new Vector4(9.0f, 9.0f, 13.0f, 0.0f);
        impulseRadius = 9.0f;//90.0f;//7.0f;//6.0f;
        impulsePower = 20.0f;//40.0f;
        buoyancy = 100000.0f;
        tempAmbient = 1000.0f;
        dissipation = 50.2f;
        k = 0.0f;
        jacobiIterations = 20;



        //relative position
        impulsePosition = localPosition(impulsePosition);
        //
        float dt=0.0f;
        //AddDensity(impulsePosition, textureSize, impulseRadius, impulsePower);
        //ApplyBuoyancy(buoyancy, tempAmbient, dt);
        ApplyForce(impulsePosition, textureSize, impulseRadius, impulsePower, dt);
        //AddTemperature(impulsePosition, textureSize, impulseRadius, impulsePower);
         
    }

    Vector4 localPosition(Vector4 p)
    {
        p = p - new Vector4(gridMin.x, gridMin.y, gridMin.z);
        p /= cellSize;
        return new Vector4(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z));
    }
         
    void FixedUpdate()
    {     
     /*
        float dt = Time.deltaTime;

        if(lastUpdate > StepTime) {
            //AdvectDensity(textureSize, dissipation, dt);
            //ApplyBuoyancy(buoyancy, tempAmbient, dt);
            lastUpdate = 0.0f;
        }
        lastUpdate += Time.fixedDeltaTime;
        //Debug.Log(lastUpdate);
        
       */
        float dt = 0.0f;
        //AdvectDensity(textureSize, dissipation, dt); 

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) wind = !wind;
        float dt = 0.0f;
        JacobiDiffusion(dissipation, jacobiIterations);
        //AddDensity(impulsePosition, textureSize, impulseRadius, impulsePower);
        if (wind)
            AdvectDensity(textureSize, dissipation, dt); 
       //Time.deltaTime;
        //ApplyBuoyancy(buoyancy, tempAmbient, dt);
       
      
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
        buoyancyShader.SetVector("size", textureSize);
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

    public void JacobiDiffusion(float dissipation, int numIterations) {
        int kernelHandle = advectShader.FindKernel("JacobiDiffusion");
        advectShader.SetFloat("dissipation", dissipation);
        for(int i = 0; i < numIterations; i += 1) {
            advectShader.SetTexture(kernelHandle, "Prev", smokeDensity[READ]);
            advectShader.SetTexture(kernelHandle, "Result", smokeDensity[WRITE]);
            advectShader.Dispatch(kernelHandle, (int) size / 8, (int) size / 8, (int) size / 8);
            Switch(smokeDensity);
        }
    }

    void Switch(RenderTexture[] textures) {
        RenderTexture temp = textures[READ];
        textures[READ] = textures[WRITE];
        textures[WRITE] = temp; 
    }

    public void MakeSmokeAtPoint(Vector3 pos, float rad, float pow)
    {
        AddDensity(localPosition(new Vector4(pos.x, pos.y, pos.z)), textureSize, rad, pow);
    }

    public void MakeTrials(ScreenWriter.Ejecta[] es, float rad, float pow)
    {
        Debug.Log("making trails");
        int kernelHandle = impulseShader.FindKernel("CreateTrails");
        impulseShader.SetTexture(kernelHandle, "Prev", smokeDensity[READ]);
        impulseShader.SetTexture(kernelHandle, "Result", smokeDensity[WRITE]);
        impulseShader.SetTexture(kernelHandle, "Temperature", temperature[WRITE]);
        impulseShader.SetFloat("power", pow);
        impulseShader.SetFloat("radius", rad);

        ComputeBuffer ejectaBuffer = new ComputeBuffer(es.Length, ScreenWriter.EjectaSize);
        ejectaBuffer.SetData(es);

        impulseShader.SetBuffer(kernelHandle, "Ejectas", ejectaBuffer);
        impulseShader.SetVector("gridMin", gridMin);
        impulseShader.SetFloat("cellSize", cellSize);
        impulseShader.SetVector("size", textureSize);
        impulseShader.Dispatch(kernelHandle, ejectaBuffer.count / 10, 1, 1);
        ejectaBuffer.Release();
        Switch(smokeDensity);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenWriter : MonoBehaviour
{
    [SerializeField] EjectaHandler ejectaHandler;

    public Color smokeColor;
    public float SmokeCellSideLength;
    public Vector3 SmokeGridDimensions;
    public Vector3 smokeGridCells;

    public float hashBinSideLength;
    public int EjectaPerBin;
    public Vector3 gridMinPoint;
    int[] binsPerAxis;

    public Shader shader;
    public ComputeShader EjectaHasher;
    public Transform container;
    Material material;
    public Transform lightPoint;
    public float lRadius;
    public float smokeLightAbsorb;
    public float scale;

    [SerializeField] ComputeShaderTest smoke;

    private Camera cam;

    [SerializeField] List<Transform> tempEjecta;

    [SerializeField] float ejectaLightIntensity;
    public Color ejectaColor;
    public ComputeBuffer smokeBuffer;
    public ComputeBuffer ejectaBuffer;
    //public ComputeBuffer hashBuffer;
    

    public Transform sun;
    private Vector3 sunDir;
    private Vector4 sunColor;
    private float sunIntensity;

    public struct Ejecta{
        public Vector3 pos;
        public Vector3 v;
        public Vector4 color;
        //public bool landed; //TODO make second, simple struct that can be sent to shader (bools dont work)
        public int landed; //0 = false, 1 = true. Compute buffers don't like bools for some reason
    }


    public static int EjectaSize = (sizeof(float) * 10) + sizeof(int);

    Ejecta[] es = new Ejecta[0];

    private SmokeSim smokeSim;
    private FireworkSim fireworkSim;
    private void Start()
    {
        EjectaSize = (sizeof(float) * 10) + sizeof(int);

        smokeSim = GetComponent<SmokeSim>();
        fireworkSim = GetComponent<FireworkSim>();
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        smokeGridCells = SmokeGridDimensions / SmokeCellSideLength;

        int s = Mathf.CeilToInt(SmokeGridDimensions.x * SmokeGridDimensions.y * SmokeGridDimensions.z / hashBinSideLength);
        //hashBuffer = new ComputeBuffer(s * EjectaPerBin, EjectaSize);

        gridMinPoint = new Vector3(-SmokeGridDimensions.x / 2, 0, -SmokeGridDimensions.z / 2);
        binsPerAxis = new int[] { Mathf.CeilToInt(SmokeGridDimensions.x / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength) };

        GetComponent<SmokeSim>().SmokeStart();
    }

    private void Update()
    {
        Debug.Log("there are currently " + ejectaHandler.getEjectas().Length + " ejecta in the scene");
        sunColor = sun.GetComponent<Light>().color;
        sunDir = -sun.forward;
        sunIntensity = sun.GetComponent<Light>().intensity;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
       // es = GetComponent<FireworkSim>().es;
        es = ejectaHandler.getEjectas();
        if (es != null && es.Length > 0)
        {
            ejectaBuffer = new ComputeBuffer(es.Length, EjectaSize);
            ejectaBuffer.SetData(es);
        }
        
        //Ejecta Hashing
        //hashBuffer.SetData(new Ejecta[hashBuffer.count]);
        RenderTexture hash = new RenderTexture(binsPerAxis[0], binsPerAxis[1], 0);
        hash.volumeDepth = binsPerAxis[2];
        hash.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        hash.enableRandomWrite = true;
        hash.Create();
        
        RenderTexture hashC = new RenderTexture(binsPerAxis[0], binsPerAxis[1], 0);
        hashC.volumeDepth = binsPerAxis[2];
        hashC.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        hashC.enableRandomWrite = true;
        hashC.Create();
        

        EjectaHasher.SetInts("binsPerAxis", binsPerAxis);
        EjectaHasher.SetVector("gridMin", gridMinPoint);
        EjectaHasher.SetVector("gridSize", SmokeGridDimensions);
        EjectaHasher.SetFloat("binLength", hashBinSideLength);
        EjectaHasher.SetInt("binSize", EjectaPerBin);
        EjectaHasher.SetBuffer(0, "Ejectas", ejectaBuffer);
        //EjectaHasher.SetBuffer(0, "Hash", hashBuffer);
        EjectaHasher.SetTexture(1, "hashC", hashC);
        EjectaHasher.SetTexture(1, "hash", hash);
        EjectaHasher.Dispatch(1, hash.width / 8, hash.height / 8, hash.volumeDepth / 8);

        EjectaHasher.SetTexture(0, "hashC", hashC);
        EjectaHasher.SetTexture(0, "hash", hash);
        EjectaHasher.Dispatch(0, ejectaBuffer.count / 10, 1, 1);
        ejectaBuffer.Release();

        //Debug Hash
        //Ejecta[] d = new Ejecta[hashBuffer.count];
        //hashBuffer.GetData(d);
        //foreach(Ejecta e in d)
        //{
        //    Debug.Log(e.pos);
        //}

        //Fragment shader
        if (material == null)
        {
            material = new Material(shader);
        }


        material.SetTexture("EHash", hash);
        material.SetTexture("EHashC", hashC);
        material.SetVector("binsPerAxis", new Vector4(binsPerAxis[0], binsPerAxis[1], binsPerAxis[2], 0));
        material.SetVector("gridMin", gridMinPoint);
        material.SetVector("gridSize", SmokeGridDimensions);
        material.SetFloat("binLength", hashBinSideLength);
        material.SetFloat("smokeCellSize", SmokeCellSideLength);
        material.SetFloat("smokeCellSize", SmokeCellSideLength);
        material.SetFloat("lRadius", lRadius);
        material.SetVector("BoundsMin", container.position - container.localScale / 2);
        material.SetVector("BoundsMax", container.position + container.localScale / 2);
        //material.SetTexture("Shape", smoke.getPerlinTexture());
        material.SetTexture("Shape", smokeSim.smokeDensity[0]);
        material.SetInt("numSteps", 20);
        material.SetInt("numStepsLight", 20);
        material.SetVector("lPos", lightPoint.position);
        material.SetVector("lColor", ejectaColor);
        material.SetFloat("lIntensity", ejectaLightIntensity);
        material.SetFloat("smokeLightAbsorb", smokeLightAbsorb);

        //Sun stuff
        material.SetVector("sunColor", sunColor);
        material.SetVector("sunDir", sunDir);
        material.SetFloat("sunIntensity", sunIntensity);
        material.SetVector("smokeColor", smokeColor);

        Graphics.Blit(source, destination, material);

        hash.Release();
        hashC.Release();

    }
    
    
}

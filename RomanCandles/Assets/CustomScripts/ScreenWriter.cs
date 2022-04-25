using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenWriter : MonoBehaviour
{
    [SerializeField] float eyeOffset;
    public Color smokeColor;
    public float SmokeCellSideLength;
    public Vector3 SmokeGridDimensions;

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
    public ComputeBuffer hashBuffer;
    

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


    public int EjectaSize = (sizeof(float) * 10) + sizeof(int);

    private SmokeSim smokeSim;
    private FireworkSim fireworkSim;



    Matrix4x4 left_world_from_view;
    Matrix4x4 right_world_from_view;

    // Both stereo eye inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
    Matrix4x4 left_screen_from_view;
    Matrix4x4 right_screen_from_view;
    Matrix4x4 left_view_from_screen;
    Matrix4x4 right_view_from_screen;
    private void Start()
    {
        EjectaSize = (sizeof(float) * 10) + sizeof(int);

        smokeSim = GetComponent<SmokeSim>();
        fireworkSim = GetComponent<FireworkSim>();
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        Vector3 smokeGridCells = SmokeGridDimensions / SmokeCellSideLength;
        float[] smokeData = new float[Mathf.CeilToInt(smokeGridCells.x * smokeGridCells.y * smokeGridCells.z)];
        for (int i = 0; i < smokeData.Length; i++)
        {
            smokeData[i] = Random.value;
        }
        smokeBuffer = new ComputeBuffer(smokeData.Length, sizeof(float));
        smokeBuffer.SetData(smokeData);

        // initialize ejecta
        Ejecta[] es = new Ejecta[10];
        for (int i = 0; i < es.Length; i++)
        {
            Ejecta e = new Ejecta();
            e.color = Random.ColorHSV();
            e.color.w = 0f;
            e.pos = Random.insideUnitSphere * 10;
            e.pos.y = Mathf.Abs(e.pos.y);
            es[i] = e;
        }
        ejectaBuffer = new ComputeBuffer(es.Length, EjectaSize);
        ejectaBuffer.SetData(es);

        int s = Mathf.CeilToInt(SmokeGridDimensions.x * SmokeGridDimensions.y * SmokeGridDimensions.z / hashBinSideLength);
        hashBuffer = new ComputeBuffer(s * EjectaPerBin, EjectaSize);

        gridMinPoint = new Vector3(-SmokeGridDimensions.x / 2, -SmokeGridDimensions.y / 2, 0);
        binsPerAxis = new int[] { Mathf.CeilToInt(SmokeGridDimensions.x / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength) };
    }

    private void Update()
    {
        sunColor = sun.GetComponent<Light>().color;
        sunDir = -sun.forward;
        sunIntensity = sun.GetComponent<Light>().intensity;
    }

    private void OnPreRender()
    {
        // Both stereo eye inverse view matrices
        left_world_from_view = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
        right_world_from_view = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;

        // Both stereo eye inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
        left_screen_from_view = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        right_screen_from_view = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
        left_view_from_screen = GL.GetGPUProjectionMatrix(left_screen_from_view, true).inverse;
        right_view_from_screen = GL.GetGPUProjectionMatrix(right_screen_from_view, true).inverse;

        // Negate [1,1] to reflect Unity's CBuffer state
        left_view_from_screen[1, 1] *= -1;
        right_view_from_screen[1, 1] *= -1;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Ejecta Hashing
        //hashBuffer.SetData(new Ejecta[hashBuffer.count]);
        RenderTexture hash = new RenderTexture(binsPerAxis[0], binsPerAxis[1], 0);
        hash.volumeDepth = binsPerAxis[2];
        hash.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        hash.enableRandomWrite = true;
        hash.Create();
        EjectaHasher.SetTexture(0, "hash", hash);
        RenderTexture hashC = new RenderTexture(binsPerAxis[0], binsPerAxis[1], 0);
        hashC.volumeDepth = binsPerAxis[2];
        hashC.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        hashC.enableRandomWrite = true;
        hashC.Create();
        EjectaHasher.SetTexture(0, "hashC", hashC);

        EjectaHasher.SetInts("binsPerAxis", binsPerAxis);
        EjectaHasher.SetVector("gridMin", gridMinPoint);
        EjectaHasher.SetVector("gridSize", SmokeGridDimensions);
        EjectaHasher.SetFloat("binLength", hashBinSideLength);
        EjectaHasher.SetInt("binSize", EjectaPerBin);
        EjectaHasher.SetBuffer(0, "Ejectas", ejectaBuffer);
        //EjectaHasher.SetBuffer(0, "Hash", hashBuffer);
        EjectaHasher.Dispatch(0, ejectaBuffer.count / 10, 1, 1);

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

        material.SetMatrix("_LeftWorldFromView", left_world_from_view);
        material.SetMatrix("_RightWorldFromView", right_world_from_view);
        material.SetMatrix("_LeftViewFromScreen", left_view_from_screen);
        material.SetMatrix("_RightViewFromScreen", right_view_from_screen);

        material.SetFloat("eyeOffset", eyeOffset);
        material.SetTexture("EHash", hash);
        material.SetTexture("EHashC", hashC);
        material.SetVector("binsPerAxis", new Vector4(binsPerAxis[0], binsPerAxis[1], binsPerAxis[2], 0));
        material.SetVector("gridMin", gridMinPoint);
        material.SetVector("gridSize", SmokeGridDimensions);
        material.SetFloat("binLength", hashBinSideLength);
        material.SetFloat("smokeScale", scale);
        material.SetFloat("lRadius", lRadius);
        material.SetVector("BoundsMin", container.position - container.localScale / 2);
        material.SetVector("BoundsMax", container.position + container.localScale / 2);
        material.SetTexture("Shape", smoke.getPerlinTexture());
        //material.SetTexture("Shape", smokeSim.smokeDensity);
        material.SetInt("numSteps", 5);
        material.SetInt("numStepsLight", 3);
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

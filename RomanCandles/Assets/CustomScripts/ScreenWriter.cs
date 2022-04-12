using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenWriter : MonoBehaviour
{
    public Shader shader;
    public ComputeShader ZeroBounceCompute;
    public ComputeShader DefualtCompute;
    public Transform container;
    Material material;
    public Transform lightPoint;
    public float smokeLightAbsorb;

    [SerializeField] ComputeShaderTest smoke;

    private Camera cam;

    [SerializeField] List<Transform> tempEjecta;

    [SerializeField] float ejectaLightIntensity;

    public struct Ejecta{
        public Vector3 pos;
        public Vector4 color;
    }

    public int EjectaSize = sizeof(float) * 7;

    public RenderTexture ZeroB;

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //0 bounce ejecta lights
        Ejecta[] es = new Ejecta[tempEjecta.Count];
        for (int i = 0; i < es.Length; i++)
        {
            Ejecta e = new Ejecta();
            e.color = tempEjecta[i].GetComponent<Light>().color;
            e.pos = tempEjecta[i].position;
            es[i] = e;
            //Debug.Log(e.pos);
        }
        ComputeBuffer ejectaBuffer = new ComputeBuffer(es.Length, EjectaSize);
        ejectaBuffer.SetData(es);

        ZeroB = new RenderTexture(source.width,source.height,source.depth);
        ZeroB.enableRandomWrite = true;
        ZeroB.Create();

        ZeroBounceCompute.SetTexture(0, "Result", ZeroB);
        ZeroBounceCompute.SetBuffer(0, "Ejectas", ejectaBuffer);
        ZeroBounceCompute.SetMatrix("w2s", cam.projectionMatrix * cam.worldToCameraMatrix);
        ZeroBounceCompute.SetInt("cWidth", source.width);
        ZeroBounceCompute.SetInt("cHeight", source.height);
        ZeroBounceCompute.Dispatch(0, es.Length,1, 1);
        
        ejectaBuffer.Release();

        //DefualtCompute.SetTexture(0, "Result", ZeroB);
        //DefualtCompute.Dispatch(0, ZeroB.width / 8, ZeroB.height / 8, 1);

        Debug.Log(ZeroB.width + " " + ZeroB.height + " " + source.width + " " + source.height);

        //Smoke (1+bounce)
        if (material == null)
        {
            material = new Material(shader);
        }

        material.SetTexture("ZeroB", ZeroB);
        material.SetVector("BoundsMin", container.position - container.localScale / 2);
        material.SetVector("BoundsMax", container.position + container.localScale / 2);
        material.SetTexture("Shape", smoke.getPerlinTexture());
        material.SetInt("numSteps", 20);
        material.SetInt("numStepsLight", 20);
        material.SetVector("lPos", lightPoint.position);
        material.SetVector("lColor", lightPoint.GetComponent<Light>().color);
        material.SetFloat("lIntensity", lightPoint.GetComponent<Light>().intensity);
        material.SetFloat("smokeLightAbsorb", smokeLightAbsorb);
        material.SetFloat("sWidth", source.width);
        material.SetFloat("sHeight", source.height);

        Graphics.Blit(source, destination, material);

        //ZeroB.Release(); 
    }
}

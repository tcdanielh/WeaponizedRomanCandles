using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenWriter : MonoBehaviour
{
    public Shader shader;
    public Transform container;
    Material material;
    public Transform lightPoint;
    public float smokeLightAbsorb;

    [SerializeField] ComputeShaderTest smoke;

    private Camera cam;

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //0 bounce ejecta lights
        //cam.WorldToScreenPoint

        //Smoke (1+bounce)
        if (material == null)
        {
            material = new Material(shader);
        }
        
        material.SetVector("BoundsMin", container.position - container.localScale / 2);
        material.SetVector("BoundsMax", container.position + container.localScale / 2);
        material.SetTexture("Shape", smoke.getPerlinTexture());
        material.SetInt("numSteps", 20);
        material.SetInt("numStepsLight", 20);
        material.SetVector("lPos", lightPoint.position);
        material.SetVector("lColor", lightPoint.GetComponent<Light>().color);
        material.SetFloat("lIntensity", lightPoint.GetComponent<Light>().intensity);
        material.SetFloat("smokeLightAbsorb", smokeLightAbsorb);

        Graphics.Blit(source, destination, material);
    }
}

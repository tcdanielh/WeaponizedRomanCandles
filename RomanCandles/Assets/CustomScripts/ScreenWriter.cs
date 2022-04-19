using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenWriter : MonoBehaviour
{
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

    public ComputeBuffer smokeBuffer;
    public ComputeBuffer ejectaBuffer;
    public ComputeBuffer hashBuffer;

    public struct Ejecta{
        public Vector3 pos;
        public Vector3 v;
        public Vector4 color;
        public bool landed = false;
    }


    public int EjectaSize = sizeof(float) * 7;

    private SmokeSim smokeSim;
    private FireworkSim fireworkSim;
    private void Start()
    {
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
            es[i] = e;
        }
        ejectaBuffer = new ComputeBuffer(es.Length, EjectaSize);
        ejectaBuffer.SetData(es);

        int s = Mathf.CeilToInt(SmokeGridDimensions.x * SmokeGridDimensions.y * SmokeGridDimensions.z / hashBinSideLength);
        hashBuffer = new ComputeBuffer(s * EjectaPerBin, EjectaSize);

        gridMinPoint = new Vector3(-SmokeGridDimensions.x / 2, -SmokeGridDimensions.y / 2, 0);
        binsPerAxis = new int[] { Mathf.CeilToInt(SmokeGridDimensions.x / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength) };
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Ejecta Hashing
        hashBuffer.SetData(new Ejecta[hashBuffer.count]);
        EjectaHasher.SetInts("binsPerAxis", binsPerAxis);
        EjectaHasher.SetVector("gridMin", gridMinPoint);
        EjectaHasher.SetVector("gridSize", SmokeGridDimensions);
        EjectaHasher.SetFloat("binLength", hashBinSideLength);
        EjectaHasher.SetInt("binSize", EjectaPerBin);
        EjectaHasher.SetBuffer(0, "Ejectas", ejectaBuffer);
        EjectaHasher.SetBuffer(0, "Hash", hashBuffer);
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

        material.SetFloat("smokeScale", scale);
        material.SetFloat("lRadius", lRadius);
        material.SetVector("BoundsMin", container.position - container.localScale / 2);
        material.SetVector("BoundsMax", container.position + container.localScale / 2);
        material.SetTexture("Shape", smoke.getPerlinTexture());
        //material.SetTexture("Shape", smokeSim.smokeDensity);
        material.SetInt("numSteps", 20);
        material.SetInt("numStepsLight", 20);
        material.SetVector("lPos", lightPoint.position);
        material.SetVector("lColor", lightPoint.GetComponent<Light>().color);
        material.SetFloat("lIntensity", lightPoint.GetComponent<Light>().intensity);
        material.SetFloat("smokeLightAbsorb", smokeLightAbsorb);;

        Graphics.Blit(source, destination, material);

    }
    
    // we could also include parameters for angles in spherical coordinates
    private void firework(Ejecta[] es)
    {   /* Physical Parameters */
        float g = 9.81f;    // gravitational acceleration [m/s]
        float eta = 0.05f;  // explosive efficiency
        float mu_a = 1.8e-5f;   // air viscosity [kg/(m*s)]
        float N_p = es.Length;
        float H_e = 3e6f;   // explosive heat of combustion [J/kg]
        float R_p = 0.005f; // projectile radius
        float R_r = .2f;    // firework radius
        float rho_a = 1.225f;   // air density [kg/m^3]
        float rho_p = 2000.f;    // projectile density [kg/m^3]
        Vector3 v_a = new Vector3(0,0,0);  // air velocity [m/s]
        float m_s = 2.f; // inert structural mass [kg]
        

        // options to change burst radius, flight time, accuracy
        float m_e = 5.f; // explosive charge mass [kg]
        float m_f = 10.f;    // launch charge mass [kg]
        float t_e = 5.f; // time of explsion [sec]
        float dt = 0.001f;  // time step [sec]

        float m_i = 4.f / 3.f * Mathf.PI * Mathf.Pow((float)R_p, 3) * rho_p;  // mass of ejecta 
        float m_p = N_p * m_i; // total mass of ejecta
        float A_px = Mathf.PI * Mathf.pow((float)R_p, 2);  // cross-sectional area of ejecta
        
        float m_r = m_p + m_e + m_s;   // total rocket mass [kg]
        Vector3 v_0 = new Vector3(0, 0, Mathf.Sqrt((float)(2 * eta * H_e * m_f / m_r)));    // initial velocity [m/s]

        /* Rocket Ascent */
        // TODO: mark time for when rocket is launched

        Vector3 F_gr = new Vector3(0., 0., -g * m_r);   // force of gravity on rocket [N]
        foreach (Ejecta e in es)
        {
            e.v = v_0;  // initial velocity at time = 0
        }
        int num_steps_ascent = t_e / dt; 
        float[] discrete_time_ascent = new float[num_steps_ascent];
        for (int i = 0; i < num_steps_ascent; i++)
        {
            discrete_time_ascent[i] = dt * (float) (i-1);
        }
        Vector3 F_drag = new Vector3(0.); 
        Vector3 psi_tot = new Vector3d(0.);
        // time stepping loop during ascent
        foreach (float dt_i in discrete_time_ascent) 
        {
            foreach (Ejecta e in es)
            {
                F_drag = 0.5f * Mathf.PI * Mathf.pow(R_r, 2) * coeff_drag(R_r, rho_a, e.v, v_a, mu_a) * 
                    (v_a - e.v).magnitude *  * (v_a - e.v);
                psi_tot = F_drag + F_gr;
                e.v = e.v + dt / m_r * psi_tot;
                e.pos = e.pos + dt * e.v;

                // TODO: use fixedUpdate method
            }

        }

        /* EXPLOSION!!! */
        Vector3 blast_origin = es[0].pos;
        float deltav = Mathf.Sqrt((float)(2 * eta * H_e * m_e / m_p));   // change in speed after detonation


    }
    
    private float coeff_drag(float radius, float rho_a, Vector3 v, Vector3 v_a, float mu_a)
    {
        float cd;
        float Re = (2f * radius * rho_a * (v - v_a).magnitude) / mu_a;
        if (Re > 2e6f)
        {
            cd = 0.18;
        } else if (Re <= 2e6f && Re > 3e5f)
        {
            cd = 3.66e-4f * Mathf.pow(Re, 0.4275f);
        } else if (Re <= 3e5f & Re > 400.f)
        {
            cd = 0.5f;
        } else if (Re <= 400f && Re > 1f)
        {
            cd = 24f * Mathf.pow(Re, -.646f);
        } else if (Re < 1f)
        {
            cd = 24.f / Re;
        }
        return cd;
    }
}

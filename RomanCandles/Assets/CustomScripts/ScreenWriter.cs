using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenWriter : MonoBehaviour
{
    [SerializeField] EjectaHandler ejectaHandler;

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

    Ejecta[] es = new Ejecta[0];

    private SmokeSim smokeSim;
    private FireworkSim fireworkSim;
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
        //Ejecta[] es = new Ejecta[10];
        //for (int i = 0; i < es.Length; i++)
        //{
        //    Ejecta e = new Ejecta();
        //    e.color = Random.ColorHSV();
        //    e.color.w = 0f;
        //    e.pos = Random.insideUnitSphere * 10;
        //    e.pos.y = Mathf.Abs(e.pos.y);
        //    es[i] = e;
        //}
        //ejectaBuffer = new ComputeBuffer(es.Length, EjectaSize);
        //ejectaBuffer.SetData(es);

        int s = Mathf.CeilToInt(SmokeGridDimensions.x * SmokeGridDimensions.y * SmokeGridDimensions.z / hashBinSideLength);
        hashBuffer = new ComputeBuffer(s * EjectaPerBin, EjectaSize);

        gridMinPoint = new Vector3(-SmokeGridDimensions.x / 2, -SmokeGridDimensions.y / 2, 0);
        binsPerAxis = new int[] { Mathf.CeilToInt(SmokeGridDimensions.x / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength), Mathf.CeilToInt(SmokeGridDimensions.y / hashBinSideLength) };
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
        material.SetFloat("smokeScale", scale);
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
    
    /* This function is a physics-based simulation of a fragmenting explosion from a firework.
    All ejecta travel together in a rocket prior to the explosion at time = t_e. 
    For simplicity, everything is a sphere. */
    // TODO: we could also include spherical coordinate parameters/angles
    //private void firework(Ejecta[] es)
    //{   /* Physical Parameters */
    //    float g = 9.81f;    // gravitational acceleration [m/s]
    //    float eta = 0.05f;  // explosive efficiency
    //    float mu_a = 1.8e-5f;   // air viscosity [kg/(m*s)]
    //    float N_p = es.Length;
    //    float H_e = 3e6f;   // explosive heat of combustion [J/kg]
    //    float R_p = 0.005f; // projectile radius
    //    float R_r = .2f;    // firework radius
    //    float rho_a = 1.225f;   // air density [kg/m^3]
    //    float rho_p = 2000f;    // projectile density [kg/m^3]
    //    Vector3 v_a = new Vector3(0f,0f,0f);  // air velocity [m/s]
    //    float m_s = 2f; // inert structural mass [kg]
    //    
    //
    //    // options to change burst radius, flight time, accuracy
    //    float m_e = 5f; // explosive charge mass [kg]
    //    float m_f = 10f;    // launch charge mass [kg]
    //    float t_e = 5f; // time of explsion [sec]
    //    float dt = 0.001f;  // time step [sec]
    //
    //    float m_i = 4f / 3f * Mathf.PI * Mathf.Pow((float)R_p, 3) * rho_p;  // mass of ejecta [kg]
    //    Vector3 F_gi = new Vector3(0f, 0f, m_i * -g);   // weight of ejecta [N]
    //    float m_p = N_p * m_i; // total mass of ejecta
    //    float A_px = Mathf.PI * Mathf.Pow((float)R_p, 2);  // cross-sectional area of ejecta
    //    
    //    float m_r = m_p + m_e + m_s;   // total rocket mass [kg]
    //    Vector3 v_0 = new Vector3(0, 0, Mathf.Sqrt((float)(2f * eta * H_e * m_f / m_r)));    // initial velocity [m/s]
    //
    //    /* Rocket Ascent */
    //    // TODO: mark time for when rocket is launched with in-game clock
    //
    //    Vector3 F_gr = new Vector3(0f, 0f, -g * m_r);   // force of gravity on rocket [N] along -z
    //    foreach (Ejecta e in es)
    //    {
    //        e.v.Set(v_0.x, v_0.y, v_0.z);  // initial velocity at time = 0
    //        e.pos.Set(0f, 0f, 0f);  // rocket starts at the origin (0, 0, 0)
    //    }
    //    int num_steps_ascent = Mathf.CeilToInt(t_e / dt); 
    //    float[] discrete_time_ascent = new float[num_steps_ascent];
    //    for (int i = 0; i < num_steps_ascent; i++)
    //    {
    //        discrete_time_ascent[i] = dt * (float) (i-1);
    //    }
    //    Vector3 F_di = new Vector3(0f, 0f, 0f); 
    //    Vector3 psi_tot = new Vector3(0f, 0f, 0f);
    //    // time stepping loop during ascent
    //    foreach (float dt_i in discrete_time_ascent) 
    //    {
    //        //foreach (Ejecta e in es)
    //        for (int i = 0; i < es.Length; i++)
    //        {
    //            Ejecta e = es[i]; 
    //            F_di = 0.5f * Mathf.PI * Mathf.Pow(R_r, 2) * coeff_drag(R_r, rho_a, e.v, v_a, mu_a) * 
    //                (v_a - e.v).magnitude * (v_a - e.v);
    //            psi_tot = F_di + F_gr;
    //
    //            // perform explicit Euler integration
    //            e.v.Set((e.v + dt / m_r * psi_tot).x, (e.v + dt / m_r * psi_tot).y, (e.v + dt / m_r * psi_tot).z);
    //            e.pos.Set((e.pos + dt * e.v).x, (e.pos + dt * e.v).y, (e.pos + dt * e.v).z);
    //
    //            // TODO: use fixedUpdate method and display objects in scene
    //            // example: if (timeNow == dt_i + timeStart) => show object in scene
    //
    //            // TODO: Insert rocket's smoke trail?
    //        }
    //
    //    }
    //
    //    /* EXPLOSION!!! */
    //    Vector3 blast_origin = es[0].pos;   // place puff of smoke here
    //    float deltav = Mathf.Sqrt((float)(2 * eta * H_e * m_e / m_p));   // change in speed after detonation
    //    float eps_1;  
    //    float eps_2;
    //    float theta_s;  // spherical polar angle RNV
    //    float phi_s;    // spherical azimuthal angle RNV
    //    Vector3 n_i = new Vector3(0f, 0f, 0f);  // velocity trajectory
    //    for (int i = 0; i < es.Length; i++)
    //    {
    //        Ejecta e = es[i];
    //        eps_1 = 0.5f; // TODO: Change to random variable [0,1]
    //        eps_2 = 0.5f; // TODO: Change to random variable [0,1]
    //        theta_s = 2f * Mathf.PI * eps_1;
    //        phi_s = Mathf.Acos(1f - 2f * eps_2);
    //        
    //        // set new position from fragmenting blast
    //        e.pos.Set(e.pos.x + R_r * Mathf.Cos(theta_s) * Mathf.Sin(phi_s),
    //            e.pos.y + R_r * Mathf.Sin(theta_s) * Mathf.Sin(phi_s),
    //            e.pos.z + R_r * Mathf.Cos(phi_s));
    //
    //        // trajectory calculation
    //        n_i.Set((e.pos.x - blast_origin.x) / (e.pos - blast_origin).magnitude,
    //            (e.pos.y - blast_origin.y) / (e.pos - blast_origin).magnitude,
    //            (e.pos.z - blast_origin.z) / (e.pos - blast_origin).magnitude);
    //        // set velocity
    //        e.v.Set(e.v.x + deltav * n_i.x, e.v.y + deltav * n_i.y, e.v.z + deltav * n_i.z);
    //    }
    //    
    //
    //    /* Particle Descent */
    //    int num_landed = 0;
    //    while (num_landed < N_p)
    //    {
    //
    //        //foreach (Ejecta e in es)
    //        for (int i = 0; i < es.Length; i++)
    //        {
    //            Ejecta e = es[i];
    //
    //            if (e.pos.z <= 0f)  // TODO: landing criteria at or below the z=0 plane? Can change
    //            {
    //                e.landed = 1;
    //                e.pos.Set(e.pos.x, e.pos.y, 0f);
    //                num_landed++;
    //            }     
    //
    //            // perform explicit Euler integration on particles in flight
    //            if (e.landed != 1) 
    //            {
    //                F_di = 0.5f * Mathf.PI * Mathf.Pow(R_p, 2) * coeff_drag(R_p, rho_a, e.v, v_a, mu_a) *
    //                (v_a - e.v).magnitude * (v_a - e.v);
    //                psi_tot = F_di + F_gi;
    //                e.v.Set((e.v + dt / m_r * psi_tot).x, (e.v + dt / m_r * psi_tot).y, (e.v + dt / m_r * psi_tot).z);
    //                e.pos.Set((e.pos + dt * e.v).x, (e.pos + dt * e.v).y, (e.pos + dt * e.v).z);
    //
    //                // TODO: use fixedUpdate method and display objects in scene as time elapses
    //            }
    //
    //        }
    //    }
    //
    //    // TODO: erase ejecta from scene after set amount of time?
    //}
    //
    //private float coeff_drag(float radius, float rho_a, Vector3 v, Vector3 v_a, float mu_a)
    //{
    //    float cd = 1000;
    //    float Re = (2f * radius * rho_a * (v - v_a).magnitude) / mu_a;
    //    if (Re > 2e6f)
    //    {
    //        cd = 0.18f;
    //    } else if (Re <= 2e6f && Re > 3e5f)
    //    {
    //        cd = 3.66e-4f * Mathf.Pow(Re, 0.4275f);
    //    } else if (Re <= 3e5f & Re > 400f)
    //    {
    //        cd = 0.5f;
    //    } else if (Re <= 400f && Re > 1f)
    //    {
    //        cd = 24f * Mathf.Pow(Re, -.646f);
    //    } else if (Re <= 1f)
    //    {
    //        cd = 24f / Re;
    //    }
    //    return cd;
    //}
}

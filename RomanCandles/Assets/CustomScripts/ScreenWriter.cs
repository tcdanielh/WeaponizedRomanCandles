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
        double g = 9.81;    // gravitational acceleration [m/s]
        double eta = 0.05;  // explosive efficiency
        double mu_a = 1.8e-5;   // air viscosity [kg/(m*s)]
        double N_p = es.Length;
        double H_e = 3e6;   // explosive heat of combustion [J/kg]
        double R_p = 0.005; // projectile radius
        double R_r = .2;    // firework radius
        double rho_a = 1.225;   // air density [kg/m^3]
        double rho_p = 2000;    // projectile density [kg/m^3]
        Vector3 v_a = new Vector3(0,0,0);  // air velocity [m/s]
        double m_s = 2; // inert structural mass [kg]
        

        // options to change burst radius, flight time, accuracy
        double m_e = 5; // explosive charge mass [kg]
        double m_f = 10;    // launch charge mass [kg]
        double t_e = 5; // time of explsion [sec]
        double dt = 0.001;  // time step [sec]

        double m_i = 4 / 3 * Mathf.PI * Mathf.Pow((float)R_p, 3) * rho_p;  // mass of ejecta 
        double m_p = N_p * m_i; // total mass of ejecta
        double A_px = Mathf.PI * Mathf.pow((float)R_p, 2);  // cross-sectional area of ejecta
        double deltav = Mathf.Sqrt((float)(2 * eta * H_e * m_e / m_p));   // change in speed after detonation
        
        double m_r = m_p + m_e + m_s;   // total rocket mass [kg]
        Vector3 v_0 = new Vector3(0, 0, Mathf.Sqrt((float)(2 * eta * H_e * m_f / m_r)));    // initial velocity [m/s]

        /* Rocket Ascent */
        // TODO: mark time for when rocket is launched

        Vector3 F_gr = new Vector3(0., 0., -g * m_r);   // force of gravity on rocket [N]
        
        int num_steps_ascent = t_e / dt + 1; 
        double[] discrete_time_ascent = new double[num_steps_ascent];
        for (int i = 0; i < num_steps_ascent; i++)
        {
            discrete_time_ascent[i] = dt * (double) i;
        }
        Vector3 F_drag = new Vector3((v_a - v_0).x, (v_a - v_0).y, (v_a - v_0).z); 
        F_drag = F_drag * 0.5 * coeff_drag(R_r, rho_a, v_0, v_a, mu_a) * (v_a - v_0).magnitude * Mathf.PI * Mathf.pow(R_r, 2);
        Vector3d psi_tot = new Vector3d(0.);
        // time stepping loop during ascent
        foreach (double time in discrete_time_ascent)
        {
            foreach (Ejecta e in es)
            {
                psi_tot = F_drag + F_gr;
                e.v = e.v + dt / m_r * psi_tot;
                e.pos = e.pos + dt * e.v;

                // TODO: use fixedUpdate method
            }
        }

        /* EXPLOSION!!! */
        Vector3 blast_origin = es[0].pos;

        

    }
    
    private double coeff_drag(double radius, double rho_a, Vector3 v, Vector3 v_a, double mu_a)
    {
        double cd;
        double Re = (2 * radius * rho_a * (v - v_a).magnitude) / mu_a;
        if (Re > 2e6)
        {
            cd = 0.18;
        } else if (Re <= 2e6 && Re > 3e5)
        {
            cd = 3.66e-4 * Mathf.pow(Re, 0.4275);
        } else if (Re <= 3e5 & Re > 400.)
        {
            cd = 0.5;
        } else if (Re <= 400 && Re > 1)
        {
            cd = 24. * Mathf.pow(Re, -.646);
        } else if (Re < 1)
        {
            cd = 24. / Re;
        }
        return cd;
    }
}

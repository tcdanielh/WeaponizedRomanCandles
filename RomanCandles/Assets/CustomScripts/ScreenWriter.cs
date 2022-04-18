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
    public ComputeBuffer hashBuffer

    public struct Ejecta{
        public Vector3 pos;
        public Vector3 v;
        public Vector4 color;
    }


    public int EjectaSize = sizeof(float) * 7;

    private void Start()
    {
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
        material.SetInt("numSteps", 20);
        material.SetInt("numStepsLight", 20);
        material.SetVector("lPos", lightPoint.position);
        material.SetVector("lColor", lightPoint.GetComponent<Light>().color);
        material.SetFloat("lIntensity", lightPoint.GetComponent<Light>().intensity);
        material.SetFloat("smokeLightAbsorb", smokeLightAbsorb);;

        Graphics.Blit(source, destination, material);

    }
    
    private void firework(Ejecta[] es)
    {   /* Physical Parameters */
        double g = 9.81;    // gravitational acceleration [m/s]
        double eta = 0.05;  // explosive efficiency
        double mu_a = 1.8e-5;   // air viscosity [kg/(m*s)]
        double N_p = es.length;
        double H_e = 3e6;   // explosive heat of combustion [J/kg]
        double R_p = 0.005; // projectile radius
        double R_r = .2;    // firework radius
        double rho_a = 1.225;   // air density [kg/m^3]
        double rho_p = 2000;    // projectile density [kg/m^3]
        Vector3 v_a = new Vector3(0.);  // air velocity [m/s]
        double m_s = 2; // inert structural mass [kg]
        double m_i = 4. / 3. * MATH.PI * Math.pow(R_p, 3) * rho_p;  // mass of ejecta
        double m_p = N_p * m_i; // total mass of ejecta

        double m_e = 5; // explosive charge mass [kg]
        double deltav = Math.sqrt(2 * eta * H_e * m_e / m_p);   // change in speed after detonation
        double m_f = 10;    // launch charge mass [kg]
        Vector3 vt0 = new Vector3(0., 0., (double)Math.sqrt(2 * eta * H_e * m_f / m_r));    // initial velocity [m/s]

        double m_r = m_p + m_e + m_s;   // total rocket mass [kg]

    }
}

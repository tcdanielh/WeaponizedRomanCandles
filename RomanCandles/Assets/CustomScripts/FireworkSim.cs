using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireworkSim : MonoBehaviour
{
    [SerializeField] EjectaHandler ejectaHandler;

    /* Physical Parameters */
    float g = 9.81f;    // gravitational acceleration [m/s]
    float eta = 0.05f;  // explosive efficiency
    float mu_a = 1.8e-5f;   // air viscosity [kg/(m*s)]
    public int N_p = 10;    // number of particles
    float H_e = 3e6f;   // explosive heat of combustion [J/kg]
    float R_p = 0.005f; // particle radius
    float R_r = .2f;    // firework radius
    float rho_a = 1.225f;   // air density [kg/m^3]
    float rho_p = 2000f;    // projectile density [kg/m^3]
    Vector3 v_a = new Vector3(0f, 0f, 0f);  // air velocity [m/s]
    float m_s = 2f; // inert structural mass [kg]


    // options to change burst radius, flight time, accuracy
    [SerializeField] float m_e = 1e-30f; // explosive charge mass [kg]
    [SerializeField] float m_f = 1e-30f;    // launch charge mass [kg]
    [SerializeField] float t_e = 0f; // time of explosion [sec]
    float dt;  // time step [sec]

    float m_i;  // mass of ejecta [kg]
    Vector3 F_gi;   // weight of ejecta [N]
    float m_p; // total mass of ejecta
    float A_px;  // cross-sectional area of ejecta

    float m_r;   // total rocket mass [kg]
    Vector3 v_0;// initial velocity [m/s]
    Vector3 F_gr;

    public ScreenWriter.Ejecta[] es;

    float rocketLauchTime;
    bool exploded;


    //Debug position spheres
    GameObject[] spheres;


    //Destroy after time
    [SerializeField] float lifeTime;
    float birthTime;

    public Color color;

    [SerializeField] bool debug = false;

    // Start is called before the first frame update
    void Start()
    {
        birthTime = Time.time;
        ejectaHandler.addFirework(this);
        exploded = false;
        dt = Time.fixedDeltaTime;
        rocketLauchTime = Time.time;
        m_i = 4f / 3f * Mathf.PI * Mathf.Pow(R_p, 3) * rho_p;  // mass of ejecta [kg]
        
        m_p = N_p * m_i; // total mass of ejecta
        A_px = Mathf.PI * Mathf.Pow(R_p, 2);  // cross-sectional area of ejecta

        m_r = m_p + m_e + m_s;   // total rocket mass [kg]
        
        v_0 = new Vector3(0, Mathf.Sqrt((float)(2f * eta * H_e * m_f / m_r)), 0);    // initial velocity [m/s]

        F_gr = new Vector3(0f, m_r * -g, 0f);

        F_gi = new Vector3(0f, m_i * -g, 0f);   // weight of ejecta [N]

        //initialize es
        es = new ScreenWriter.Ejecta[N_p];
        for (int i = 0; i < es.Length; i++)
        {
            ScreenWriter.Ejecta e = new ScreenWriter.Ejecta();
            e.color = color;
            e.color.w = 0f;
            e.v.Set(v_0.x, v_0.y, v_0.z);  // initial velocity at time = 0
            e.pos.Set(0f, 0.1f, 0f);  // rocket starts these coordinates
            es[i] = e;
        }

        if (debug)
        {
            spheres = new GameObject[N_p];
            for (int i = 0; i < spheres.Length; i++)
            {
                spheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                float radius = 1f;
                spheres[i].transform.localScale = radius * Vector3.one;
            }
        }
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Debug.Log("mr = " + m_r);
        if (Time.time < rocketLauchTime + t_e)
        {
            Debug.Log("A");
            Ascent();
        }
        else if (!exploded)
        {
            Debug.Log("E");
            exploded = true;
            Explosion();
        }
        else
        {
            Debug.Log("D");
            Descent();
        }
        //Debug.Log("pos = " + es[0].pos);
        //Debug.Log("v = " + es[0].v);
        if (debug)
        {
            for (int i = 0; i < spheres.Length; i++)
            {
                spheres[i].transform.position = es[i].pos;
            }
        }
        
    }

    

    void Ascent()
    {
        //foreach (Ejecta e in es)
        for (int i = 0; i < es.Length; i++)
        {
            Vector3 F_di = 0.5f * Mathf.PI * Mathf.Pow(R_r, 2) * coeff_drag(R_r, rho_a, es[i].v, v_a, mu_a) *
                (v_a - es[i].v).magnitude * (v_a - es[i].v);
            Vector3 psi_tot = F_di + F_gr;

            // perform explicit Euler integration

            es[i].v.Set((es[i].v + dt / m_r * psi_tot).x, (es[i].v + dt / m_r * psi_tot).y, (es[i].v + dt / m_r * psi_tot).z);
            es[i].pos.Set((es[i].pos + dt * es[i].v).x, (es[i].pos + dt * es[i].v).y, (es[i].pos + dt * es[i].v).z);

            // TODO: Insert rocket's smoke trail?
        }
    }

    void Explosion()
    {
        /* EXPLOSION!!! */
        Vector3 blast_origin = es[0].pos;   // place puff of smoke here
        float deltav = Mathf.Sqrt((float)(2 * eta * H_e * m_e / m_p));   // change in speed after detonation
        float eps_1;
        float eps_2;
        float theta_s;  // spherical polar angle RNV
        float phi_s;    // spherical azimuthal angle RNV
        Vector3 n_i = new Vector3(0f, 0f, 0f);  // velocity trajectory

        Debug.Log("pre exp pos = " + es[1].pos);
        Debug.Log("pre exp v = " + es[1].v);
        for (int i = 0; i < es.Length; i++)
        {
            eps_1 = Random.value;
            eps_2 = Random.value;
            theta_s = 2f * Mathf.PI * eps_1;
            phi_s = Mathf.Acos(1f - 2f * eps_2);

            // set new position from fragmenting blast
            es[i].pos.Set(es[i].pos.x + R_r * Mathf.Cos(theta_s) * Mathf.Sin(phi_s),
                es[i].pos.y + R_r * Mathf.Cos(phi_s),
                es[i].pos.z + R_r * Mathf.Sin(theta_s) * Mathf.Sin(phi_s));          

            // trajectory calculation
            //n_i.Set((es[i].pos.x - blast_origin.x) / (es[i].pos - blast_origin).magnitude,
            //    (es[i].pos.y - blast_origin.y) / (es[i].pos - blast_origin).magnitude,
            //    (es[i].pos.z - blast_origin.z) / (es[i].pos - blast_origin).magnitude);
            n_i = (es[i].pos - blast_origin).normalized;

            // set velocity
            es[i].v.Set(es[i].v.x + deltav * n_i.x, es[i].v.y + deltav * n_i.y, es[i].v.z + deltav * n_i.z);

            
        }
        Debug.Log("post exp pos = " + es[1].pos);
        Debug.Log("post exp v = " + es[1].v);
    }

    void Descent()
    {
        /* Particle Descent */
        //foreach (Ejecta e in es)
        Vector3 F_di = new Vector3(0f, 0f, 0f);
        Vector3 psi_tot = new Vector3(0f, 0f, 0f);
        float drag_scalar;
        for (int i = 0; i < es.Length; i++)
        {

            if (es[i].pos.y <= 0f)  // TODO: landing criteria at or below the y=0 plane? Can change
            {
                es[i].landed = 1;
                es[i].pos.Set(es[i].pos.x, 0f, es[i].pos.z);    // correction to y = 0
            }

            // perform explicit Euler integration on particles in flight
            if (es[i].landed != 1)
            {
                //Vector3 F_di = 0.5f * A_px * coeff_drag(R_p, rho_a, es[i].v, v_a, mu_a) *
                //(v_a - es[i].v).magnitude * (v_a - es[i].v);
                drag_scalar = 0.5f * A_px * coeff_drag(R_p, rho_a, es[i].v, v_a, mu_a) * (v_a - es[i].v).magnitude;
                F_di.Set(drag_scalar * (v_a - es[i].v).x,
                    drag_scalar * (v_a - es[i].v).y,
                    drag_scalar * (v_a - es[i].v).z);
                psi_tot.Set((F_di + F_gi).x, (F_di + F_gi).y, (F_di + F_gi).z);

                // perform explicit Euler integration
                es[i].v.Set((es[i].v + dt / m_i * psi_tot).x, (es[i].v + dt / m_i * psi_tot).y, (es[i].v + dt / m_i * psi_tot).z);
                es[i].pos.Set((es[i].pos + dt * es[i].v).x, (es[i].pos + dt * es[i].v).y, (es[i].pos + dt * es[i].v).z);
            }

        }
    }

    /* Returns piecewise coefficient of drag for a sphere */
    private float coeff_drag(float radius, float rho_a, Vector3 v, Vector3 v_a, float mu_a)
    {
        float cd = 1000f;
        float Re = (2f * radius * rho_a * (v - v_a).magnitude) / mu_a;  // Reynolds number for sphere
        if (Re > 2e6f)
        {
            cd = 0.18f;
        }
        else if (Re <= 2e6f && Re > 3e5f)
        {
            cd = 3.66e-4f * Mathf.Pow(Re, 0.4275f);
        }
        else if (Re <= 3e5f & Re > 400f)
        {
            cd = 0.5f;
        }
        else if (Re <= 400f && Re > 1f)
        {
            cd = 24f * Mathf.Pow(Re, -.646f);
        }
        else if (Re <= 1f)
        {
            cd = 24f / Re;
        }
        return cd;
    }

    private void Update()
    {
        if (Time.time > birthTime + lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        ejectaHandler.removeFirework(this);
    }
}

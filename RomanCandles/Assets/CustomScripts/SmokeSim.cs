using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSim : MonoBehaviour
{
    public ComputeBuffer smokeDensityBuffer;
    //other buffer variables go here
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //physics updates (i.e. calls to the compute shaders) go here
    }

}

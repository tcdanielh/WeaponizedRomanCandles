using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCMakeFirework : MonoBehaviour
{
    [SerializeField] GameObject fireworkPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Instantiate(fireworkPrefab).GetComponent<FireworkSim>().color = Random.ColorHSV();
        }
    }
}

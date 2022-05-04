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
            Vector2 angle = Random.insideUnitCircle;
            angle /= 3;
            Vector3 dir = Vector3.up + new Vector3(angle.x, 0, angle.y);
            dir.Normalize();
            Vector2 pos = Random.insideUnitCircle;
            pos *= 2;
            Vector3 p3 = new Vector3(pos.x, 0, pos.y);
            FireworkSim fsim = Instantiate(fireworkPrefab).GetComponent<FireworkSim>();
            fsim.color = Random.ColorHSV();
            fsim.playerTraj = dir;
            fsim.playerOrigin = p3;
        }
    }
}

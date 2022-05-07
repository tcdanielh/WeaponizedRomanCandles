using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
public class PCMakeFirework : MonoBehaviour
{
    [SerializeField] GameObject fireworkPrefab;


    public GameObject leftControllerObj;
    public GameObject rightControllerObj;

    private InputDevice leftController;
    private InputDevice rightController;

    private bool leftAlreadyPressed;
    private bool rightAlreadyPressed;

    // Start is called before the first frame update
    void Start()
    {
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        //Debug.Log(rightController);
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

        leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftPressed);
        rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightPressed);

        if (leftPressed && !leftAlreadyPressed)
        {
            leftAlreadyPressed = true;

            //Vector2 angle = Random.insideUnitCircle;
            //angle /= 3;
            Vector3 dir = leftControllerObj.transform.forward;
            //dir.Normalize();
            //Vector2 pos = Random.insideUnitCircle;
            //pos *= 2;
            Vector3 p3 = leftControllerObj.transform.position;
            FireworkSim fsim = Instantiate(fireworkPrefab).GetComponent<FireworkSim>();
            fsim.color = Random.ColorHSV();
            fsim.playerTraj = dir;
            fsim.playerOrigin = p3;

        }
        else if (!leftPressed && leftAlreadyPressed)
        {
            leftAlreadyPressed = false;
        }

        if (rightPressed && !rightAlreadyPressed)
        {
            rightAlreadyPressed = true;

            //Vector2 angle = Random.insideUnitCircle;
            //angle /= 3;
            Vector3 dir = rightControllerObj.transform.forward;
            //dir.Normalize();
            //Vector2 pos = Random.insideUnitCircle;
            //pos *= 2;
            Vector3 p3 = rightControllerObj.transform.position;
            FireworkSim fsim = Instantiate(fireworkPrefab).GetComponent<FireworkSim>();
            fsim.color = Random.ColorHSV();
            fsim.playerTraj = dir;
            fsim.playerOrigin = p3;

        } else if (!rightPressed && rightAlreadyPressed)
        {
            rightAlreadyPressed = false;
        }
    }
}

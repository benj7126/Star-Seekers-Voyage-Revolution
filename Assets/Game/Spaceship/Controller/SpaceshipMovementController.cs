using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipMovementController : MonoBehaviour
{
    [SerializeField] private SpaceshipControllerPart ControllerL;
    [SerializeField] private SpaceshipControllerPart ControllerR;

    [SerializeField] public SpaceshipLandingHandler Lander;

    [SerializeField] public Transform Spaceship;
    [SerializeField] public Transform Player;

    // Commented out to shut-up unity warning
    //[SerializeField] private int methode = 0;

    [NonSerialized] public Vector3 Velocity = new Vector3(0, 0, 0);
    [NonSerialized] public Vector3 RotVelocity = new Vector3(0, 0, 0);

    private bool onGround = false;

    private float speedScale = 10f;
    private float rotationScale = 10f;

    void Update()
    {
        //Debug.Log("ong: " + onGround);
        GameManager.instance.PlayerOnGround = onGround;
        if (!onGround)
        {

            //gameObject.GetComponent<Transform>().SetPositionAndRotation(Spaceship.position, Spaceship.rotation);

            Vector3 offsetL = ControllerL.GetOffsetPosition();
            Vector3 offsetR = ControllerR.GetOffsetPosition();

            bool LeftTrigger = LeftHandTriggerHeld();
            bool RightTrigger = RightHandTriggerHeld();

            // if trigger is held, dont add forwards vel
            float ForwardL = LeftTrigger ? 0f : offsetL.z;
            float ForwardR = LeftTrigger ? 0f : offsetR.z;
            Velocity -= transform.forward * (ForwardL + ForwardR);

            float LeftL = offsetL.x;
            float UpL = offsetL.y;

            if (LeftTrigger)
            {
                Velocity += transform.right * -LeftL + transform.up*UpL;
            }
            else
            {
                RotVelocity += new Vector3(UpL, -LeftL, 0f);
            }

            float LeftR = offsetR.x;
            float UpR = offsetR.y;

            if (RightTrigger)
            {
                Velocity += transform.right * -LeftR + transform.up * UpR;
            }
            else
            {
                RotVelocity += new Vector3(UpR, -LeftR, 0f);
            }

            if (!RightTrigger && !LeftTrigger)
            {
                RotVelocity += new Vector3(0f, 0f, UpL - UpR);
            }

            Velocity *= (1-Time.deltaTime);
            RotVelocity *= (1-Time.deltaTime*0.4f);

            Spaceship.Rotate(RotVelocity * rotationScale * Time.deltaTime, Space.Self);
            Spaceship.Translate(Velocity * speedScale * Time.deltaTime, Space.World);

            int landedPlanet = Lander.LandUpdate(this);

            //Debug.Log("landedplanet: " + landedPlanet);

            if (landedPlanet != -1)
            {
                onGround = true;

                Spaceship.SetParent(GameManager.instance.SS.Planets[landedPlanet].PhysicalPlanet.transform);
                Player.SetParent(GameManager.instance.SS.Planets[landedPlanet].PhysicalPlanet.transform);
                VRPlayerController.CurPlanet = landedPlanet;
                Player.GetComponent<Rigidbody>().useGravity = true;

                Player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            }
        }

    }

    public void genSyst()
    {
        GameManager.instance.GenerateRandSolarSystem();
    }

    public void TakeOff()
    {
        Debug.Log("pressed");

        if(!onGround)
            return;

        onGround = false;

        Spaceship.parent = null;
        Player.SetParent(Spaceship);
        VRPlayerController.CurPlanet = -1;
        Player.GetComponent<Rigidbody>().useGravity = false;
        Player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        Lander.TakeOff(this);
    }

    bool LeftHandTriggerHeld()
    {
        var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, leftHandDevices);

        if (leftHandDevices.Count != 1)
            return false;

        bool triggerValue = false;
        leftHandDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue);

        return triggerValue;
    }
    bool RightHandTriggerHeld()
    {
        var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);

        if (rightHandDevices.Count != 1)
            return false;

        bool triggerValue = false;
        rightHandDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue);

        return triggerValue;
    }
}




/* 'old' movement methode

// add super man like controls, ig

// speed += ForwardL + ForwardR;
// speed *= 0.6f; // maby dont do this

float YRotate = ForwardL - ForwardR;
float ZRotate = UpR - UpL;
float XRotate = 0;

if (methode == 0)
    XRotate = (GetRotationFromUpForward(UpL, ForwardL) + GetRotationFromUpForward(UpR, ForwardR)) / 40f;

if (methode == 1)
    XRotate = (GetRotationFromUpForward(UpL, ForwardL) + GetRotationFromUpForward(UpR, ForwardR)) / 20f;

RotVelocity += new Vector3(XRotate, YRotate, ZRotate);
RotVelocity *= rotVelMul;

if (methode == 0)
{
    Velocity += transform.forward * (ForwardL + ForwardR);
    Velocity += transform.right * (LeftL + LeftR);
    Velocity += transform.up * (UpL + UpR);
}


if (methode == 1)
    Velocity += transform.forward * (ForwardL + ForwardR - XRotate);

//Velocity += transform.right * (LeftL + LeftR);
//Velocity += transform.up * (UpL + UpR);


Velocity *= speedVelMul;



// float YRotate = ForwardL - ForwardR;

// float ZRotate = UpL - UpR;

// Spaceship.Rotate(new Vector3(XRotate*0.05f, YRotate, ZRotate), Space.World);
// VelocityL *= 0; // 0.99f;
// VelocityL += new Vector3(LeftL, UpL, ForwardL);

// VelocityR *= 0; // 0.99f;
// VelocityR += new Vector3(LeftR, UpR, ForwardR);

    float GetRotationFromUpForward(float Up, float Forward)
    {
        float rot = 0;
        if (Mathf.Abs(Up) > 0.1f && Mathf.Abs(Forward) > 0.1f)
        {
            float totalVal = (Mathf.Abs(Up) + Mathf.Abs(Forward)) / 2f - 0.1f;

            float lowestVal;
            if (Mathf.Abs(Forward) < Mathf.Abs(Up))
                lowestVal = Mathf.Abs(Up);
            else
                lowestVal = Mathf.Abs(Forward);

            rot = lowestVal / totalVal;

            if (Up < 0) // negative
                rot *= -1;

            if (Forward < 0) // negative
                rot *= -1;
        }

        return -rot;
    }

*/

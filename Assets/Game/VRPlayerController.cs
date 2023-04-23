using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRPlayerController : MonoBehaviour
{
    [NonSerialized] public static int CurPlanet = -1; // -1 = in spaceship...

    private static float worldSizeReduction = 0.01f;

    [SerializeField] private SkinnedMeshRenderer LeftHandCube;
    [SerializeField] private SkinnedMeshRenderer RightHandCube;

    void Update()
    {
        DoHandStuff();
        if (CurPlanet == -1)
        {
            Physics.gravity = (transform.up).normalized * -9.81f * worldSizeReduction;

        }
        else if (GameManager.instance)
        {
            Transform targetPlanetTransform = GameManager.instance.SS.Planets[CurPlanet].PhysicalPlanet.transform;
            Physics.gravity = (targetPlanetTransform.position - transform.position).normalized * 9.81f * worldSizeReduction;

            transform.LookAt(targetPlanetTransform.position);
            transform.Rotate(new Vector3(-90f, 0f, 0f), Space.Self);
        }
    }

    void DoHandStuff()
    {
        float LeftHandBlendShapeCoefficient = LeftHandGripHeld();
        float RightHandBlendShapeCoefficient = RightHandGripHeld();

        LeftHandCube.SetBlendShapeWeight(0, LeftHandBlendShapeCoefficient*100);
        RightHandCube.SetBlendShapeWeight(0, RightHandBlendShapeCoefficient*100);
    }

    float LeftHandGripHeld()
    {
        var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, leftHandDevices);

        if (leftHandDevices.Count != 1)
            return 0;

        float GripValue = 0;
        leftHandDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out GripValue);

        return GripValue;
    }
    float RightHandGripHeld()
    {
        var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);

        if (rightHandDevices.Count != 1)
            return 0;

        float GripValue = 0;
        rightHandDevices[0].TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out GripValue);

        return GripValue;
    }
}

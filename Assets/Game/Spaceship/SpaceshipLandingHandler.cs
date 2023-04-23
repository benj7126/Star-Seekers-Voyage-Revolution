using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class SpaceshipLandingHandler : MonoBehaviour
{
    [SerializeField] private Transform FrontLeg;
    [SerializeField] private Transform LeftLeg;
    [SerializeField] private Transform RightLeg;

    public float dot = 0;

    [NonSerialized] public int TargetPlanet = -1;

    private float checkDist = 4f;

    public Transform XRRoot;

    public SkinnedMeshRenderer SpaceshipMeshRendere;

    public GameObject particle1;
    public GameObject particle2;
    //int TargetPlanet = -1;
    public int LandUpdate(SpaceshipMovementController controller)
    {
        int closest = -1;
        float dist = 9999999f;

        for (int i = 0; i < GameManager.instance.SS.Planets.Length; i++)
        {
            Planet p = GameManager.instance.SS.Planets[i];

            float nDist = (p.PhysicalPlanet.transform.position - transform.position).magnitude;
            if (nDist < dist)
            {
                dist = nDist;
                closest = i;
            }
        }

        TargetPlanet = closest;

        int layerMask = 1 << 3;

        Transform[] transforms = new Transform[] { FrontLeg, LeftLeg, RightLeg };
        float[] distances = new float[] { -1f, -1f, -1f };

        dot = 0;
        if (TargetPlanet != -1)
        {
            Vector3 posDiff = GameManager.instance.SS.Planets[TargetPlanet].PhysicalPlanet.transform.position - controller.Spaceship.transform.position;

            dot = Vector3.Dot(-controller.Spaceship.transform.up, posDiff.normalized);
        }

        for (int i = 0; i < 3; i++)
        {
            RaycastHit hit;

            Transform LegPart = transforms[i];

            Vector3 direction = (controller.Spaceship.transform.up*-1);

            if (Physics.Raycast(LegPart.position, direction, out hit, checkDist, layerMask))
            {
                distances[i] = hit.distance;
            }
        }

        //Debug.Log(distances[0] + " | " + distances[1] + " | " + distances[2]);

        if (distances[0] != -1 && distances[1] != -1f && distances[2] != -1f && dot > 0.8)
        {
            //Debug.Log("Landed at "+ TargetPlanet);
            //float average = (distances[0] + distances[1] + distances[2]) / 3f;

            //controller.Spaceship.transform.position += controller.Spaceship.transform.up * -average;

            Land(controller, distances);

            dot = 0;
            return TargetPlanet;
        }

        return -1; // if it landed
    }

    public void Land(SpaceshipMovementController controller, float[] distances)
    {
        float smallestDist = Mathf.Min(distances);

        controller.Spaceship.transform.position += controller.Spaceship.transform.up * -smallestDist;
        controller.Velocity = Vector3.zero;
        controller.RotVelocity = Vector3.zero;
        particle1.SetActive(false);
        particle2.SetActive(false);

        controller.Player.position = XRRoot.position;
        controller.Player.GetComponent<DynamicMoveProvider>().enabled = true;

        SpaceshipMeshRendere.SetBlendShapeWeight(0, 0);
        SpaceshipMeshRendere.SetBlendShapeWeight(1, 100);
    }

    public void TakeOff(SpaceshipMovementController controller)
    {
        controller.Spaceship.transform.position += controller.Spaceship.transform.up * checkDist*3f;
        particle1.SetActive(true);
        particle2.SetActive(true);

        controller.Player.transform.position = XRRoot.position;
        controller.Player.GetComponent<DynamicMoveProvider>().enabled = false;

        SpaceshipMeshRendere.SetBlendShapeWeight(0, 100);
        SpaceshipMeshRendere.SetBlendShapeWeight(1, 0);
    }

    // Start is called before the first frame update
    /*
    public void LandUpdate(SpaceshipMovementController controller)
    {
        if (TargetPlanet == -1)
        {
            int closest = -1;
            float dist = 9999999f;

            for (int i = 0; i < GameManager.instance.SS.Planets.Length; i++)
            {
                Planet p = GameManager.instance.SS.Planets[i];

                float nDist = (p.PhysicalPlanet.transform.position - transform.position).magnitude;
                if (nDist < dist)
                {
                    dist = nDist;
                    closest = i;
                }
            }

            TargetPlanet = closest;
        }

        if (TargetPlanet != -1)
        {
            int layerMask = 1 << 3;

            Planet p = GameManager.instance.SS.Planets[TargetPlanet];
            Vector3 TargetPosition = p.PhysicalPlanet.transform.position;

            float dist = (controller.Spaceship.position - TargetPosition).magnitude;

            controller.Spaceship.up = Vector3.RotateTowards(controller.Spaceship.up, (controller.Spaceship.position - TargetPosition).normalized, 0.01f, 1f);
            controller.RotVelocity = Vector3.zero;
            controller.Velocity = controller.Spaceship.localToWorldMatrix * new Vector3(0, dist > p.radius*2f ? 0 : -1f, 1f);
            
            Transform[] transforms = new Transform[] { FrontLeg, LeftLeg, RightLeg };

            for (int i = 0; i < 3; i++)
            {
                RaycastHit hit;

                Transform LegPart = transforms[i];

                Vector3 direction = (TargetPosition - LegPart.position).normalized; // might need to change to just look down... change to parts to look down from as well... sorta...

                if (Physics.Raycast(LegPart.position, direction, out hit, Mathf.Infinity, layerMask))
                {
                    Debug.DrawRay(LegPart.position, direction * hit.distance, Color.yellow);
                    Debug.Log("Did Hit with a dist of: " + hit.distance);
                }
                else
                {
                    Debug.DrawRay(LegPart.position, direction * 10, Color.white);
                    //Debug.Log("Did not Hit");
                }
            }
        }
    }
    */
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SpaceshipControllerPart : MonoBehaviour
{
    [SerializeField] private Material targetMat;
    [SerializeField] private Transform targetTransform;

    [SerializeField] private Transform Spaceship;

    private bool held = false;

    private float vel = 0;

    private void Start()
    {
        targetTransform.gameObject.GetComponent<MeshFilter>().mesh.bounds = new Bounds(transform.position, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
    }

    void Update()
    {
        if (held)
        {
            float Distance = (targetTransform.position - transform.position).magnitude;
            targetMat.SetFloat("_TargetDistance", Distance);

            targetTransform.LookAt(transform);
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            vel += Time.deltaTime;

            float D1 = (targetTransform.position - transform.position).magnitude;
            Vector3 dir = (targetTransform.position - transform.position).normalized;

            transform.position += dir * vel;

            float D2 = (targetTransform.position - transform.position).magnitude;

            if (D1 < D2) {
                targetTransform.localRotation = Quaternion.identity;
                transform.position = targetTransform.position;
            }

            targetMat.SetFloat("_TargetDistance", (targetTransform.position - transform.position).magnitude);
        }
    }

    public Vector3 GetOffsetPosition()
    {
        return held ? Quaternion.Inverse(Spaceship.rotation) * (transform.position - targetTransform.position) : Vector3.zero;
    }

    public void ToggleHeld(SelectExitEventArgs args) {
        held = false;
        vel = (targetTransform.position - transform.position).magnitude;
    }
    public void ToggleHeld(SelectEnterEventArgs args) { held = true; }
}

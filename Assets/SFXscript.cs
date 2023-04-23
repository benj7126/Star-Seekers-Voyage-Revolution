using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXscript : MonoBehaviour
{
    public SpaceshipMovementController SpaceshipMovementController;
    public AudioSource audio;
    float pitch = 0;
    public float pitchmax = 3;
    public float movespeedmax = 3;
    void Start()
    {
        audio = GetComponent<AudioSource>();
    }
    void Update()
    {
        float movespeed = SpaceshipMovementController.Velocity.magnitude;
        pitch = movespeed / movespeedmax * pitchmax;
        pitch = Mathf.Min(pitch, pitchmax);
        audio.pitch = pitch;
        //Debug.Log(movespeed);
    }
}

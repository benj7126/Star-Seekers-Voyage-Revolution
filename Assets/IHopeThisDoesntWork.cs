using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IHopeThisDoesntWork : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<SpaceshipMovementController>().enabled = false;
        GetComponent<SpaceshipMovementController>().enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

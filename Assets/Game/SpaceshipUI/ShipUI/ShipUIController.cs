using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShipUIController : MonoBehaviour
{
    public SpaceshipMovementController SpaceshipMovementController;
    public TMP_Text MovementSpeedText;
    public TMP_Text LanndingAccuarcyText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MovementSpeedText.text = $"{(int)(SpaceshipMovementController.Velocity.magnitude * 2.5)} km/h";
        float dot = SpaceshipMovementController.Lander.dot;
        string canLand = dot > 0.8 ? "Can land" : "Can't land";
        LanndingAccuarcyText.text = $"{canLand} | Precision: {(int)(-(1 - dot) * 180f)}°";
    }
}

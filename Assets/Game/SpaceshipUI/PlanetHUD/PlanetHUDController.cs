using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlanetHUDController : MonoBehaviour
{
    private GameObject MainCamGO;
    public TMP_Text DistanceText;
    public TMP_Text PlanetNameText;

    public UnityEngine.UI.Image OutlinePanel;
    public UnityEngine.UI.Image SeperatorPanel;
    
    // Start is called before the first frame update
    void Start()
    {
        MainCamGO = Camera.main.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(MainCamGO.transform);
        transform.localRotation *= Quaternion.Euler(0, 0, GameManager.instance.Spaceship.transform.rotation.eulerAngles.z);
        
        var Distance = (MainCamGO.transform.position - transform.position).magnitude / 4;
        DistanceText.text = $"{(int)(Distance * 10)} km";
        //transform.rotation = transform.rotation * Quaternion.Euler(90, 0, 0);

        float TransparencyLerp = (Distance - 75f) / (150f-75f);
        if (GameManager.instance.PlayerOnGround) TransparencyLerp = 0;
        
        DistanceText.color = new Color(DistanceText.color.r, DistanceText.color.g, DistanceText.color.b, TransparencyLerp);
        PlanetNameText.color = new Color(PlanetNameText.color.r, PlanetNameText.color.g, PlanetNameText.color.b, TransparencyLerp);
        OutlinePanel.color = new Color(OutlinePanel.color.r, OutlinePanel.color.g, OutlinePanel.color.b, TransparencyLerp);
        SeperatorPanel.color = new Color(SeperatorPanel.color.r, SeperatorPanel.color.g, SeperatorPanel.color.b, TransparencyLerp);
    }

    public void SetName(string Name)
    {
        PlanetNameText.text = Name;
    }
}

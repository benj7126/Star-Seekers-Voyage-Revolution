using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using Unity.Mathematics;
using UnityEngine.Serialization;

[Serializable]
public class PlanetScreenSpaceInfo
{
    public Vector3 PlanetCenter;
    public float PlanetRadius = 1000;
    
    public Color WaterTopColor = new Color(0, 0.3f, 1, 1);
    public Color WaterBottomColor = new Color(0, 0, 1, 1);

    public Color AtmosphereColor = new Color(1, 0.4f, 1, 1);

    public PlanetScreenSpaceInfo(Vector3 PlanetCenter, float PlanetRadius, Color WaterTopColor, Color WaterBottomColor, Color AtmosphereColor)
    {
        this.PlanetCenter = PlanetCenter;
        this.PlanetRadius = PlanetRadius;
        this.WaterTopColor = WaterTopColor;
        this.WaterBottomColor = WaterBottomColor;
        this.AtmosphereColor = AtmosphereColor;
    }
}

public class WaterShaderScript : MonoBehaviour
{
    public static WaterShaderScript instance = null;

    private Material WaterMaterial;
    private Material AtmosphereMaterial;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Debug.LogError("Singleton was instantiated multiple times!");
#if false
        WaterMaterial = new Material(Shader.Find("Hidden/WaterShader"));
        AtmosphereMaterial = new Material(Shader.Find("Hidden/AtmosphereShader"));
#endif
        SetupMaterialProperties();
    }

    public Vector3 SunPos = new Vector3(0, 0, 0);

    [SerializeField] private SpaceshipLandingHandler Lander;

    [FormerlySerializedAs("PlanetWater")] [SerializeField] public PlanetScreenSpaceInfo[] PlanetScreenSpace;

    public float Time = 0;

    public void SetupMaterialProperties()
    {
#if false
        WaterMaterial.SetVector("_SunPos", SunPos);
        WaterMaterial.SetInt("_PlanetAmount", PlanetScreenSpace.Length);
        
        AtmosphereMaterial.SetVector("_SunPos", SunPos);
        AtmosphereMaterial.SetInt("_PlanetAmount", PlanetScreenSpace.Length);
        
        for (int i = 0; i < PlanetScreenSpace.Length; i++)
        {
            WaterMaterial.SetVectorArray("_PlanetCenter", PlanetScreenSpace.Select(x => (Vector4)x.PlanetCenter).ToArray());
            WaterMaterial.SetFloatArray("_PlanetRadius", PlanetScreenSpace.Select(x => x.PlanetRadius).ToArray());
            
            WaterMaterial.SetVectorArray("_TopColor",    PlanetScreenSpace.Select(x => (Vector4)x.WaterTopColor).ToArray());
            WaterMaterial.SetVectorArray("_BottomColor", PlanetScreenSpace.Select(x => (Vector4)x.WaterBottomColor).ToArray());
            
            
            AtmosphereMaterial.SetVectorArray("_PlanetCenter", PlanetScreenSpace.Select(x => (Vector4)x.PlanetCenter).ToArray());
            AtmosphereMaterial.SetFloatArray("_PlanetRadius", PlanetScreenSpace.Select(x => x.PlanetRadius).ToArray());
            
            AtmosphereMaterial.SetVectorArray("_AtmosphereColor", PlanetScreenSpace.Select(x => (Vector4)x.AtmosphereColor).ToArray());
        }
#endif
    }

    private void Update()
    {
        Time += UnityEngine.Time.deltaTime;

        var Distance =
            (transform.position -
             GameManager.instance.SS.Planets[Lander.TargetPlanet].PhysicalPlanet.transform.position).magnitude;
        
        var AtmosphereColor = PlanetScreenSpace[Lander.TargetPlanet].AtmosphereColor;

        Distance /= 125;
        Distance = Mathf.Min(Distance, 1);
        GetComponent<Camera>().backgroundColor = Color.Lerp(AtmosphereColor, Color.black, Distance);

        //WaterMaterial.SetFloat("_NoiseTime", Time);
    }

    // Postprocess the image
    //void OnRenderImage (RenderTexture source, RenderTexture destination)
    //{
    //    RenderTexture WaterTex = new RenderTexture(source);
    //    
    //    Graphics.Blit (source, WaterTex, WaterMaterial);
    //    Graphics.Blit (WaterTex, destination, AtmosphereMaterial);

    //    WaterTex.Release();
    //}

    private void OnValidate()
    {
        //SetupMaterialProperties(ref material);
    }
}
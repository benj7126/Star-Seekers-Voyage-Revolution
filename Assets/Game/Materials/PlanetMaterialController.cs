using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PlanetMaterialController : MonoBehaviour
{
    [SerializeField] private Material BaseMat;
    [NonSerialized] public Material Mat;

    private static readonly int Radius = Shader.PropertyToID("_MixRadius");
    private static readonly int NoiseAmount = Shader.PropertyToID("_MixNoiseAmount");
    [SerializeField] public float MixRadius = 2;
    [SerializeField] public float MixNoiseAmount = 0.15f;
    
    private static readonly int ColorsLen = Shader.PropertyToID("_ColorsLen");
    private static readonly int SteepnessThreshold = Shader.PropertyToID("_SteepnessThreshold");
    private static readonly int Colors = Shader.PropertyToID("_Colors");
    private static readonly int LayerBleed = Shader.PropertyToID("_LayerBleed");
    [SerializeField] public ColorLayer[] Layers;
    
    private static readonly int BaseHeight = Shader.PropertyToID("_BaseHeight");
    [NonSerialized] public float PlanetBaseHeight = 1000;

    private static readonly int NormalSize = Shader.PropertyToID("_NormalSize");
    [NonSerialized] public float NormalSizeVal = 0;


    private static readonly int PlanetCenter = Shader.PropertyToID("_PlanetCenter");


    public Material GenerateMat()
    {
        Mat = new Material(BaseMat);
        
        SetMaterialProperties();
        
        return Mat;
    }
    
    private void OnValidate()
    {
        //SetMaterialProperties();
    }

    private void SetMaterialProperties()
    {
        Mat.SetFloat(BaseHeight, PlanetBaseHeight);
        Mat.SetVector(PlanetCenter, transform.position);

        Mat.SetFloat(Radius, MixRadius);
        Mat.SetFloat(NoiseAmount, MixNoiseAmount);
        Mat.SetFloat(NormalSize, NormalSizeVal);


        Mat.SetInt(ColorsLen, Layers.Length);
        for (int i = 0; i < Layers.Length; i++)
        {
            Mat.SetFloatArray(SteepnessThreshold, Layers.Select(x => x.SteepnessThreshold).ToArray());
            Mat.SetVectorArray(Colors, Layers.Select(x => (Vector4)x.LayerColor).ToArray());
            Mat.SetFloatArray(LayerBleed, Layers.Select(x => x.NoiseBleedCoefficient).ToArray());
        }
    }

    public void SetPlanetCenter()
    {
        Mat.SetVector(PlanetCenter, transform.position);
    }
}

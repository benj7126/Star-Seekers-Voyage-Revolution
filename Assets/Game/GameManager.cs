using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplexNoise;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    [NonSerialized] public SolarSystem SS;

    public GameObject planetBase;
    public GameObject PlanetHudPrefab;

    [SerializeField] private Vector3 TestPosition;

    [NonSerialized] public bool PlayerOnGround = false;
    public GameObject Spaceship;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Debug.LogError("Singleton was instantiated multiple times!");
    }

    private void Start()
    {
        GenerateSolarSystem();
    }

    private void Update()
    {
        System.DateTime epochStart = new System.DateTime(2023, 3, 30, 0, 0, 0, System.DateTimeKind.Utc);
        float currentTime = (float)(System.DateTime.UtcNow - epochStart).TotalHours;

        SS.Update(currentTime);

        System.DateTime start_ms = System.DateTime.UtcNow;

        // figure out how much time this shit can get...
        while (((System.DateTime.UtcNow - start_ms).TotalMilliseconds < 6) && IcosphereSegment.queuedSegments.Count != 0)
        {
            IcosphereSegment.SegmentNLOD segNLOD = IcosphereSegment.queuedSegments[IcosphereSegment.queuedSegments.Count-1];
            IcosphereSegment.queuedSegments.RemoveAt(IcosphereSegment.queuedSegments.Count-1);
            segNLOD.segment.GenerateMissingLod(segNLOD.LOD);
        }
    }

    public void GenerateSolarSystem()
    {
        if (SS != null)
            SS.Destroy();

        IcosphereSegment.queuedSegments.Clear();
        SS = new SolarSystem(TestPosition);
    }
    public void GenerateRandSolarSystem()
    {
        float max = MathF.Pow(10, 4);
        float min = -max;

        TestPosition = new Vector3(UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max));;
        GenerateSolarSystem();
    }
}

public class ColorLayer
{
    public float SteepnessThreshold = 0;
    public float NoiseBleedCoefficient = 0f;
    public Color LayerColor;

    public ColorLayer(float SteepnessThreshold)
    {
        this.SteepnessThreshold = SteepnessThreshold;

        LayerColor = Color.HSVToRGB(ValueGenerator.Gen(), Mathf.Sqrt(ValueGenerator.Gen()), Mathf.Sqrt(ValueGenerator.Gen()));

        NoiseBleedCoefficient = ValueGenerator.Gen();
    }
}

public class Planet
{
    public float PlanetOffset;
    public Vector3 LocalPosition
    {
        get
        {
            return new Vector3(0, 0, PlanetOffset);
        }
    }

    public GameObject PhysicalPlanet;

    public GameObject PlanetHud;

    public string PlanetName;

    public Material PlanetMat;

    public Vector3 RotationVector;

    public float RotationMultipler = 1f;

    public float radius = 1f;

    public float MixRadius = 2f;
    public float MixNoiseAmount = 0.15f;

    public float NormalScale = 0.15f;

    public ColorLayer[] ColorLayers;

    public Color ShallowWaterColor;
    public Color DeepWaterColor;

    public Color AtmosphereColor;

    public Planet(int planetIDX, int planetMAX, Vector3 WorldPosition, ref float PosOffset)
    {
        radius = 40f * (0.4f + ValueGenerator.Gen()*1.6f);
        MixRadius = 2 * (0.1f + ValueGenerator.Gen() * 2);
        MixNoiseAmount = 0.15f * (0.1f + ValueGenerator.Gen() * 2);

        NormalScale = (1f + ValueGenerator.Gen() * 2);

        PosOffset += radius;
        PlanetOffset = PosOffset;
        PosOffset += radius;

        RotationMultipler = (1f + ValueGenerator.Gen()*2f);
        RotationVector = new Vector3(ValueGenerator.GenNeg(), ValueGenerator.GenNeg(), ValueGenerator.GenNeg()).normalized;

        PosOffset += radius * 5 * (1 + ValueGenerator.Gen());

        float colorAmount = (0.4f + ValueGenerator.Gen() / 2f);

        float steepnessValue = -1f;
        List<float> steepnessValues = new List<float>() { -1f };
        while (steepnessValue < 2)
        {
            steepnessValue += ValueGenerator.Gen() * colorAmount; // TODO: 'colorAmount' idk about it, at 1 it has few colors at 0.2 it has a lot of colors, whats best, who knows...
            steepnessValues.Add(steepnessValue);
        }

        ColorLayers = new ColorLayer[steepnessValues.Count];
        for (int i = 0; i < steepnessValues.Count; i++)
            ColorLayers[i] = new ColorLayer(steepnessValues[steepnessValues.Count - i - 1]);

        ShallowWaterColor = Color.HSVToRGB(ValueGenerator.Gen(), Mathf.Sqrt(ValueGenerator.Gen()), Mathf.Sqrt(ValueGenerator.Gen()));
        DeepWaterColor = Color.HSVToRGB(ValueGenerator.Gen(), Mathf.Sqrt(ValueGenerator.Gen()), Mathf.Sqrt(ValueGenerator.Gen()));
        AtmosphereColor = Color.HSVToRGB(ValueGenerator.Gen(), Mathf.Sqrt(ValueGenerator.Gen()), Mathf.Sqrt(ValueGenerator.Gen()));

        //WaterShaderScript.instance.PlanetWater[planetIDX] = new PlanetWaterInfo(WorldPosition + LocalPosition, radius, ShallowWaterColor, DeepWaterColor);
        WaterShaderScript.instance.PlanetScreenSpace[planetIDX] = new PlanetScreenSpaceInfo(WorldPosition + LocalPosition, radius, ShallowWaterColor, DeepWaterColor, AtmosphereColor);


        int NameID = (int)(((WorldPosition.x * 28867 + WorldPosition.y) * 39227 + (WorldPosition.z + PlanetOffset)));
        PlanetName = PlanetNameGenerator.GenerateName(ValueGenerator.rand, 5);
    }

    public void GeneratePlanet(Vector3 WorldPosition)
    {
        PhysicalPlanet = GameObject.Instantiate(GameManager.instance.planetBase, WorldPosition + LocalPosition, Quaternion.identity) as GameObject;
        PlanetHud = GameObject.Instantiate(GameManager.instance.PlanetHudPrefab, WorldPosition + LocalPosition, Quaternion.identity) as GameObject;
        PlanetHud.transform.localScale = new Vector3(radius*2.5f, radius*2.5f, -1);
        PlanetHud.GetComponent<PlanetHUDController>().SetName(PlanetName);

        PlanetMaterialController PlanetMatCont = PhysicalPlanet.GetComponent<PlanetMaterialController>();
        PlanetMatCont.PlanetBaseHeight = radius;

        PlanetMatCont.Layers = ColorLayers;
        PlanetMatCont.MixRadius = MixRadius;
        PlanetMatCont.MixNoiseAmount = MixNoiseAmount;

        PlanetMatCont.NormalSizeVal = NormalScale;

        PlanetMat = PlanetMatCont.GenerateMat();
        var IcosphereComponent = PhysicalPlanet.GetComponent<Icosphere>();
        
        IcosphereComponent.GenerateThisPlanet(radius, WorldPosition + LocalPosition, PlanetMat);
        var WaterMat = IcosphereComponent.WaterObject.GetComponent<MeshRenderer>().material;
        WaterMat.SetVector("_PlanetCenter", IcosphereComponent.WaterObject.transform.position);
        WaterMat.SetFloat("_PlanetRadius", radius);
        WaterMat.SetVector("_TopColor", ShallowWaterColor);
        WaterMat.SetVector("_BottomColor", DeepWaterColor);


        /*
        for (int i = 0; i < Moons.Length; i++) {
            Moons[i].GeneratePlanet(WorldPosition + LocalPosition);
        }
        */
    }

    public void Update(Vector3 WorldPosition, float time)
    {
        var IcosphereComponent = PhysicalPlanet.GetComponent<Icosphere>();
        var WaterMat = IcosphereComponent.WaterObject.GetComponent<MeshRenderer>().material;
        WaterMat.SetVector("_PlanetCenter", IcosphereComponent.WaterObject.transform.position);

        PhysicalPlanet.transform.position = WorldPosition + Quaternion.AngleAxis((float)time * RotationMultipler, RotationVector) * LocalPosition;
        PlanetHud.transform.position = PhysicalPlanet.transform.position;

        PlanetMaterialController PlanetMatCont = PhysicalPlanet.GetComponent<PlanetMaterialController>();
        PlanetMatCont.SetPlanetCenter();
        // set water position
    }

    public void Destory()
    {
        Material.Destroy(PlanetMat);
        GameObject.Destroy(PlanetHud);
        GameObject.Destroy(PhysicalPlanet);
    }
}

public class SolarSystem
{
    public float SunRadius;

    public Planet[] Planets;

    public SolarSystem(Vector3 SolarSystemCenter)
    {
        ValueGenerator.SetSeed(SolarSystemCenter);

        SunRadius = 400f * (1+ ValueGenerator.Gen());

        int planets = Mathf.CeilToInt(2 + ValueGenerator.Gen() * 8f);

        WaterShaderScript.instance.PlanetScreenSpace = new PlanetScreenSpaceInfo[planets];

        float PlanetOffset = SunRadius;

        Planets = new Planet[planets];
        for (int i = 0; i < planets; i++)
        {
            Planets[i] = new Planet(i, planets, SolarSystemCenter, ref PlanetOffset);
            Planets[i].GeneratePlanet(SolarSystemCenter);
        }

        WaterShaderScript.instance.SetupMaterialProperties();
    }

    public void Update(float time)
    {
        foreach (Planet p in Planets)
        {
            p.Update(new Vector3(0, 0, 0), time);
        }
    }

    public void Destroy()
    {
        foreach (Planet p in Planets)
        {
            p.Destory();
        }
    }
}

public static class ValueGenerator
{
    public static System.Random rand;

    public static float Gen()
    {
        return (float)rand.NextDouble();
    }
    public static float GenNeg()
    {
        float v = Gen();
        return (v * 2) - 1;
    }
    public static void SetSeed(Vector3 _position)
    {
        rand = new System.Random((int)_position.x + (int)_position.y + (int)_position.z);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IcosphereSegment : MonoBehaviour
{
    [NonSerialized] public Mesh[] MeshLODs;
    [NonSerialized] public int[] LODLevels;
    [NonSerialized] public bool[] QueuedLODs;
    [NonSerialized] public bool[] GeneratedLODs;

    MeshFilter mf;
    MeshCollider mc;

    [SerializeField] private ComputeShader TriCS;
    [SerializeField] private ComputeShader CS;

    [SerializeField] private int LOD = 0;
    private int lastLOD = -1;

    [NonSerialized] public float Height;
    [NonSerialized] public Vector3 PlanetPosition;

    public struct SegmentNLOD
    {
        public IcosphereSegment segment;
        public int LOD;

        public SegmentNLOD(IcosphereSegment seg, int LOD)
        {
            segment = seg;
            this.LOD = LOD;
        }
    }

    public static List<SegmentNLOD> queuedSegments = new List<SegmentNLOD>();

    private void Awake()
    {
        mf = gameObject.GetComponent<MeshFilter>();
        mc = gameObject.GetComponent<MeshCollider>();
    }

    public void SetLOD(int TargetLOD)
    {
        if (GeneratedLODs.Length < TargetLOD)
            return;

        if (!QueuedLODs[TargetLOD])
        {
            queuedSegments.Add(new SegmentNLOD(this, TargetLOD));
            //GenerateMissingLod(TargetLOD);

            QueuedLODs[TargetLOD] = true;
        }

        if (GeneratedLODs[TargetLOD])
        {
            LOD = TargetLOD;

            if (lastLOD != LOD)
            {
                lastLOD = LOD;
                mf.mesh = MeshLODs[LOD];

                if (TargetLOD == GeneratedLODs.Length - 2)
                    mc.enabled = false;
                else
                    mc.enabled = true;
            }
        }
    }

    public void GenerateMissingLod(int LOD)
    {
        Subdivide.SubdivideTri(ref MeshLODs[LOD], TriCS, (int)Math.Pow(LODLevels[LOD], 4));

        foreach (Mesh m in MeshLODs)
        {
            ApplyCS(m);
            m.bounds = new Bounds(transform.position, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
            m.RecalculateNormals();
        }

        GeneratedLODs[LOD] = true;

        if (LOD == GeneratedLODs.Length - 1)
            mc.sharedMesh = MeshLODs[LOD];
    }

    void ApplyCS(Mesh mesh) // ApplyCS(Mesh mesh);
    {
        ComputeBuffer Verts;

        int len = mesh.vertices.Length;
        Vector3[] nVecs = new Vector3[len];

        Verts = new ComputeBuffer(len, sizeof(float) * 3);
        Verts.SetData<Vector3>(mesh.vertices.ToList());

        CS.SetBuffer(0, "Verts", Verts);
        CS.SetInt("VertsAmount", len);
        CS.SetFloat("DefaultHeight", Height);
        CS.SetVector("Center", PlanetPosition/1000f);

        CS.Dispatch(0, Mathf.CeilToInt((float)len / 512.0f), 1, 1);

        //for (int i = 0; i < len; i++)
        //{
        //    nVecs[i] = mesh.vertices[i].normalized * HeightList[i] - Center; // nVecs[i] = mesh.vertices[i].normalized * HeightList[i];
        //}

        Vector3[] OutVerts = new Vector3[len];
        Verts.GetData(OutVerts);
        mesh.vertices = OutVerts;

        Verts.Release();
    }

    Mesh[] GenMeshLOD(List<Vector3> MeshVerts)
    {
        Mesh[] Meshes = new Mesh[LODLevels.Length];

        List<int> MeshTris = new List<int>() { 0, 1, 2 };

        for (int i = 0; i < LODLevels.Length; i++)
        {
            Mesh m = new Mesh();
            m.vertices = MeshVerts.ToArray();
            m.triangles = MeshTris.ToArray();


            //Subdivide.SubdivideTriCPU(ref m, (int)Math.Pow(LODLevels[i], 4));

            Meshes[i] = m;
        }

        return Meshes;
    }

    /*
    private void Update()
    {
        float lodCalc = (Camera.main.transform.position).magnitude;
        if (lodCalc > 5000f)
            LOD = 0;
        else if (lodCalc > 2500f)
            LOD = 1;
        else if (lodCalc > 1000f)
            LOD = 2;
        else if (lodCalc > 500f)
            LOD = 3;
        else
            LOD = 4;

        if (lastLOD != LOD)
        {
            lastLOD = LOD;
            MeshLODs[LOD].RecalculateBounds();
            MeshLODs[LOD].bounds.Expand(1000f);
            mf.mesh = mc.sharedMesh = MeshLODs[LOD];
        }
    }
    */
        }

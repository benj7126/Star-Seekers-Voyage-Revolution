using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class Icosphere : MonoBehaviour
{
    // make compute shader to control lod of icosphere segments

    [SerializeField] private Mesh sourceMesh; // might want to split this so that the script dosent have to at some point
    [SerializeField] private GameObject SegmentPrefab;

    [SerializeField] private ComputeShader TriCS;
    [SerializeField] private ComputeShader CS;

    [SerializeField] private ComputeShader ST;
    private ComputeBuffer PositionsBuffer;
    private ComputeBuffer LODsBuffer;

    private IcosphereSegment[] Segments;

    [SerializeField] private int LowestQuality;

    [SerializeField] private int[] LODLevels;

    private bool hasBeenGenerated = false;

    [SerializeField] public GameObject WaterObject;

    // Start is called before the first frame update
    public void GenerateThisPlanet(float Height, Vector3 PlanetPosition, Material PlanetMaterial)
    {
        Mesh mesh = sourceMesh;
        List<int>  _Tris = mesh.triangles.ToList();
        List<Vector3>  _Verts = mesh.vertices.ToList();

        for (int i = 0; i < LowestQuality; i++)
            SplitAllFaces(_Tris, _Verts);

        int TriCount = _Tris.Count;
        int SegmentCount = _Tris.Count / 3;

        Vector3[] LocalSegmentPositions = new Vector3[SegmentCount];
        int[] LocalSegmentLods = new int[SegmentCount];

        Segments = new IcosphereSegment[SegmentCount];

        for (int i = 0; i < TriCount; i += 3)
        {
            Vector3 CenterNormalized = ((_Verts[_Tris[i]] + _Verts[_Tris[i + 1]] + _Verts[_Tris[i + 2]]) / 3f).normalized;
            Vector3 Center = CenterNormalized * Height;

            LocalSegmentPositions[i / 3] = Center;

            Mesh[] MeshLODs = GenMeshLOD(new List<Vector3> { _Verts[_Tris[i]], _Verts[_Tris[i + 1]], _Verts[_Tris[i + 2]] });

            GameObject Segment = Instantiate(SegmentPrefab, transform);
            IcosphereSegment ISegment = Segments[i / 3] = Segment.GetComponent<IcosphereSegment>();

            ISegment.MeshLODs = MeshLODs;
            ISegment.LODLevels = LODLevels;
            ISegment.QueuedLODs = Enumerable.Repeat<bool>(false, LODLevels.Length).ToArray();
            ISegment.GeneratedLODs = Enumerable.Repeat<bool>(false, LODLevels.Length).ToArray();
            ISegment.Height = Height;
            ISegment.PlanetPosition = PlanetPosition;

            LocalSegmentLods[i / 3] = 0;

            Segment.GetComponent<MeshRenderer>().sharedMaterial = PlanetMaterial;
        }

        PositionsBuffer = new ComputeBuffer(SegmentCount, sizeof(float) * 3);
        PositionsBuffer.SetData(LocalSegmentPositions);

        LODsBuffer = new ComputeBuffer(SegmentCount, sizeof(int));
        LODsBuffer.SetData(LocalSegmentLods);


        WaterObject.transform.localScale = new Vector3(Height*2, Height*2, Height*2);
        
        
        hasBeenGenerated = true;

        // RecalcLODs(); - dosent work...

        /*
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.vertices = Verts.ToArray();
        mesh.triangles = Tris.ToArray();
        */
    }

    public void OnDestroy()
    {
        PositionsBuffer.Release();
        LODsBuffer.Release();
    }

    float t = 2f;
    private void Update()
    {
        t += Time.deltaTime;
        if (hasBeenGenerated && t > 1f)
        {
            if ((Camera.main.transform.position - transform.position).magnitude < 51000)
                RecalcLODs();

            t = 0f;
        }
    }

    void RecalcLODs()
    {
        ST.SetVector("CamPos", Camera.main.transform.position - transform.position);

        ST.SetBuffer(0, "Verts", PositionsBuffer);
        ST.SetBuffer(0, "LODs", LODsBuffer);

        ST.Dispatch(0, Mathf.CeilToInt((float)PositionsBuffer.count / 512.0f), 1, 1);

        int[] LODs = new int[LODsBuffer.count];
        LODsBuffer.GetData(LODs);
        for (int i = 0; i < LODs.Length; i++)
        {
            //Debug.Log(i); Segments just isn't a thing...
            Segments[i].SetLOD(LODs[i]);
        }
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
            
            Meshes[i] = m;
        }

        return Meshes;
    }

    void SplitAllFaces(List<int> Tris, List<Vector3> Verts)
    {
        var TriCount = Tris.Count;
        for(int i = 0; i < TriCount; i+=3)
        {
            SplitFace(0, Tris, Verts);
        }
    }
    
    void SplitFace(int FaceIdx, List<int> Tris, List<Vector3> Verts)
    {
        // have a dictionary that makes sure the verts are shared correctly...

        int[] CurTri = Tris.GetRange(FaceIdx, 3).ToArray();
        Vector3[] CurTriVert =
        {
            Verts[CurTri[0]],
            Verts[CurTri[1]],
            Verts[CurTri[2]]
        };
        Tris.RemoveRange(FaceIdx, 3);

        for (int i = 0; i < 3; i++)
        {
            Verts.Add(Vector3.Lerp(CurTriVert[i], CurTriVert[(i+1)%3], 0.5f));
        }

        int l = Verts.Count;
        int[] TriAdd =
        {
            CurTri[0], l - 3, l - 1,
            l - 3, CurTri[1], l - 2,
            l - 2, CurTri[2], l - 1,
            l - 3, l - 2, l - 1
        };

        foreach (int t in TriAdd)
        {
            Tris.Add(t);
        }
    }
}

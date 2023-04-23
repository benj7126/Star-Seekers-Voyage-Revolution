using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Subdivide
{
    public static void SubdivideTri(ref Mesh mesh, ComputeShader TriCS, int Subdivisions)
    {
        ComputeBuffer Verts;
        ComputeBuffer Indices;

        int VertsLen = ((Subdivisions + 2) * (Subdivisions + 3)) / 2;
        int IndicesLen = (Subdivisions + 1) * (Subdivisions + 1) * 3;
        
        Verts = new ComputeBuffer(VertsLen, sizeof(float) * 3);
        Indices = new ComputeBuffer(IndicesLen, sizeof(int));

        TriCS.SetBuffer(0, "Verts", Verts);
        TriCS.SetBuffer(0, "Indices", Indices);


        Vector3 FirstCorner = mesh.vertices[0];
        Vector3 SecondCorner = mesh.vertices[1];
        Vector3 ThirdCorner = mesh.vertices[2];

        Vector3 ToSecondCorner = SecondCorner - FirstCorner;
        ToSecondCorner /= (Subdivisions + 1);
        Vector3 ToThirdCorner = ThirdCorner - FirstCorner;
        ToThirdCorner /= (Subdivisions + 1);
        
        TriCS.SetVector("FirstCorner", FirstCorner);
        TriCS.SetVector("ToSecondCorner", ToSecondCorner);
        TriCS.SetVector("ToThirdCorner", ToThirdCorner);
        
        TriCS.SetInt("Subdivisions", Subdivisions);
        
        TriCS.SetInt("NumVerts", VertsLen);
        
        TriCS.SetInt("FirstTriLen", ((Subdivisions+1) * (Subdivisions+2))/2);
        
        TriCS.Dispatch(0, Mathf.CeilToInt((float)(Subdivisions+2) / 32.0f), Mathf.CeilToInt((float)(Subdivisions + 2)/32.0f), 1);
        //TriCS.Dispatch(0, Subdivisions+2, Subdivisions + 2, 1);
        
        Vector3[] VertsArr = new Vector3[VertsLen];
        Verts.GetData(VertsArr);

        int[] IndicesArr = new int[IndicesLen];
        Indices.GetData(IndicesArr);

        Verts.Release();
        Indices.Release();
        
        Mesh m = new Mesh();
        m.indexFormat = IndexFormat.UInt32;
        m.vertices = VertsArr;
        m.triangles = IndicesArr;
        
        
        mesh = m;
    }
    
    public static void SubdivideTriCPU(ref Mesh mesh, int Subdivisions)
    {
        int VertsLen = ((Subdivisions + 2) * (Subdivisions + 3)) / 2;
        int IndicesLen = (Subdivisions + 1) * (Subdivisions + 1) * 3;

        Vector3 FirstCorner = mesh.vertices[0];
        Vector3 SecondCorner = mesh.vertices[1];
        Vector3 ThirdCorner = mesh.vertices[2];

        Vector3 ToSecondCorner = SecondCorner - FirstCorner;
        ToSecondCorner /= (Subdivisions + 1);
        Vector3 ToThirdCorner = ThirdCorner - FirstCorner;
        ToThirdCorner /= (Subdivisions + 1);
        
        Vector3[] VertsArr = new Vector3[VertsLen];
        int[] IndicesArr = new int[IndicesLen*3];

        int FirstTriLen = ((Subdivisions + 1) * (Subdivisions + 2)) / 2;

        Parallel.For(0, Subdivisions + 2, x =>
        {
            Vector3 StartCorner = FirstCorner + ToThirdCorner * x;
            Parallel.For(0, Subdivisions + 2, y =>
            {
                if (y >= Subdivisions + 2 - x)
                    return;
                int tmpIdx = 0;
                for (int i = 0; i < x; i++)
                {
                    tmpIdx += Subdivisions + 2 - i;
                }

                VertsArr[y + tmpIdx] = StartCorner + ToSecondCorner * y;

                int Idx = y + tmpIdx;

                if (Idx > Subdivisions + 1)
                {
                    int TriIdx = Idx - (Subdivisions + 2);

                    IndicesArr[TriIdx * 3 + 0] = Idx;
                    IndicesArr[TriIdx * 3 + 1] = Idx - (Subdivisions + 2 - x + 1);
                    IndicesArr[TriIdx * 3 + 2] = Idx - (Subdivisions + 2 - x + 1) + 1;

                    int InvIDX = (Subdivisions + 1) - x;
                    int NextEnd = VertsLen - (InvIDX * (InvIDX + 1)) / 2 - 1;

                    if (Idx != NextEnd)
                    {
                        TriIdx = FirstTriLen + Idx - (Subdivisions + 2) - (x - 1);
                        IndicesArr[TriIdx * 3 + 0] = Idx;
                        IndicesArr[TriIdx * 3 + 1] = Idx - (Subdivisions + 2 - x + 1) + 1;
                        IndicesArr[TriIdx * 3 + 2] = Idx + 1;
                    }
                }
            });
        });
        /*
        for (int x = 0; x < Subdivisions + 2; x++)
        {
            Vector3 StartCorner = FirstCorner + ToThirdCorner * x;
            for (int y = 0; y < Subdivisions + 2; y++)
            {
                if (y >= Subdivisions + 2 - x)
                    continue;
                int tmpIdx = 0;
                for (int i = 0; i < x; i++)
                {
                    tmpIdx += Subdivisions + 2 - i;
                }

                Debug.Log($"TEST: {Subdivisions} - {VertsLen} - {y + tmpIdx} - {tmpIdx}");
                VertsArr[y + tmpIdx] = StartCorner + ToSecondCorner * y;

                int Idx = y + tmpIdx;

                if (Idx > Subdivisions + 1)
                {
                    int TriIdx = Idx - (Subdivisions + 2);
                    
                    IndicesArr[TriIdx * 3 + 0] = Idx;
                    IndicesArr[TriIdx * 3 + 1] = Idx - (Subdivisions + 2 - x + 1);
                    IndicesArr[TriIdx * 3 + 2] = Idx - (Subdivisions + 2 - x + 1) + 1;

                    int InvIDX = (Subdivisions + 1) - x;
                    int NextEnd = VertsLen - (InvIDX * (InvIDX + 1)) / 2 - 1;

                    if (Idx != NextEnd)
                    {
                        TriIdx = FirstTriLen + Idx - (Subdivisions + 2) - (x - 1);
                        IndicesArr[TriIdx * 3 + 0] = Idx;
                        IndicesArr[TriIdx * 3 + 1] = Idx - (Subdivisions + 2 - x + 1) + 1;
                        IndicesArr[TriIdx * 3 + 2] = Idx + 1;
                    }
                }
            }
        }*/

        
        Mesh m = new Mesh();
        m.indexFormat = IndexFormat.UInt32;
        m.vertices = VertsArr;
        m.triangles = IndicesArr;
        
        mesh = m;
    }
}
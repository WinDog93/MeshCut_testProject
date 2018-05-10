using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slice
{
    public List<Vector3> objPosList;
    public List<int> objTriList;
    public List<Vector3> capPosList;
    public List<int> capTriList;
    public List<Vector3> posList;
    public List<int> triList;
    public Vector3 position;


    public void Init()
    {
        objPosList = new List<Vector3>();
        objTriList = new List<int>();
        capPosList = new List<Vector3>();
        capTriList = new List<int>();
        posList = new List<Vector3>();
        triList = new List<int>();
    }
}

public class CuttingMesh : MonoBehaviour
{
    private Material material;
    private Mesh cutMesh;
    private Vector3 meshPos;
    private Vector3 meshScale;
    private int[] meshTriangles;
    private Vector3[] meshVertices;
    private Vector3[] meshNormals;


    private void Start()
    {
        cutMesh = GetComponent<MeshFilter>().mesh;
        material = GetComponent<MeshRenderer>().material;
    }

    public void Cut(Plane plane)
    {
        Slice[] slice = new Slice[2];
        for (int i = 0; i < slice.Length; i++)
        {
            slice[i] = new Slice();
        }
        meshPos = transform.position;
        meshScale = transform.localScale;
        meshTriangles = cutMesh.triangles;
        meshVertices = cutMesh.vertices;
        meshNormals = cutMesh.normals;
        int[] triangle = new int[3];
        var verts = new List<Vector3>();

        for (var i = 0; i < meshTriangles.Length; i += 3)
        {
            slice[0].posList.Clear();
            slice[0].triList.Clear();
            slice[1].posList.Clear();
            slice[1].triList.Clear();
            verts.Clear();

            for (int j = 0; j < 3; j++)
            {
                triangle[j] = meshTriangles[i + j];
                verts.Add(Vector3.Scale(meshVertices[triangle[j]], meshScale));
            }

            Vector3 normal = Vector3.Cross(meshVertices[triangle[2]] - meshVertices[triangle[0]], meshVertices[triangle[1]] - meshVertices[triangle[0]]);
            for (int k = 0; k < verts.Count; k++)
            {
                if (plane.GetSide(verts[k]))
                    slice[0].posList.Add(verts[k]);
                else
                    slice[1].posList.Add(verts[k]);
            }

            if (slice[0].posList.Count > 0 && slice[1].posList.Count > 0)
            {
                float distance = 0;
                Vector3[] vc = new Vector3[3];
                if (slice[1].posList.Count < slice[0].posList.Count)
                {
                    vc[0] = slice[1].posList[0];
                    vc[1] = slice[0].posList[0];
                    vc[2] = slice[0].posList[1];
                }
                else
                {
                    vc[0] = slice[0].posList[0];
                    vc[1] = slice[1].posList[0];
                    vc[2] = slice[1].posList[1];
                }

                Ray ray1 = new Ray(vc[0], (vc[1] - vc[0]).normalized);
                plane.Raycast(ray1, out distance);
                slice[0].position = ray1.GetPoint(distance);

                Ray ray2 = new Ray(vc[0], (vc[2] - vc[0]).normalized);
                plane.Raycast(ray2, out distance);
                slice[1].position = ray2.GetPoint(distance);
            }

            for (int l = 0; l < slice.Length; l++)
            {
                if (slice[0].posList.Count > 0 && slice[1].posList.Count > 0)
                {
                    slice[l].posList.Add(slice[0].position);
                    slice[l].posList.Add(slice[1].position);
                    slice[l].capPosList.Add(slice[0].position);
                    slice[l].capPosList.Add(slice[1].position);
                }
                if (slice[l].posList.Count > 0)
                {
                    List<int> tris1 = CreateTriangles(slice[l].posList, normal);
                    int triIdx = slice[l].objPosList.Count;
                    slice[l].objPosList.AddRange(slice[l].posList);
                    foreach (var triI in tris1)
                    {
                        slice[l].objTriList.Add(triI + triIdx);
                    }
                }
            }
        }

        for (int j = 0; j < slice.Length; j++)
        {
            Limitation(plane, slice[j].capPosList, slice[j].capTriList, j * (-1));
        }

        for (int l = 0; l < slice.Length; l++)
        {
            int tri1Idx = slice[l].objPosList.Count;
            slice[l].objPosList.AddRange(slice[l].capPosList);
            foreach (var idx1 in slice[l].capTriList)
            {
                slice[l].objTriList.Add(tri1Idx + idx1);
            }
        }

        Mesh[] sliceObMeshes = new Mesh[2];
        for (int i = 0; i < sliceObMeshes.Length; i++)
        {
            sliceObMeshes[i] = CreateMesh(slice[i].objPosList, slice[i].objTriList);
        }
        CreateSlice(sliceObMeshes[0]);
        ChangeMeshObj(gameObject, sliceObMeshes[1]);
        transform.localScale = Vector3.one;
    }


    private List<int> CreateTriangles(List<Vector3> pos, Vector3 normal)
    {
        if (pos.Count >= 3)
        {
            List<int> triangles = new List<int>();
            List<int> idx = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                idx.Add(0);
            }
            Vector3 cross = Vector3.zero;
            float inner = 0.0f;

            for (int i = 0; i < pos.Count; i += 3)
            {
                for (int k = 1; k < 4; k++)
                {
                    idx[k] = idx[0] + (k - 1);
                }

                cross = Vector3.Cross(pos[idx[3]] - pos[idx[1]], pos[idx[2]] - pos[idx[1]]);
                inner = Vector3.Dot(cross, normal);
                if (inner < 0)
                {
                    idx[1] = idx[3];
                    idx[3] = idx[0];
                }
                for (int j = 1; j < 4; j++)
                {
                    triangles.Add(idx[j]);
                }
                idx[0]++;
            }

            return triangles;
        }
        else
        {
            return null;
        }
    }

    private void Limitation(Plane plane, List<Vector3> posList, List<int> tris, int front)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < posList.Count; i++)
        {
            center += posList[i];
        }
        center = center / posList.Count;
        posList.Add(center);
        int centerIdx = posList.Count - 1;
        int[] idx = new int[3];
        for (int i = 0; i < posList.Count - 1; i += 2)
        {
            idx[0] = centerIdx;
            idx[1] = i;
            idx[2] = i + 1;
            Vector3 crs = Vector3.Cross(posList[idx[2]] - posList[idx[0]], posList[idx[1]] - posList[idx[0]]);
            float inner = Vector3.Dot(crs, plane.normal);
            if (front >= 0 && inner < 0)
            {
                idx[0] = idx[2];
                idx[2] = centerIdx;
            }
            if (front < 0 && inner > 0)
            {
                idx[0] = idx[2];
                idx[2] = centerIdx;
            }
            for (int h = 0; h < idx.Length; h++)
            {
                tris.Add(idx[h]);
            }
        }
    }

    private Mesh CreateMesh(List<Vector3> verts, List<int> tris)
    {
        if (verts.Count != 0 && tris.Count != 0)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        return null;
    }

    private void ChangeMeshObj(GameObject ob, Mesh mesh)
    {
        Collider tmpColl = ob.GetComponent<Collider>();

        if (tmpColl != null)
        {
            Destroy(tmpColl);
        }
        ob.GetComponent<MeshFilter>().mesh = mesh;
        ob.GetComponent<MeshRenderer>().material = material;
        MeshCollider collider = ob.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.inflateMesh = true;
        collider.convex = true;
    }

    private void CreateSlice(Mesh mesh)
    {
        GameObject ob = new GameObject("new slice", typeof(MeshFilter), typeof(MeshRenderer));
        ob.transform.position = transform.position;
        ChangeMeshObj(ob, mesh);
        ob.AddComponent<CuttingMesh>();
    }

}

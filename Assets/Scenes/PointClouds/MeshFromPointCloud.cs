using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshFromPointCloud : MonoBehaviour
{
    public void GenerateMesh()
    {
        string scanFolder =
            PlayerPrefs.GetString(
                "LastScanFolder",
                "");

        if (string.IsNullOrEmpty(scanFolder))
        {
            Debug.LogError(
                "No Scan Found");

            return;
        }

        string jsonPath =
            Path.Combine(
                scanFolder,
                "pointcloud.json");

        if (!File.Exists(jsonPath))
        {
            Debug.LogError(
                "pointcloud.json not found");

            return;
        }

        string json =
            File.ReadAllText(
                jsonPath);

        PointCloudData data =
            JsonUtility.FromJson<PointCloudData>(
                json);

        BuildMesh(
            data.points);
    }

    private void BuildMesh(
        List<SerializableVector3> points)
    {
        if (points.Count < 3)
        {
            Debug.LogError(
                "Not enough points");

            return;
        }

        Mesh mesh =
            new Mesh();

        mesh.indexFormat =
            UnityEngine.Rendering
            .IndexFormat.UInt32;

        Vector3[] vertices =
            new Vector3[
                points.Count];

        for (int i = 0;
            i < points.Count;
            i++)
        {
            vertices[i] =
                new Vector3(
                    points[i].x,
                    points[i].y,
                    points[i].z);
        }

        List<int> triangles =
            new List<int>();

        for (int i = 0;
            i < points.Count - 2;
            i += 3)
        {
            triangles.Add(i);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles.ToArray();

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>()
            .mesh = mesh;

        Debug.Log(
            $"Verts: {mesh.vertexCount}");

        Debug.Log(
            $"Tris: {mesh.triangles.Length / 3}");
    }
}
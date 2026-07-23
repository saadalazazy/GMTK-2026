using DG.Tweening.Core.Easing;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterManager : MonoBehaviour
{
    private MeshFilter meshFilter;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Update()
    {
        // Get all vertices of the water plane mesh
        Vector3[] vertices = meshFilter.mesh.vertices;

        // Loop through each vertex and set its height (y) using the WaveManager
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = WaterWaveManager.Instance.GetWaveHeight(transform.position.x + vertices[i].x);
        }

        // Apply updated vertices back to the mesh and recalculate lighting normals
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateNormals();
    }
}
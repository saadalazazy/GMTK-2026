using System;
using UnityEngine;
using Ditzelgames;

[RequireComponent(typeof(MeshRenderer))]
public class Waves : MonoBehaviour
{
    //Public Properties
    public int Dimension = 10;
    public float UVScale = 2f;
    public Octave[] Octaves;

    // Max octaves the shader supports (must match the array size declared
    // in WaveShader.shader: _OctaveScaleHeight[MAX_OCTAVES] / _OctaveSpeed[MAX_OCTAVES])
    const int MaxOctaves = 8;

    //Mesh
    protected MeshFilter MeshFilter;
    protected MeshRenderer MeshRenderer;
    protected Mesh Mesh;
    protected Material MaterialInstance;

    void Start()
    {
        Mesh = new Mesh();
        Mesh.name = gameObject.name;

        Mesh.vertices = GenerateVerts();
        Mesh.triangles = GenerateTries();
        Mesh.uv = GenerateUVs();
        Mesh.RecalculateNormals();
        Mesh.RecalculateBounds();
        // The mesh never moves on the CPU again after this, so give it a
        // generous bounds box up front (avoids incorrect culling once the
        // GPU pushes vertices outside the flat plane's original bounds).
        var b = Mesh.bounds;
        var maxHeight = 0f;
        foreach (var o in Octaves) maxHeight += Mathf.Abs(o.height);
        b.Expand(new Vector3(0, maxHeight * 2f, 0));
        Mesh.bounds = b;

        MeshFilter = gameObject.AddComponent<MeshFilter>();
        MeshFilter.mesh = Mesh;

        MeshRenderer = GetComponent<MeshRenderer>();
        MaterialInstance = MeshRenderer.material; // instantiates a unique material

        BuildPermutationTexture();
        PushOctavesToShader();
    }

    // Bakes the shared permutation table into a 256x1 texture the shader
    // samples from, so GPU noise uses the exact same hash as the CPU noise.
    void BuildPermutationTexture()
    {
        var tex = new Texture2D(256, 1, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        var colors = new Color[256];
        for (int i = 0; i < 256; i++)
        {
            float v = PerlinNoise.Permutation[i] / 255f;
            colors[i] = new Color(v, v, v, v);
        }
        tex.SetPixels(colors);
        tex.Apply(false, true);

        MaterialInstance.SetTexture("_PermTable", tex);
    }

    void PushOctavesToShader()
    {
        var scaleHeight = new Vector4[MaxOctaves];
        var speed = new Vector4[MaxOctaves];

        int count = Mathf.Min(Octaves.Length, MaxOctaves);
        for (int i = 0; i < count; i++)
        {
            scaleHeight[i] = new Vector4(Octaves[i].scale.x, Octaves[i].scale.y, Octaves[i].height, Octaves[i].alternate ? 1f : 0f);
            speed[i] = new Vector4(Octaves[i].speed.x, Octaves[i].speed.y, 0f, 0f);
        }

        MaterialInstance.SetVectorArray("_OctaveScaleHeight", scaleHeight);
        MaterialInstance.SetVectorArray("_OctaveSpeed", speed);
        MaterialInstance.SetFloat("_Dimension", Dimension);

        if (Octaves.Length > MaxOctaves)
            Debug.LogWarning($"Waves: {Octaves.Length} octaves configured, but WaveShader.shader only supports {MaxOctaves}. Extra octaves are ignored by the GPU (raise MaxOctaves in both Waves.cs and the shader if you need more).");
    }

    /// <summary>
    /// Analytic wave height at a world position, used by floating objects.
    /// This purposefully does NOT read Mesh.vertices any more (the CPU copy
    /// of the mesh is flat and stays flat - only the GPU displaces it), and
    /// it purposefully does NOT do 4-corner interpolation any more: since
    /// the displacement is a continuous function of (x, z), we can just
    /// evaluate it directly at the exact local position instead of
    /// sampling/blending the nearest grid vertices.
    /// </summary>
    public float GetHeight(Vector3 position)
    {
        var scale = new Vector3(1f / transform.lossyScale.x, 0f, 1f / transform.lossyScale.z);
        var localPos = Vector3.Scale(position - transform.position, scale);

        float x = Mathf.Clamp(localPos.x, 0, Dimension);
        float z = Mathf.Clamp(localPos.z, 0, Dimension);

        float y = 0f;
        for (int o = 0; o < Octaves.Length; o++)
        {
            if (Octaves[o].alternate)
            {
                float perl = PerlinNoise.Perlin2D((x * Octaves[o].scale.x) / Dimension, (z * Octaves[o].scale.y) / Dimension) * Mathf.PI * 2f;
                y += Mathf.Cos(perl + Octaves[o].speed.magnitude * Time.time) * Octaves[o].height;
            }
            else
            {
                float perl = PerlinNoise.Perlin2D((x * Octaves[o].scale.x + Time.time * Octaves[o].speed.x) / Dimension, (z * Octaves[o].scale.y + Time.time * Octaves[o].speed.y) / Dimension) - 0.5f;
                y += perl * Octaves[o].height;
            }
        }

        // NOTE: previous version omitted transform.position.y here, which
        // only "worked" if the water plane sat at world Y=0. Fixed so the
        // Waves object can be placed anywhere.
        return y * transform.lossyScale.y + transform.position.y;
    }

    private Vector3[] GenerateVerts()
    {
        var verts = new Vector3[(Dimension + 1) * (Dimension + 1)];

        for (int x = 0; x <= Dimension; x++)
            for (int z = 0; z <= Dimension; z++)
                verts[index(x, z)] = new Vector3(x, 0, z);

        return verts;
    }

    private int[] GenerateTries()
    {
        var tries = new int[Mesh.vertices.Length * 6];

        for (int x = 0; x < Dimension; x++)
        {
            for (int z = 0; z < Dimension; z++)
            {
                tries[index(x, z) * 6 + 0] = index(x, z);
                tries[index(x, z) * 6 + 1] = index(x + 1, z + 1);
                tries[index(x, z) * 6 + 2] = index(x + 1, z);
                tries[index(x, z) * 6 + 3] = index(x, z);
                tries[index(x, z) * 6 + 4] = index(x, z + 1);
                tries[index(x, z) * 6 + 5] = index(x + 1, z + 1);
            }
        }

        return tries;
    }

    private Vector2[] GenerateUVs()
    {
        var uvs = new Vector2[Mesh.vertices.Length];

        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                var vec = new Vector2((x / UVScale) % 2, (z / UVScale) % 2);
                uvs[index(x, z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }

        return uvs;
    }

    private int index(int x, int z) => x * (Dimension + 1) + z;

    // Update() is gone on purpose: the CPU no longer writes Mesh.vertices or
    // calls RecalculateNormals() every frame. All visible motion now happens
    // in WaveShader.shader's vertex function, driven by _Time and the
    // octave arrays pushed once (and whenever Octaves changes, see
    // OnValidate below).

#if UNITY_EDITOR
    void OnValidate()
    {
        if (MaterialInstance != null)
            PushOctavesToShader();
    }
#endif

    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }
}

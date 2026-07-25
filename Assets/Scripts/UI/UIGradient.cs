using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    [SerializeField] private Color colorTop = Color.white;
    [SerializeField] private Color colorBottom = Color.black;

    public Color ColorTop
    {
        get => colorTop;
        set { colorTop = value; if (graphic != null) graphic.SetVerticesDirty(); }
    }

    public Color ColorBottom
    {
        get => colorBottom;
        set { colorBottom = value; if (graphic != null) graphic.SetVerticesDirty(); }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        UIVertex vertex = new UIVertex();
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        // Find vertical bounds of the image mesh
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            minY = Mathf.Min(minY, vertex.position.y);
            maxY = Mathf.Max(maxY, vertex.position.y);
        }

        float height = maxY - minY;

        // Interpolate colors across vertices
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            float t = height == 0 ? 0 : (vertex.position.y - minY) / height;
            vertex.color *= Color.Lerp(colorBottom, colorTop, t);

            vh.SetUIVertex(vertex, i);
        }
    }
}

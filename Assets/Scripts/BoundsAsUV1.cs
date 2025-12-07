using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class BoundsAsUV1 : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        var verts = ListPool<UIVertex>.Get();
        vh.GetUIVertexStream(verts);

        if (verts.Count == 0)
        {
            ListPool<UIVertex>.Release(verts);
            return;
        }

        var minX = verts[0].position.x;
        var maxX = minX;
        var minY = verts[0].position.y;
        var maxY = minY;

        for (var i = 1; i < verts.Count; i++)
        {
            var p = verts[i].position;
            if (p.x < minX)
                minX = p.x;
            if (p.x > maxX)
                maxX = p.x;
            if (p.y < minY)
                minY = p.y;
            if (p.y > maxY)
                maxY = p.y;
        }

        var sizeX = Mathf.Max(0.0001f, maxX - minX);
        var sizeY = Mathf.Max(0.0001f, maxY - minY);

        for (var i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            var p = v.position;

            v.uv1 = new Vector4(
                (p.x - minX) / sizeX,
                (p.y - minY) / sizeY,
                sizeX,
                sizeY
            );

            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);

        ListPool<UIVertex>.Release(verts);
    }
}
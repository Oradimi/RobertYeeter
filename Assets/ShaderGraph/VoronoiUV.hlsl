void VoronoiUV_float(
    float2 _UV,
    float _AngleOffset,
    float _CellDensity,
    out float Out,
    out float Cells,
    out float2 Center)
{
    float2 g = floor(_UV * _CellDensity);
    float2 f = frac(_UV * _CellDensity);

    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 lattice = float2(x, y);
            float2 sUV = lattice + g;

            float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
            sUV = frac(sin(mul(sUV, m)) * 46839.32);

            float2 offset = float2(
                sin(sUV.y * _AngleOffset) * 0.5 + 0.5,
                cos(sUV.x * _AngleOffset) * 0.5 + 0.5
            );

            float d = distance(lattice + offset, f);

            if (d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
                Center = (offset + lattice + g) / _CellDensity;
            }
        }
    }
}
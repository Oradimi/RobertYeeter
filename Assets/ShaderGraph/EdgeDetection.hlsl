#pragma vertex Vert
#pragma fragment frag

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

float _OutlineThickness;
float3 _OuterLightenRGB;
float3 _InnerDarkenRGB;

float3 SampleSceneNormalsRemapped(float2 uv)
{
    return SampleSceneNormals(uv) * 0.5 + 0.5;
}

half4 frag(Varyings IN) : SV_Target
{
    float2 uv = IN.texcoord;

    // diagonal directions
    float2 dirs[4] = {
        float2(-1,  1),
        float2( 1,  1),
        float2(-1, -1),
        float2( 1, -1)
    };

    // center pixel data
    float centerDepth = SampleSceneDepth(uv);
    if (centerDepth >= 0.9999) discard;
    float3 centerNorm = SampleSceneNormalsRemapped(uv);
    float3 sceneColor = SampleSceneColor(uv);

    float depthThresh = 1.0 / 200.0;
    float normThresh  = 1.0 /   4.0;

    bool isOuter = false, isInner = false;

    // halfThick = _OutlineThickness * 0.5;
    float2 texelSize = 1.0 / _ScreenParams.xy;
    float halfThick = _OutlineThickness * 0.5;
    float innerOffset = 0.5 - halfThick;    // <- flipped

    for (int i = 0; i < 4; i++)
    {
        // still add, but innerOffset is now negative when halfThick > 0.5
        float2 sampleUV = uv + dirs[i] * innerOffset * texelSize;
        float   nDepth  = SampleSceneDepth(sampleUV);
        float3  nNorm   = SampleSceneNormalsRemapped(sampleUV);

        if (abs(nDepth - centerDepth) > depthThresh && centerDepth < nDepth)
            isOuter = true;
        else if (dot(centerNorm, nNorm) < normThresh && centerDepth > nDepth)
            isInner = true;
    }

    float3 result = sceneColor;
    if (isOuter)  result = saturate(sceneColor + _OuterLightenRGB);
    else if (isInner) result = saturate(sceneColor - _InnerDarkenRGB);

    return float4(result, 1);
}
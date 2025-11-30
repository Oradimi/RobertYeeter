Shader "Shader Graphs/UI_Shader"
{
    Properties
    {
        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
        _Shift("Shift", Float) = 0
        [ToggleUI]_HasTransition("HasTransition", Float) = 0
        _Transition("Transition", Float) = 0
        [ToggleUI]_IsVertical("IsVertical", Float) = 0
        _CellDensity("CellDensity", Float) = 8
        _Scale("Scale", Float) = 8
        _Speed("Speed", Float) = 0.1
        _ScrollDirection("ScrollDirection", Vector, 2) = (1, 1, 0, 0)
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent"
            "DisableBatching"="False"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalUnlitSubTarget"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                // LightMode: <None>
            }
        
        // Render State
        Cull Back
        Blend SrcAlpha One, One One
        ZTest LEqual
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
        #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
        #pragma shader_feature _ _SAMPLE_GI
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_COLOR
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_COLOR
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_UNLIT
        #define _FOG_FRAGMENT 1
        #define _SURFACE_TYPE_TRANSPARENT 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 color : COLOR;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 color;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float2 NDCPosition;
             float2 PixelPosition;
             float4 VertexColor;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 color : INTERP0;
             float3 positionWS : INTERP1;
             float3 normalWS : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.color.xyzw = input.color;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.color = input.color.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _Shift;
        float _Transition;
        float _IsVertical;
        float _CellDensity;
        float _Scale;
        float _Speed;
        float2 _ScrollDirection;
        float _HasTransition;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
        {
            Hash_Tchou_2_2_float(UV, UV);
            return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
        }
        
        void Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
        {
            float2 g = floor(UV * CellDensity);
            float2 f = frac(UV * CellDensity);
            float t = 8.0;
            float3 res = float3(8.0, 0.0, 0.0);
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 lattice = float2(x, y);
                    float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
                    float d = distance(lattice + offset, f);
                    if (d < res.x)
                    {
                        res = float3(d, offset.x, offset.y);
                        Out = res.x;
                        Cells = res.y;
                    }
                }
            }
        }
        
        float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0; Hash_Tchou_2_1_float(c0, r0);
            float r1; Hash_Tchou_2_1_float(c1, r1);
            float r2; Hash_Tchou_2_1_float(c2, r2);
            float r3; Hash_Tchou_2_1_float(c3, r3);
            float bottomOfGrid = lerp(r0, r1, f.x);
            float topOfGrid = lerp(r2, r3, f.x);
            float t = lerp(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        
        void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
        {
            float freq, amp;
            Out = 0.0f;
            freq = pow(2.0, float(0));
            amp = pow(0.5, float(3-0));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Lerp_float(float A, float B, float T, out float Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Split_af3a6d77e9044b9e89f9114690e89ff1_R_1_Float = IN.VertexColor[0];
            float _Split_af3a6d77e9044b9e89f9114690e89ff1_G_2_Float = IN.VertexColor[1];
            float _Split_af3a6d77e9044b9e89f9114690e89ff1_B_3_Float = IN.VertexColor[2];
            float _Split_af3a6d77e9044b9e89f9114690e89ff1_A_4_Float = IN.VertexColor[3];
            float3 _Vector3_3f5c06b7f49e421fb71322e3a501bc47_Out_0_Vector3 = float3(_Split_af3a6d77e9044b9e89f9114690e89ff1_R_1_Float, _Split_af3a6d77e9044b9e89f9114690e89ff1_G_2_Float, _Split_af3a6d77e9044b9e89f9114690e89ff1_B_3_Float);
            float _Property_49f28783e83742099b1dc228a2a37b62_Out_0_Boolean = _HasTransition;
            float4 _ScreenPosition_1135ca87c6f34ab389150df723ce7adc_Out_0_Vector4 = float4(IN.NDCPosition.xy * 2 - 1, 0, 0);
            float _Split_4bf70fcd1b8345c0a6ba3b13dba13def_R_1_Float = _ScreenPosition_1135ca87c6f34ab389150df723ce7adc_Out_0_Vector4[0];
            float _Split_4bf70fcd1b8345c0a6ba3b13dba13def_G_2_Float = _ScreenPosition_1135ca87c6f34ab389150df723ce7adc_Out_0_Vector4[1];
            float _Split_4bf70fcd1b8345c0a6ba3b13dba13def_B_3_Float = _ScreenPosition_1135ca87c6f34ab389150df723ce7adc_Out_0_Vector4[2];
            float _Split_4bf70fcd1b8345c0a6ba3b13dba13def_A_4_Float = _ScreenPosition_1135ca87c6f34ab389150df723ce7adc_Out_0_Vector4[3];
            float _Multiply_4df156891a8f42bcae783555cd99f6a3_Out_2_Float;
            Unity_Multiply_float_float(_Split_4bf70fcd1b8345c0a6ba3b13dba13def_G_2_Float, _ScreenParams.y, _Multiply_4df156891a8f42bcae783555cd99f6a3_Out_2_Float);
            float _Divide_6bbf518a2df94480bd532418eb0dcf5b_Out_2_Float;
            Unity_Divide_float(_Multiply_4df156891a8f42bcae783555cd99f6a3_Out_2_Float, _ScreenParams.x, _Divide_6bbf518a2df94480bd532418eb0dcf5b_Out_2_Float);
            float2 _Vector2_ca4b324dd3fa456d823f8294b96ff93c_Out_0_Vector2 = float2(_Split_4bf70fcd1b8345c0a6ba3b13dba13def_R_1_Float, _Divide_6bbf518a2df94480bd532418eb0dcf5b_Out_2_Float);
            float2 _Property_7d37925c0a1f4da98af1449e056188e0_Out_0_Vector2 = _ScrollDirection;
            float _Property_77b6ad482de342c0b4b2a48e3e218a6c_Out_0_Float = _Speed;
            float _Multiply_d49765067e0d4435a54b6760ebde9af4_Out_2_Float;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_77b6ad482de342c0b4b2a48e3e218a6c_Out_0_Float, _Multiply_d49765067e0d4435a54b6760ebde9af4_Out_2_Float);
            float2 _TilingAndOffset_bd5c1210fb4c47bbb825e5fe7b510a8d_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_ca4b324dd3fa456d823f8294b96ff93c_Out_0_Vector2, _Property_7d37925c0a1f4da98af1449e056188e0_Out_0_Vector2, (_Multiply_d49765067e0d4435a54b6760ebde9af4_Out_2_Float.xx), _TilingAndOffset_bd5c1210fb4c47bbb825e5fe7b510a8d_Out_3_Vector2);
            float _Property_25f4aefc805a44ae93a3d9505881618d_Out_0_Float = _CellDensity;
            float _Voronoi_46b67fb55e404a08875985e50aac9bc8_Out_3_Float;
            float _Voronoi_46b67fb55e404a08875985e50aac9bc8_Cells_4_Float;
            Unity_Voronoi_Deterministic_float(_TilingAndOffset_bd5c1210fb4c47bbb825e5fe7b510a8d_Out_3_Vector2, IN.TimeParameters.x, _Property_25f4aefc805a44ae93a3d9505881618d_Out_0_Float, _Voronoi_46b67fb55e404a08875985e50aac9bc8_Out_3_Float, _Voronoi_46b67fb55e404a08875985e50aac9bc8_Cells_4_Float);
            float _Property_5b7af6b873dd4904bdc45c5ed4f147b3_Out_0_Float = _Scale;
            float _SimpleNoise_e6507eacb3ea4059b7f6b6403749c868_Out_2_Float;
            Unity_SimpleNoise_Deterministic_float(_TilingAndOffset_bd5c1210fb4c47bbb825e5fe7b510a8d_Out_3_Vector2, _Property_5b7af6b873dd4904bdc45c5ed4f147b3_Out_0_Float, _SimpleNoise_e6507eacb3ea4059b7f6b6403749c868_Out_2_Float);
            float _Multiply_3a2533948d5c42c28bdc60f46f42a310_Out_2_Float;
            Unity_Multiply_float_float(_Voronoi_46b67fb55e404a08875985e50aac9bc8_Cells_4_Float, _SimpleNoise_e6507eacb3ea4059b7f6b6403749c868_Out_2_Float, _Multiply_3a2533948d5c42c28bdc60f46f42a310_Out_2_Float);
            float _Property_e81e5fadb6904da68c682986c42f9807_Out_0_Float = _Transition;
            float _Property_59d7731d48c142ea9c17a649dcbde84f_Out_0_Boolean = _IsVertical;
            float _Split_db7bf99f8a9343aa964f53bc0dfa5e72_R_1_Float = _Vector2_ca4b324dd3fa456d823f8294b96ff93c_Out_0_Vector2[0];
            float _Split_db7bf99f8a9343aa964f53bc0dfa5e72_G_2_Float = _Vector2_ca4b324dd3fa456d823f8294b96ff93c_Out_0_Vector2[1];
            float _Split_db7bf99f8a9343aa964f53bc0dfa5e72_B_3_Float = 0;
            float _Split_db7bf99f8a9343aa964f53bc0dfa5e72_A_4_Float = 0;
            float _Branch_b1ee125b1ce5463dadaf6f0fe36ac26f_Out_3_Float;
            Unity_Branch_float(_Property_59d7731d48c142ea9c17a649dcbde84f_Out_0_Boolean, _Split_db7bf99f8a9343aa964f53bc0dfa5e72_G_2_Float, _Split_db7bf99f8a9343aa964f53bc0dfa5e72_R_1_Float, _Branch_b1ee125b1ce5463dadaf6f0fe36ac26f_Out_3_Float);
            float _Property_d997c29bcb87439e8c5d9d014a93db49_Out_0_Float = _Shift;
            float _Subtract_3dd4f7cc16c7404eac6e64fd8f91c37f_Out_2_Float;
            Unity_Subtract_float(_Branch_b1ee125b1ce5463dadaf6f0fe36ac26f_Out_3_Float, _Property_d997c29bcb87439e8c5d9d014a93db49_Out_0_Float, _Subtract_3dd4f7cc16c7404eac6e64fd8f91c37f_Out_2_Float);
            float _Lerp_600f0b288c2142edbde191762355ce71_Out_3_Float;
            Unity_Lerp_float(_Multiply_3a2533948d5c42c28bdc60f46f42a310_Out_2_Float, _Property_e81e5fadb6904da68c682986c42f9807_Out_0_Float, _Subtract_3dd4f7cc16c7404eac6e64fd8f91c37f_Out_2_Float, _Lerp_600f0b288c2142edbde191762355ce71_Out_3_Float);
            float _Branch_2e19450bb08e47dbad9131d31663a1b2_Out_3_Float;
            Unity_Branch_float(_Property_49f28783e83742099b1dc228a2a37b62_Out_0_Boolean, _Lerp_600f0b288c2142edbde191762355ce71_Out_3_Float, _Multiply_3a2533948d5c42c28bdc60f46f42a310_Out_2_Float, _Branch_2e19450bb08e47dbad9131d31663a1b2_Out_3_Float);
            float _Saturate_62cdd7af891548d39f696ca3b8a0ce57_Out_1_Float;
            Unity_Saturate_float(_Branch_2e19450bb08e47dbad9131d31663a1b2_Out_3_Float, _Saturate_62cdd7af891548d39f696ca3b8a0ce57_Out_1_Float);
            float _Multiply_45f0bf168079400ebe9c0ab520aff046_Out_2_Float;
            Unity_Multiply_float_float(_Split_af3a6d77e9044b9e89f9114690e89ff1_A_4_Float, _Saturate_62cdd7af891548d39f696ca3b8a0ce57_Out_1_Float, _Multiply_45f0bf168079400ebe9c0ab520aff046_Out_2_Float);
            surface.BaseColor = _Vector3_3f5c06b7f49e421fb71322e3a501bc47_Out_0_Vector3;
            surface.Alpha = _Multiply_45f0bf168079400ebe9c0ab520aff046_Out_2_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.VertexColor = input.color;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphUnlitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}
// ############################################################################
//   FILE: Assets/Shaders/NatureWind.shader
//
//   Richiede URP 14+ (Unity 2022.2 / 2022.3 LTS).
//   Testato su Forward e Forward+.
// ############################################################################

Shader "Nature/Wind Vegetation"
{
    Properties
    {
        [MainTexture] _BaseMap   ("Texture", 2D) = "white" {}
        [MainColor]   _BaseColor ("Colore", Color) = (1,1,1,1)

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 1
        _Cutoff ("Soglia Alpha", Range(0,1)) = 0.4

        [Header(Vento)]
        _WindStrength   ("Forza (moltiplicatore locale)", Range(0,4)) = 1
        _WindStiffness  ("Rigidita del fusto", Range(0.5,6)) = 2
        _PlantHeight    ("Altezza pianta (metri)", Float) = 1
        _FlutterAmount  ("Tremolio foglie", Range(0,1)) = 0.35
        _FlutterSpeed   ("Velocita tremolio", Range(0,20)) = 7
        _PhaseVariation ("Sfasamento tra piante", Range(0,3)) = 1

        [Header(Mascheratura)]
        [Toggle(_USE_VERTEX_COLOR_MASK)] _UseVCMask
            ("Usa vertex color R come maschera", Float) = 0
        [Toggle(_USE_VERTEX_COLOR_THICKNESS)] _UseVCThickness
            ("Usa vertex color G come spessore foglia", Float) = 0

        [Header(Traslucenza)]
        _Translucency      ("Intensita", Range(0,3)) = 0.7
        _TransDistortion   ("Distorsione normale", Range(0,1)) = 0.4
        _TransPower        ("Concentrazione", Range(1,16)) = 4
        _TransShadowFloor  ("Passa attraverso l ombra", Range(0,1)) = 0.35

        [Header(Aspetto)]
        _ColorVariation ("Variazione colore tra piante", Range(0,1)) = 0.25
        [HDR] _TintA ("Tinta A", Color) = (0.85, 1.0, 0.75, 1)
        [HDR] _TintB ("Tinta B", Color) = (1.0, 0.95, 0.70, 1)
        _AOFromMask ("Occlusione alla base", Range(0,1)) = 0.35

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Toggle] _AlphaToMask ("Alpha To Coverage (con MSAA)", Float) = 1
        [HideInInspector] _QueueOffset ("Queue Offset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "TransparentCutout"
            "Queue"             = "AlphaTest"
            "RenderPipeline"    = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector"   = "True"
            "ShaderModel"       = "4.5"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float  _PlantHeight;
            float  _Cull;
            float  _AlphaToMask;
            float  _QueueOffset;
            half4  _BaseColor;
            half4  _TintA;
            half4  _TintB;
            half   _Cutoff;
            half   _WindStrength;
            half   _WindStiffness;
            half   _FlutterAmount;
            half   _FlutterSpeed;
            half   _PhaseVariation;
            half   _Translucency;
            half   _TransDistortion;
            half   _TransPower;
            half   _TransShadowFloor;
            half   _ColorVariation;
            half   _AOFromMask;
        CBUFFER_END

        float4 _GlobalWindDirection;
        float4 _GlobalWindParams; 
        float4 _GlobalGustParams; 

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        float3 GetInstanceOriginWS()
        {
            return float3(UNITY_MATRIX_M._m03,
                          UNITY_MATRIX_M._m13,
                          UNITY_MATRIX_M._m23);
        }

        float PlantHash(float3 worldOrigin, float salt)
        {
            int3 q = (int3)floor(worldOrigin * 100.0 + 0.5);
            uint h = (uint)q.x * 73856093u ^
                     (uint)q.y * 19349663u ^
                     (uint)q.z * 83492791u ^
                     (uint)salt * 2654435761u;
            h ^= h >> 13u; h *= 0x5bd1e995u; h ^= h >> 15u;
            return (float)(h & 0x00FFFFFFu) / 16777215.0;
        }

        float WindMask(float3 positionOS, half4 vertexColor)
        {
        #if defined(_USE_VERTEX_COLOR_MASK)
            return saturate(vertexColor.r);
        #else
            return saturate(positionOS.y / max(0.01, _PlantHeight));
        #endif
        }

        float3 ApplyWind(float3 positionOS, float3 positionWS, float mask)
        {
            float strength = (_GlobalWindParams.x + _GlobalGustParams.x) * _WindStrength;

            if (strength <= 0.0001 || mask <= 0.0001) return positionWS;

            float t          = _GlobalWindParams.y;
            float waveScale  = _GlobalWindParams.z;
            float coherence  = _GlobalWindParams.w;

            float3 dir = _GlobalWindDirection.xyz;
            dir = (dot(dir, dir) < 1e-6) ? float3(1, 0, 0) : normalize(dir);

            float3 origin = GetInstanceOriginWS();
            float  hash   = PlantHash(origin, 1.0);

            float travel = dot(origin, dir) * waveScale;
            float wave  = sin(t * 1.6 - travel * 6.2831);
            wave += sin(t * 2.7 - travel * 11.0) * 0.35;
            wave *= 0.74;

            float phase = hash * 6.2831 * _PhaseVariation;
            float own   = sin(t * (1.1 + hash * 0.6) + phase);

            float bend = lerp(own, wave, coherence);
            float flex = pow(saturate(mask), _WindStiffness);

            float3 offset = dir * (bend * strength * flex * 0.35);

            #if !defined(SHADERPASS_SHADOWS_LOWCOST)
            if (_FlutterAmount > 0.001)
            {
                float f = sin(t * _FlutterSpeed + phase * 2.3 + positionOS.x * 9.0)
                        * cos(t * _FlutterSpeed * 0.7 + positionOS.z * 7.0);

                float3 side = cross(dir, float3(0, 1, 0));
                side = (dot(side, side) < 1e-6) ? float3(0, 0, 1) : normalize(side);

                offset += (side * f + float3(0, f * 0.4, 0))
                        * (_FlutterAmount * strength * flex * 0.06);
            }
            #endif

            float horiz2 = dot(offset.xz, offset.xz);
            offset.y -= horiz2 * 0.5 / max(0.01, _PlantHeight);

            return positionWS + offset;
        }

        float3 WindedPositionWS(float3 positionOS, half4 color, out float maskOut)
        {
            float3 posWS = TransformObjectToWorld(positionOS);
            maskOut = WindMask(positionOS, color);
            return ApplyWind(positionOS, posWS, maskOut);
        }

        half AlphaClipTest(float2 uv)
        {
            half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
        #if defined(_ALPHATEST_ON)
            clip(a - _Cutoff);
        #endif
            return a;
        }
        ENDHLSL

        // ====================================================================
        //  PASS PRINCIPALE
        // ====================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _USE_VERTEX_COLOR_MASK
            #pragma shader_feature_local _USE_VERTEX_COLOR_THICKNESS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half4  tintAO     : TEXCOORD3;
                half   thickness  : TEXCOORD4;
                float  fogFactor  : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float mask;
                float3 posWS = WindedPositionWS(IN.positionOS.xyz, IN.color, mask);

                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);

                float3 origin = GetInstanceOriginWS();
                float  h = PlantHash(origin, 7.0);

                half3 tint = lerp(half3(1, 1, 1),
                                  lerp(_TintA.rgb, _TintB.rgb, h),
                                  _ColorVariation);

                half ao = lerp(1.0h - _AOFromMask, 1.0h, saturate(mask));
                OUT.tintAO = half4(tint, ao);

            #if defined(_USE_VERTEX_COLOR_THICKNESS)
                OUT.thickness = IN.color.g;
            #else
                OUT.thickness = (half)saturate(mask);
            #endif

                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 albedo = tex * _BaseColor;
                albedo.rgb *= IN.tintAO.rgb;

            #if defined(_ALPHATEST_ON)
                clip(albedo.a - _Cutoff);
            #endif

                float3 N = normalize(IN.normalWS) * (IS_FRONT_VFACE(face, 1.0, -1.0));
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half occlusion = IN.tintAO.a;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                float2 nsUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(nsUV);
                occlusion *= aoFactor.indirectAmbientOcclusion;
                mainLight.color *= aoFactor.directAmbientOcclusion;
            #endif

                half3 lighting = mainLight.color
                               * saturate(dot(N, mainLight.direction))
                               * mainLight.shadowAttenuation;

                half3 Hn = normalize(mainLight.direction + N * _TransDistortion);
                half back = pow(saturate(dot(V, -Hn)), _TransPower);
                half shadowSoft = lerp(_TransShadowFloor, 1.0h, mainLight.shadowAttenuation);
                lighting += mainLight.color * back * _Translucency
                          * IN.thickness * shadowSoft;

                half3 ambient = SampleSH(N) * occlusion;

            #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowCoord = shadowCoord;

                half4 shadowMask = half4(1, 1, 1, 1);
                uint meshRenderingLayers = GetMeshRenderingLayer();

                LIGHT_LOOP_BEGIN(GetAdditionalLightsCount())
                    Light l = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(l.layerMask, meshRenderingLayers))
                #endif
                    {
                        half att = l.distanceAttenuation * l.shadowAttenuation;
                        lighting += l.color * saturate(dot(N, l.direction)) * att;

                        half3 Hl = normalize(l.direction + N * _TransDistortion);
                        lighting += l.color * pow(saturate(dot(V, -Hl)), _TransPower)
                                  * _Translucency * IN.thickness * att * 0.5h;
                    }
                LIGHT_LOOP_END
            #endif

                half3 col = albedo.rgb * (lighting + ambient);
                col = MixFog(col, IN.fogFactor);

                return half4(col, albedo.a);
            }
            ENDHLSL
        }

        // ====================================================================
        //  OMBRE
        // ====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _USE_VERTEX_COLOR_MASK
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct AttributesS
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsS
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsS vertShadow(AttributesS IN)
            {
                VaryingsS OUT = (VaryingsS)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float mask;
                float3 posWS = WindedPositionWS(IN.positionOS.xyz, IN.color, mask);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);

            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDir = normalize(_LightPosition - posWS);
            #else
                float3 lightDir = _LightDirection;
            #endif

                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, lightDir));

            #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
            #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                OUT.positionCS = cs;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 fragShadow(VaryingsS IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                AlphaClipTest(IN.uv);
                return 0;
            }
            ENDHLSL
        }

        // ====================================================================
        //  PROFONDITA'
        // ====================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest LEqual
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   vertDepth
            #pragma fragment fragDepth
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _USE_VERTEX_COLOR_MASK
            #pragma multi_compile_instancing

            struct AttributesD
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsD
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsD vertDepth(AttributesD IN)
            {
                VaryingsD OUT = (VaryingsD)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float mask;
                float3 posWS = WindedPositionWS(IN.positionOS.xyz, IN.color, mask);

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 fragDepth(VaryingsD IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                AlphaClipTest(IN.uv);
                return 0;
            }
            ENDHLSL
        }

        // ====================================================================
        //  DEPTH NORMALS
        // ====================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   vertDN
            #pragma fragment fragDN
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_vertex _USE_VERTEX_COLOR_MASK
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct AttributesDN
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDN
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsDN vertDN(AttributesDN IN)
            {
                VaryingsDN OUT = (VaryingsDN)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float mask;
                float3 posWS = WindedPositionWS(IN.positionOS.xyz, IN.color, mask);

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 fragDN(VaryingsDN IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                AlphaClipTest(IN.uv);

                float3 N = normalize(IN.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);
                return half4(NormalizeNormalPerPixel(N), 0.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
Shader "StylizedGrass/Detail"
{
    Properties
    {
        _MainTex ("Texture (optional)", 2D) = "white" {}
        _Cull ("Cull (0 = double sided, 2 = back)", Float) = 0
        _Size ("Size", Float) = 0.4
        _SizeRange ("Size Range", Vector) = (0.7, 1.3, 0, 0)
        _MeshHeight ("Mesh Height", Float) = 1
        _RootDarkening ("Root Darkening", Range(0, 1)) = 0.2
        _LightWrap ("Light Wrap", Range(0, 1)) = 0.4
        _FlattenResponse ("Flatten Response", Range(0, 1)) = 1
        _FlattenMin ("Length Kept When Flattened", Range(0, 1)) = 0.6
        _WindStrength ("Wind Strength", Float) = 0.15
        _WindSpeed ("Wind Speed", Float) = 1.2
        _WindScale ("Wind Scale", Float) = 0.35
        _WindDirection ("Wind Direction", Vector) = (1, 0.4, 0, 0)
        _PaletteCount ("Palette Count", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_local _ _GRASS_RECEIVE_SHADOWS
            #pragma multi_compile_local _ _GRASS_EXTRA_LIGHTS

            #include "DetailCommon.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 uvh        : TEXCOORD2; // xy = uv, z = height01
                float3 tint       : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
            };

            Varyings vert(Attributes v)
            {
                DetailVertex d = EvaluateDetail(v.positionOS, v.normalOS, v.uv, v.color, v.instanceID);
                Varyings o;
                o.positionWS = d.positionWS;
                o.normalWS = d.normalWS;
                o.positionCS = TransformWorldToHClip(d.positionWS);
                o.uvh = float3(d.uv, d.height01);
                o.tint = d.tint;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 normalWS = normalize(i.normalWS);
                half3 albedo = i.tint * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvh.xy).rgb;
                albedo *= lerp(1.0 - _RootDarkening, 1.0, saturate(i.uvh.z * 1.5));

                #if defined(_GRASS_RECEIVE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                half ndl = dot(normalWS, mainLight.direction);
                half wrap = saturate((ndl + _LightWrap) / (1.0 + _LightWrap));
                half3 lighting = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation * wrap);
                lighting += SampleSH(normalWS);

                #if defined(_ADDITIONAL_LIGHTS) && defined(_GRASS_EXTRA_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    #if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
                        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    #endif

                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
                    for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light dl = GetAdditionalLight(dirIndex, i.positionWS);
                        lighting += dl.color * dl.distanceAttenuation * saturate(dot(normalWS, dl.direction) * 0.5 + 0.5);
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, i.positionWS);
                        lighting += light.color * light.distanceAttenuation * saturate(dot(normalWS, light.direction) * 0.5 + 0.5);
                    LIGHT_LOOP_END
                #endif

                half3 color = albedo * lighting;
                color = MixFog(color, i.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "DetailCommon.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                DetailVertex d = EvaluateDetail(v.positionOS, v.normalOS, v.uv, v.color, v.instanceID);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - d.positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(d.positionWS, d.normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                Varyings o;
                o.positionCS = positionCS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "DetailCommon.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                DetailVertex d = EvaluateDetail(v.positionOS, v.normalOS, v.uv, v.color, v.instanceID);
                Varyings o;
                o.positionCS = TransformWorldToHClip(d.positionWS);
                return o;
            }

            half frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

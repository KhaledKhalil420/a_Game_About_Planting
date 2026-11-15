Shader "StylizedGrass/Grass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.05, 0.6, 0.22, 1)
        _TipColor ("Tip Color", Color) = (0.45, 1, 0.4, 1)
        _VariationColor ("Variation Color", Color) = (0.75, 0.95, 0.25, 1)
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.35
        _RootDarkening ("Root Darkening", Range(0, 1)) = 0.35
        _BladeHeight ("Blade Height", Float) = 0.28
        _TuftSize ("Tuft Size", Float) = 0.5
        _FlattenMin ("Length Kept When Flattened", Range(0, 1)) = 0.5
        _ScaleRange ("Scale Range", Vector) = (0.7, 1.3, 0, 0)
        _WindStrength ("Wind Strength", Float) = 0.25
        _WindSpeed ("Wind Speed", Float) = 1.2
        _WindScale ("Wind Scale", Float) = 0.35
        _WindDirection ("Wind Direction", Vector) = (1, 0.4, 0, 0)
        _LightWrap ("Light Wrap", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        // ------------------------------------------------------------ Forward lit
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
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

            #include "GrassCommon.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 uvVar      : TEXCOORD2; // xy = uv, z = variation
                half   fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                GrassVertex g = EvaluateGrass(v.positionOS, v.uv, v.instanceID);
                Varyings o;
                o.positionWS = g.positionWS;
                o.normalWS = g.normalWS;
                o.positionCS = TransformWorldToHClip(g.positionWS);
                o.uvVar = float3(g.uv, g.variation);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 normalWS = normalize(i.normalWS);
                float h = saturate(i.uvVar.y);

                half3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, h);
                albedo = lerp(albedo, _VariationColor.rgb, i.uvVar.z * _ColorVariation);
                albedo *= lerp(1.0 - _RootDarkening, 1.0, saturate(h * 2.0));

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

        // ------------------------------------------------------------ Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "GrassCommon.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                GrassVertex g = EvaluateGrass(v.positionOS, v.uv, v.instanceID);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - g.positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(g.positionWS, g.normalWS, lightDirectionWS));
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

        // ------------------------------------------------------------ Depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "GrassCommon.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                uint   instanceID : SV_InstanceID;
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                GrassVertex g = EvaluateGrass(v.positionOS, v.uv, v.instanceID);
                Varyings o;
                o.positionCS = TransformWorldToHClip(g.positionWS);
                return o;
            }

            half frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

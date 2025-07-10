// URP-Compatible Tree Wind Shader with Transparency, Metallic Map, and Shadows
// Based on original work by Renan Bomtempo

Shader "URP/TreeWind"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _MetallicGlossMap ("Metallic Map", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _AlphaClipThreshold ("Alpha Cutoff", Range(0,1)) = 0.5
        _OffsetY ("Vertical Offset", Float) = 0

        _wind_dir ("Wind Direction", Vector) = (0.5,0.05,0.5,0)
        _wind_size ("Wind Wave Size", Range(5,50)) = 15

        _tree_sway_stutter_influence("Tree Sway Stutter Influence", Range(0,1)) = 0.2
        _tree_sway_stutter ("Tree Sway Stutter", Range(0,10)) = 1.5
        _tree_sway_speed ("Tree Sway Speed", Range(0,10)) = 1
        _tree_sway_disp ("Tree Sway Displacement", Range(0,1)) = 0.3

        _branches_disp ("Branches Displacement", Range(0,0.5)) = 0.3

        _leaves_wiggle_disp ("Leaves Wiggle Displacement", Float) = 0.07
        _leaves_wiggle_speed ("Leaves Wiggle Speed", Float) = 0.01

        _r_influence ("Red Vertex Influence", Range(0,1)) = 1
        _b_influence ("Blue Vertex Influence", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 300
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        AlphaToMask On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MetallicGlossMap;
            float4 _Tint;
            float _Metallic;
            float _Smoothness;
            float _AlphaClipThreshold;
            float _OffsetY;

            float4 _wind_dir;
            float _wind_size;
            float _tree_sway_speed;
            float _tree_sway_disp;
            float _leaves_wiggle_disp;
            float _leaves_wiggle_speed;
            float _branches_disp;
            float _tree_sway_stutter;
            float _tree_sway_stutter_influence;
            float _r_influence;
            float _b_influence;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float time = _Time.y;

                float swayX = (cos(time * _tree_sway_speed + (worldPos.x / _wind_size) + sin(time * _tree_sway_stutter * _tree_sway_speed + (worldPos.x / _wind_size)) * _tree_sway_stutter_influence) + 1) * 0.5;
                float swayZ = (cos(time * _tree_sway_speed + (worldPos.z / _wind_size) + sin(time * _tree_sway_stutter * _tree_sway_speed + (worldPos.z / _wind_size)) * _tree_sway_stutter_influence) + 1) * 0.5;

                float3 offset;
                offset.x = swayX * _tree_sway_disp * _wind_dir.x * (IN.positionOS.y / 10.0) + cos(_Time.z * IN.positionOS.x * _leaves_wiggle_speed + (worldPos.x/_wind_size)) * _leaves_wiggle_disp * _wind_dir.x * IN.color.b * _b_influence;
                offset.z = swayZ * _tree_sway_disp * _wind_dir.z * (IN.positionOS.y / 10.0) + cos(_Time.z * IN.positionOS.z * _leaves_wiggle_speed + (worldPos.z/_wind_size)) * _leaves_wiggle_disp * _wind_dir.z * IN.color.b * _b_influence;
                offset.y = cos(time * _tree_sway_speed + (worldPos.z / _wind_size)) * _tree_sway_disp * _wind_dir.y * (IN.positionOS.y / 10.0);
                offset.y += sin(_Time.z * _tree_sway_speed + _wind_dir.x + (worldPos.z/_wind_size)) * _branches_disp * IN.color.r * _r_influence;

                float3 finalPos = IN.positionOS.xyz + offset;
                finalPos.y += _OffsetY;

                OUT.positionCS = TransformObjectToHClip(finalPos);
                OUT.positionWS = TransformObjectToWorld(finalPos);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = tex2D(_MainTex, IN.uv) * _Tint;
                clip(tex.a - _AlphaClipThreshold);

                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = tex.rgb;
                surface.alpha = tex.a;
                surface.normalTS = float3(0, 0, 1);
                surface.metallic = tex2D(_MetallicGlossMap, IN.uv).r * _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion = 1.0;
                surface.emission = float3(0, 0, 0);

                InputData input = (InputData)0;
                input.positionWS = IN.positionWS;
                input.normalWS = normal;
                input.viewDirectionWS = viewDir;
                input.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);

                return UniversalFragmentPBR(input, surface);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

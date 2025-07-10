Shader "URP/Custom/LitLocalGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (0,0,0,1)
        _GradientHeight ("Gradient Height", Float) = 1.0
        [HideInInspector] _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : NORMAL;
                float3 positionWS  : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;
            };

            float4 _TopColor;
            float4 _BottomColor;
            float _GradientHeight;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Local space Y gradient
                float t = saturate(IN.positionOS.y / _GradientHeight);
                float4 color = lerp(_BottomColor, _TopColor, t);

                // Lighting
                float3 normalWS = normalize(IN.normalWS);
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float NdotL = max(0, dot(normalWS, lightDir));
                float3 litColor = color.rgb * (_MainLightColor.rgb * NdotL);

                return float4(litColor, color.a);
            }
            ENDHLSL
        }
    }
}

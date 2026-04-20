Shader "Custom/LineRendererTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Emission ("Emission", Color) = (1, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _FORWARD_PLUS

            #include "UnityCG.cginc"

            float4 _Color;
            float4 _Emission;
            float _EmissionIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 finalColor = i.color * _Color;
                float4 emission = _Emission * _EmissionIntensity;
                finalColor.rgb += emission.rgb * finalColor.a;
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/VertexLit"
}

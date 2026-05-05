Shader "UI/TorchEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlayerPos ("Player Position", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.15
        _DarknessColor ("Darkness Color", Color) = (0, 0, 0, 0.95)
        _AspectRatio ("Aspect Ratio", Float) = 1.777
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _PlayerPos;
            float _Radius;
            float4 _DarknessColor;
            float _AspectRatio;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 playerPos = float2(_PlayerPos.x, _PlayerPos.y);

                // Correct for aspect ratio to make circle truly round
                float2 diff = i.uv - playerPos;
                diff.x *= _AspectRatio;
                float dist = length(diff);

                float edge = 0.015;
                float alpha = smoothstep(_Radius - edge, _Radius + edge, dist);

                return float4(_DarknessColor.rgb, _DarknessColor.a * alpha);
            }
            ENDCG
        }
    }
}
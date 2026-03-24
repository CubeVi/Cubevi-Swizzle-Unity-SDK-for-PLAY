Shader "Custom/GridTransparent"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.7, 0.7, 0.7, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)
        _GridSpacing ("Grid Spacing", Float) = 1
        _LineWidth ("Line Width", Range(0.001, 0.2)) = 0.02
        _NearFadeStart ("Near Fade Start", Float) = 0.5
        _NearFadeEnd ("Near Fade End", Float) = 2
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 localPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float4 _GridColor;
            float4 _BackgroundColor;
            float _GridSpacing;
            float _LineWidth;
            float _NearFadeStart;
            float _NearFadeEnd;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float spacing = max(_GridSpacing, 0.0001);
                float2 coord = i.localPos.xy / spacing;
                float2 grid = abs(frac(coord) - 0.5);
                float minorLine = 1.0 - step(_LineWidth * 0.5, min(grid.x, grid.y));

                float lineMask = minorLine;
                float dist = distance(_WorldSpaceCameraPos, i.worldPos);
                float fadeRange = max(_NearFadeEnd - _NearFadeStart, 0.0001);
                float fade = saturate((dist - _NearFadeStart) / fadeRange);
                float alpha = _GridColor.a * lineMask * fade;

                fixed4 col = _BackgroundColor;
                col.rgb = lerp(_BackgroundColor.rgb, _GridColor.rgb, lineMask);
                col.a = max(_BackgroundColor.a, alpha);
                return col;
            }
            ENDCG
        }
    }
}

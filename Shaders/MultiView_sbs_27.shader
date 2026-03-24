Shader "CustomRenderTexture/MultiView_sbs_27"
{  
    Properties  
    {  
        _MainTex ("Texture", 2D) = "white" {}  
        _TextureArray ("Texture Array", 2DArray) = "" {}
        _Gamma ("Gamma Correction", Float) = 1.8  

        _OutputSizeX ("Output Size X", Float) = 3840.0
        _OutputSizeY ("Output Size Y", Float) = 2160.0 
        _RowNum ("Row Num", Float) = 8.0
        _ColNum ("Col Num", Float) = 8.0
        _ShowBoundary ("Show View Boundary", Float) = 0

        [Toggle(USE_PIXEL_ORDER_MODE)] _UsePixelOrderMode ("pixel order", Float) = 0        
        
        _Slope ("Slope", Float) = -0.1004 
        _Interval ("Interval", Float) = 19.6122 
        _X0 ("X0 Offset", Float) = 15.4  
        _X0Tex ("X0 Texture", 2D) = "black" {}
    }  
    SubShader  
    {  
        Tags { "RenderType"="Opaque" }  
        LOD 100  
  
        Pass  
        {   
            Name "CustomRenderTexture/MultiView"

            CGPROGRAM  
            #pragma vertex vert  
            #pragma fragment frag  
            #pragma shader_feature USE_PIXEL_ORDER_MODE
  
            #include "UnityCG.cginc"  
            #include "UnityCustomRenderTexture.cginc"
  
            struct appdata_t
            {  
                float4 vertex : POSITION;  
                float2 uv : TEXCOORD0;  
            };  
  
            struct v2f  
            {  
                float2 uv : TEXCOORD0;  
                float4 vertex : SV_POSITION;  
            };  
  
            UNITY_DECLARE_TEX2DARRAY(_TextureArray);
            UNITY_DECLARE_TEX2D(_X0Tex);
            float4 _TextureArray_ST; 

            float _OutputSizeX;
            float _OutputSizeY;
            float _Slope;
            float _X0;
            float _Interval;
            float _ShowBoundary;

            float _RowNum;
            float _ColNum;
            // float _KX1;
            // float _KX3;
            // float _KY2;
            // float _KY4;
            // float4 _TanX;
            // float4 _TanY;
            float _X0Array[96];
            
            float sampleX0(float2 pos)
            {
                float u = (pos.x * _RowNum + 0.5) / (_RowNum + 1.0);
                float v = (pos.y * _ColNum + 0.5) / (_ColNum + 1.0);
                return UNITY_SAMPLE_TEX2D(_X0Tex, float2(u, v)).r;
            }

            float bilinearInterpolation(float2 pos)
            {
                float rows = _RowNum;
                float cols = _ColNum;
                float x = pos.x * rows;
                float y = pos.y * cols;
                int ix = (int)floor(x);
                int iy = (int)floor(y);
                ix = max(0, min((int)rows, ix));
                iy = max(0, min((int)cols, iy));
                float wx = x - floor(x);
                float wy = y - floor(y);
                int stride = (int)(rows + 1.0);
                int i00 = iy * stride + ix;
                int i10 = iy * stride + (ix + 1);
                int i01 = (iy + 1) * stride + ix;
                int i11 = (iy + 1) * stride + (ix + 1);
                float value00 = _X0Array[i00];
                float value10 = _X0Array[i10];
                float value01 = _X0Array[i01];
                float value11 = _X0Array[i11];
                float value0 = lerp(value00, value10, wx);
                float value1 = lerp(value01, value11, wx);
                return lerp(value0, value1, wy);
            }

            // float2 bilinearEyetrack(float2 pos)
            // {
            //     float wx = pos.x;
            //     float wy = pos.y;
            //     float value00x = _TanX.x;
            //     float value10x = _TanX.y;
            //     float value01x = _TanX.z;
            //     float value11x = _TanX.w;
            //     float value00y = _TanY.x;
            //     float value10y = _TanY.y;
            //     float value01y = _TanY.z;
            //     float value11y = _TanY.w;
            //     float value0x = lerp(value00x, value10x, wx);
            //     float value1x = lerp(value01x, value11x, wx);
            //     float value0y = lerp(value00y, value10y, wx);
            //     float value1y = lerp(value01y, value11y, wx);
            //     return float2(lerp(value0x, value1x, wy), lerp(value0y, value1y, wy));
            // }

            float get_choice_float(float2 pos, float bias)
            {
                float2 p = float2(pos.x + bias / 3.0 /_OutputSizeX, 1.0 - pos.y);
                float interpolated_x0 = bilinearInterpolation(p);
                // float2 eyetrack_xy = bilinearEyetrack(p);
                // float y2 = eyetrack_xy.y * eyetrack_xy.y;

                float x = (pos.x) * _OutputSizeX + 0.5;
                float y = (1.0 - pos.y) * _OutputSizeY + 0.5;
                float x1 = (x + y * _Slope) * 3.0 + bias;
                
                //float x0_eyetrack =  _KX1 * eyetrack_xy.x + _KX3 * eyetrack_xy.x * eyetrack_xy.x * eyetrack_xy.x - eyetrack_xy.x * (_KY2 * y2 + _KY4 * y2 * y2);
                //float x_local = fmod(x1 + _X0 + interpolated_x0 + x0_eyetrack + 1000.0*_Interval, _Interval);

                float x_local = fmod(x1 + _X0 + interpolated_x0 + 1000.0*_Interval, _Interval);
                return (x_local / _Interval);
            }

            float4 get_color_sbs(float2 i, float bias) {
                float choice_float = get_choice_float(i, bias);

                float sel_pos = 0.0;
                if(choice_float < 0.5){
                    sel_pos = 1.0;
                }

                if(_ShowBoundary > 0.5) {
                    float delta = 0.025;
                    if(choice_float > 0.5 - delta && choice_float < 0.5 + delta){
                        return float4(1.0,0.0,0.0,1.0);
                    }
                }
                
                return UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(i, sel_pos));
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _TextureArray);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {

                // float4 col = float4(0.5,0.2,1.0,1.0);

                #ifdef USE_PIXEL_ORDER_MODE
                    float4 color = get_color_sbs(i.uv, 0.0);
                    color.g = get_color_sbs(i.uv, 1.0).g; //g 
                    color.b = get_color_sbs(i.uv, 2.0).b; //b
                #else   
                    float4 color = get_color_sbs(i.uv, 2.0);
                    color.g = get_color_sbs(i.uv, 1.0).g; //g 
                    color.b = get_color_sbs(i.uv, 0.0).b; //b
                #endif

                // float4 color =  tex2D(_MainTex, i.uv);
                // return col;
                // color = float4(0.2,0.7,1.0,1.0);
                // color =  tex2D(_MainTex, i.uv);

                //color = float4(0,_X0Array[0],0,1);
                return color;
            }
            ENDCG  
        }  
    }  
}
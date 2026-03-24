Shader "CustomRenderTexture/MultiView_mul"
{  
    Properties  
    {  
        _MainTex ("Texture", 2D) = "white" {}  
        _TextureArray ("Texture Array", 2DArray) = "" {}
        _Slope ("Slope", Float) = -0.1004 
        _Interval ("Interval", Float) = 19.6122 
        _X0 ("X0 Offset", Float) = 15.4  
        _MulOffset("MultiView Offser",Float) = 4.7

        _delta_vertical_choice_factor("_delta_vertical_choice_factor", Float) = 0.0
        _horizon_choice("horizon_choice", Float) = 0.0
        _ViewCount ("ViewCount", Float) = 40.0
        _ImgCount ("ImgCount", Float) = 60.0
        _RowNum ("Row Num", Float) = 10.0
        _ColNum ("Col Num", Float) = 6.0

        _Gamma ("Gamma Correction", Float) = 1.8  
        _OutputSizeX ("Output Size X", Float) = 1440.0  
        _OutputSizeY ("Output Size Y", Float) = 2560.0 

        [Toggle(USE_PIXEL_ORDER_MODE)] _UsePixelOrderMode ("pixel order", Float) = 0
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
            float4 _TextureArray_ST; 

            float _OutputSizeX;
            float _OutputSizeY;
            float _Slope;
            float _X0;
            float _MulOffset;
            float _Interval;
            float _delta_vertical_choice_factor;
            float _horizon_choice;
            float _ViewCount;
            float _ImgCount;
            float _RowNum;
            float _ColNum;
            float _X0Array[96];
            
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

            float get_choice_float(float2 pos, float bias)
            {
                float2 p = float2(pos.x + bias / 3.0 /_OutputSizeX, 1.0 - pos.y);
                float interpolated_x0 = bilinearInterpolation(p);
                float x = (pos.x) * _OutputSizeX + 0.5;
                float y = (1.0 - pos.y) * _OutputSizeY + 0.5;
                float x1 = (x + y * _Slope) * 3.0 + bias;
                float x_local = fmod(x1 + _X0 + interpolated_x0 + 1000.0*_Interval, _Interval);
                return (x_local / _Interval);
            }

            // float get_uv_from_choice(float2 pos, float choice_float)
            // {
            //     float interval_index = _ViewCount - fmod(choice_float * _ViewCount, _ViewCount);
            //     float choice = 0;
            //     float delta_choice = 0;
            //     float actual_N = _horizon_choice - (pos.x - 0.5)*_delta_vertical_choice_factor;

            //     float periodic_num = floor(actual_N / _ViewCount);
            //     float pos_quilt = actual_N % _ViewCount;

            //     if(interval_index - pos_quilt > _ViewCount / 2)
            //     {
            //        delta_choice =  - _ViewCount;
            //     }
            //     else if (pos_quilt - interval_index > _ViewCount / 2)
            //     {
                   
            //        delta_choice =   _ViewCount;
            //     }
                
            //     choice = floor(interval_index + periodic_num * _ViewCount + delta_choice + _MulOffset);
            //     //choice = fmod(floor(interval_index), _ImgsCountAll);
            //     // choice = 2;
            //     return choice;
            // }

            float4 get_color(float2 i, float bias) {
                float choice_float = get_choice_float(i, bias);

                //float choiceN = _horizon_choice - (i.x - 0.5) * _delta_vertical_choice_factor + (choice_float - 0.5) * _ViewCount;
                float choiceN = _horizon_choice - 0.0 * _delta_vertical_choice_factor + (choice_float - 0.5) * _ViewCount;
                float sel_pos = clamp(choiceN, 0.0, _ImgCount - 1.0);

                //float sel_pos = get_uv_from_choice(i, choice_float);

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
                    float4 color = get_color(i.uv, 0.0);
                    color.g = get_color(i.uv, 1.0).g; //g 
                    color.b = get_color(i.uv, 2.0).b; //b
                #else   
                    float4 color = get_color(i.uv, 2.0);
                    color.g = get_color(i.uv, 1.0).g; //g 
                    color.b = get_color(i.uv, 0.0).b; //b
                #endif

                // float4 color =  tex2D(_MainTex, i.uv);
                // return col;
                // color = float4(0.2,0.7,1.0,1.0);
                // color =  tex2D(_MainTex, i.uv);
                return color;
            }
            ENDCG  
        }  
    }  
}

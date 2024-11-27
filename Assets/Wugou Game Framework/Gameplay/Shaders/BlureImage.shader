Shader "Wugou/BlureImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0 // 模糊尺寸(纹理坐标的偏移量)
    }
    SubShader
    {

        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            //float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
            half2 uv[5]: TEXCOORD0; // 5个邻域的纹理坐标
        };

        half4 _MainTex_TexelSize; // _MainTex的像素尺寸大小, float4(1/width, 1/height, width, height)
        float _BlurSize; // 模糊尺寸(纹理坐标的偏移量)

        v2f vertVertical (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            //o.uv = v.uv;
            half2 uv = v.uv;
            o.uv[0] = uv;
            o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
            o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;      
            return o;
        }

        v2f vertHorizontal (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            //o.uv = v.uv;
            half2 uv = v.uv;
            o.uv[0] = uv;
            o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
            o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;      
            return o;
        }

        sampler2D _MainTex;

        fixed4 frag (v2f i) : SV_Target
        {
            float weight[3] = {0.4026, 0.2442, 0.0545}; // 大小为5的一维高斯核，实际只需记录3个权值
            fixed3 sum = tex2D(_MainTex, i.uv[0]).rgb * weight[0];
            for (int j = 1; j < 3; j++) {
                sum += tex2D(_MainTex, i.uv[j * 2 - 1]).rgb * weight[j]; // 中心右侧或下侧的纹理*权值
                sum += tex2D(_MainTex, i.uv[j * 2]).rgb * weight[j]; // 中心左侧或上侧的纹理*权值
            }
            return fixed4(sum, 1.0);
        }
        ENDCG

        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass {
            NAME "GAUSSIAN_BLUR_VERTICAL"
            
            CGPROGRAM
              
            #pragma vertex vertVertical  
            #pragma fragment frag
              
            ENDCG  
        }
        
        Pass {  
            NAME "GAUSSIAN_BLUR_HORIZONTAL"
            
            CGPROGRAM  
            
            #pragma vertex vertHorizontal  
            #pragma fragment frag
            
            ENDCG
        }
    }
}

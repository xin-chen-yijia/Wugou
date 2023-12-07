// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Wugou/AxisTransparent"
{
    Properties
    {
        [MainColor]_MainColor ("Main Color", Color) = (1.0,1.0,1.0,1.0)
    }
    SubShader
    {
        // No culling or depth
        Cull Off 
        ZWrite Off
        ZTest Off
        Blend SrcAlpha OneMinusSrcAlpha
        Tags { "Queue" = "Transparent+2000" }

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

float4x4 _OrthoProjection;

            v2f vert (appdata v)
            {
                v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

        fixed4 _MainColor;


            fixed4 frag (v2f i) : SV_Target
            {
                //fixed4 col = tex2D(_MainTex, i.uv);
                // just invert the colors
                //col.rgb = 1 - col.rgb;
    return _MainColor;

}
            ENDCG
        }
    }
}

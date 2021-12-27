Shader "Unlit/FluidRender"
{
    Properties
    {
        _Texture("Texture", 2D) = "white" {}
        _Color("Background Color", color) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                   float3 normal : NORMAL;
            };

            struct v2f
            {                
                fixed4 diff : COLOR0; // diffuse lighting color
                float4 vertex : SV_POSITION;
                 float2 uv : TEXCOORD1;
                float3 normal: TEXCOORD3;
            };
            sampler2D _Texture;
            float4 _Texture_ST;

            float4 _Color;

            v2f vert (MeshData v)
            {
                v2f o;
                o.uv = v.uv;                               
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.normal = v.normal;
                return o;
            }
            

            fixed4 frag (v2f i) : SV_Target
            {                                
                float4 fluidColor = tex2D(_Texture, i.uv);

                float4 fluidAlpha = fluidColor.a;

                return _Color * (1-fluidAlpha) + fluidColor;
            }
            ENDCG
        }
    }
}

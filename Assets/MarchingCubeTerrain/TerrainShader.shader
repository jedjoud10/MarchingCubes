// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/TerrainShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MainTex2("Texture 2", 2D) = "white" {}
        _MainTex3("Texture 3", 2D) = "white" {}
        _MainTex4("Texture 4", 2D) = "white" {}
        _MainTex5("Texture 5", 2D) = "white" {}
        _MainTex6("Texture 6", 2D) = "white" {}
        _MainTex7("Texture 7", 2D) = "white" {}
        _MainTex8("Texture 8", 2D) = "white" {}
        _MainTex9("Texture 9", 2D) = "white" {}
        _Tiling("Tiling", Float) = 1.0
    }
    SubShader
    {
        Pass
        {
            Tags {"LightMode" = "ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"


            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            #include "AutoLight.cginc"

            struct v2f
            {
                half3 objNormal : TEXCOORD0;
                half3 worldNormal : TEXCOORD4;
                float3 coords : TEXCOORD1;
                SHADOW_COORDS(3) // put shadows data into TEXCOORD3
                float2 uv : TEXCOORD2;
                float4 pos :  SV_POSITION;
                float3 worldPos : TEXCOORD5;
                fixed4 color : COLOR0;
                fixed3 ambient : COLOR2;
            };

            float _Tiling;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.coords = v.vertex.xyz * _Tiling;
                o.objNormal = v.normal;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.texcoord;

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.ambient = ShadeSH9(half4(worldNormal, 1));
                // compute shadows data
                TRANSFER_SHADOW(o);

                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _MainTex2;
            sampler2D _MainTex3;
            sampler2D _MainTex4;
            sampler2D _MainTex5;
            sampler2D _MainTex6;
            sampler2D _MainTex7;
            sampler2D _MainTex8;
            sampler2D _MainTex9;

            fixed4 TST(sampler2D tex, v2f i)
            {
                // use absolute value of normal as texture weights
                half3 blend = pow(abs(i.objNormal), 2);
                // make sure the weights sum up to 1 (divide by sum of x+y+z)
                blend /= dot(blend, 1.0);

                // read the three texture projections, for x,y,z axes
                fixed4 cx = tex2D(tex, i.coords.yz);
                fixed4 cy = tex2D(tex, i.coords.xz);
                fixed4 cz = tex2D(tex, i.coords.xy);
                // blend the textures based on weights
                fixed4 c = cx * blend.x + cy * blend.y + cz * blend.z;

                return c;
            }
            fixed4 lerp2(fixed4 a, fixed4 b, fixed4 c, float t) 
            {
                return lerp(a, lerp(b, c, (t - 0.5) * 2), clamp(t * 2, 0, 1));
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 normal = normalize(cross(ddy(i.worldPos), ddx(i.worldPos)));
                half nl = max(0, dot(normal, _WorldSpaceLightPos0.xyz));
                float diff = nl * _LightColor0.rgb;
                fixed4 col = fixed4(1, 1, 1, 1);
                //col = lerp2(lerp2(TST(_MainTex, i), TST(_MainTex2, i), TST(_MainTex3, i), i.color.r), lerp2(TST(_MainTex4, i), TST(_MainTex5, i), TST(_MainTex6, i), i.color.g), lerp2(TST(_MainTex7, i), TST(_MainTex8, i), TST(_MainTex9, i), i.color.b), i.color.a);
                // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                fixed shadow = SHADOW_ATTENUATION(i);
                // darken light's illumination with shadow, keep ambient intact
                fixed3 lighting = diff * shadow + i.ambient;
                col.rgb *= lighting;
                col *= i.color;
                return col;
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
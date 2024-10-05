Shader "SmartRooms/Tilemap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _ReplaceColor ("Color which will be replaced by Foreground", Color) = (1,1,1,1)
        _ReplaceMaxDistance ("Maximum distance from replace color to be replaced", Float) = 1000
        _DebugPerlin ("Debug Perlin", Float) = 0
        _FGTexturePerlin ("FG Texture Perlin", 2D) = "white" {}
        _FGTexture1 ("FG Texture 1", 2D) = "white" {}
        _FGTexture2 ("FG Texture 2", 2D) = "white" {}
        _FGTexture3 ("FG Texture 3", 2D) = "white" {}
        _FGTexture4 ("FG Texture 4", 2D) = "white" {}
        _FGTexture5 ("FG Texture 5", 2D) = "white" {}
        _OverlayColor ("Color which will be overlayed on top", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #ifndef UNITY_SPRITES_INCLUDED
            #define UNITY_SPRITES_INCLUDED

            #include "UnityCG.cginc"

            #ifdef UNITY_INSTANCING_ENABLED

            UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
                // SpriteRenderer.Color while Non-Batched/Instanced.
                UNITY_DEFINE_INSTANCED_PROP(fixed4, unity_SpriteRendererColorArray)
                // this could be smaller but that's how bit each entry is regardless of type
                UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
            UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

            #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
            #define _Flip           UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)

            #endif // instancing

            CBUFFER_START(UnityPerDrawSprite)
            #ifndef UNITY_INSTANCING_ENABLED
            fixed4 _RendererColor;
            fixed2 _Flip;
            #endif
            float _EnableExternalAlpha;
            CBUFFER_END

            // Material Color.
            fixed4 _Color;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 position_in_world_space : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float4 UnityFlipSprite(in float3 pos, in fixed2 flip)
            {
                return float4(pos.xy * flip, pos.z, 1.0);
            }

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                OUT.position_in_world_space = mul(unity_ObjectToWorld, IN.vertex);

                #ifdef PIXELSNAP_ON
            OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _AlphaTex;

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
            fixed4 alpha = tex2D (_AlphaTex, uv);
            color.a = lerp (color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            fixed4 SampleForegroundColor()
            {
                return 0;
            }

            float4 _OverlayColor;
            float4 _ReplaceColor;
            float _ReplaceMaxDistance;
            sampler2D _FGTexture1;
            float4 _FGTexture1_ST;
            float4 _FGTexture1_TexelSize;
            sampler2D _FGTexture2;
            sampler2D _FGTexture3;
            sampler2D _FGTexture4;
            sampler2D _FGTexture5;
            sampler2D _FGTexturePerlin;
            float4 _FGTexturePerlin_ST;
            float4 _FGTexturePerlin_TexelSize;
            float _DebugPerlin;

            int xorshift(in int value)
            {
                // Xorshift*32
                // Based on George Marsaglia's work: http://www.jstatsoft.org/v08/i14/paper
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value;
            }

            float nextFloat(int seed)
            {
                seed = xorshift(seed);
                seed = xorshift(seed);
                // FIXME: This should have been a seed mapped from MIN..MAX to 0..1 instead
                return abs(frac(float(seed) / 3001.592653));
            }


            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                c.rgb *= c.a;
                fixed distance = length(c - _ReplaceColor);
                if (distance < _ReplaceMaxDistance)
                {
                    float2 coords = (IN.position_in_world_space / _FGTexture1_ST.xy + _FGTexture1_ST.zw);
                    float2 perlinCoords = ((coords - floor(_FGTexturePerlin_ST.zw)) * _FGTexturePerlin_TexelSize.xy / floor(_FGTexturePerlin_ST.xy));
                    float test = tex2D(_FGTexturePerlin, perlinCoords).x;
                    float outputTest = 0;
                    float4 output = tex2D(_FGTexture5, coords % 1) * _OverlayColor;
                    if (test < 0.2)
                    {
                        outputTest = 0.2;
                        output = tex2D(_FGTexture1, coords % 1) * _OverlayColor;
                    }
                    else if (test < 0.4)
                    {
                        outputTest = 0.4;
                        output = tex2D(_FGTexture2, coords % 1) * _OverlayColor;
                    }
                    else if (test < 0.6)
                    {
                        outputTest = 0.6;
                        output = tex2D(_FGTexture3, coords % 1) * _OverlayColor;
                    }
                    else if (test < 0.8)
                    {
                        outputTest = 0.8;
                        output = tex2D(_FGTexture4, coords % 1) * _OverlayColor;
                    }

                    if (_DebugPerlin > 0)
                        output = output * float4(outputTest, 0, 0, 1);

                    return output;
                }

                return c;
            }

            #endif // UNITY_SPRITES_INCLUDED
            ENDCG
        }
    }
}
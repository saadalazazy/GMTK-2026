Shader "Ditzelgames/WaveShaderURP"
{
    Properties
    {
        _Color ("Color", Color) = (0.2, 0.5, 0.8, 1)
        _MainTex ("Texture", 2D) = "white" {}
        // Assigned at runtime by Waves.cs - do not set manually
        [HideInInspector] _PermTable ("Permutation Table", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_PermTable); SAMPLER(sampler_PermTable);

            // Must match Waves.cs MaxOctaves
            #define MAX_OCTAVES 8

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _OctaveScaleHeight[MAX_OCTAVES]; // xy=scale, z=height, w=alternate(0/1)
                float4 _OctaveSpeed[MAX_OCTAVES];       // xy=speed
                float _Dimension;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            // ---- shared noise math (mirrors PerlinNoise.cs) ----

            float perm(float x)
            {
                return SAMPLE_TEXTURE2D_LOD(_PermTable, sampler_PermTable, float2(frac(x / 256.0), 0), 0).r * 255.0;
            }

            float gradN(float hash, float x, float y)
            {
                float h = fmod(hash, 4.0);
                float u = h < 2.0 ? x : y;
                float v = h < 2.0 ? y : x;
                float su = (fmod(h, 2.0) == 0.0) ? u : -u;
                float sv = (h >= 2.0) ? -v : v;
                return su + sv;
            }

            float fadeN(float t) { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }

            float perlin2D(float x, float y)
            {
                float X = floor(x);
                float Y = floor(y);
                float fx = x - X;
                float fy = y - Y;
                float u = fadeN(fx);
                float v = fadeN(fy);

                float A  = perm(X) + Y;
                float AA = perm(A);
                float AB = perm(A + 1.0);
                float B  = perm(X + 1.0) + Y;
                float BA = perm(B);
                float BB = perm(B + 1.0);

                float res = lerp(
                    lerp(gradN(perm(AA), fx, fy),       gradN(perm(BA), fx - 1.0, fy),       u),
                    lerp(gradN(perm(AB), fx, fy - 1.0), gradN(perm(BB), fx - 1.0, fy - 1.0), u),
                    v);

                return (res + 1.0) * 0.5;
            }

            // Same octave summation as Waves.GetHeight() on the CPU.
            float waveHeight(float x, float z)
            {
                float y = 0;
                [unroll]
                for (int o = 0; o < MAX_OCTAVES; o++)
                {
                    float2 scale  = _OctaveScaleHeight[o].xy;
                    float height  = _OctaveScaleHeight[o].z;
                    float alt     = _OctaveScaleHeight[o].w;
                    float2 speed  = _OctaveSpeed[o].xy;

                    if (alt > 0.5)
                    {
                        float p = perlin2D((x * scale.x) / _Dimension, (z * scale.y) / _Dimension) * 6.2831853;
                        y += cos(p + length(speed) * _Time.y) * height;
                    }
                    else
                    {
                        float p = perlin2D((x * scale.x + _Time.y * speed.x) / _Dimension, (z * scale.y + _Time.y * speed.y) / _Dimension) - 0.5;
                        y += p * height;
                    }
                }
                return y;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float x = IN.positionOS.x;
                float z = IN.positionOS.z;
                float y = waveHeight(x, z);

                // Central-difference normal so lighting reacts to the waves
                // without ever touching Mesh.normals on the CPU.
                const float eps = 0.25;
                float hL = waveHeight(x - eps, z);
                float hR = waveHeight(x + eps, z);
                float hD = waveHeight(x, z - eps);
                float hU = waveHeight(x, z + eps);
                float3 normalOS = normalize(float3(hL - hR, 2.0 * eps, hD - hU));

                float3 positionOS = float3(x, y, z);
                OUT.positionWS  = TransformObjectToWorld(positionOS);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS    = TransformObjectToWorldNormal(normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 ambient = SampleSH(normalWS);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 albedo = tex.rgb * _Color.rgb;

                half3 color = albedo * (mainLight.color * NdotL + ambient);
                return half4(color, _Color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

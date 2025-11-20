Shader "NavalCommand/ToonWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _FoamColor ("Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveHeight ("Wave Height", Float) = 0.2
        _WaveFrequency ("Wave Frequency", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off // Render both sides just in case

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            float4 _Color;
            float4 _FoamColor;
            float _WaveSpeed;
            float _WaveHeight;
            float _WaveFrequency;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Simple Vertex Wave Animation
                float wave = sin(_Time.y * _WaveSpeed + v.vertex.x * _WaveFrequency) * 
                             cos(_Time.y * _WaveSpeed * 0.5 + v.vertex.z * _WaveFrequency);
                
                v.vertex.y += wave * _WaveHeight;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Height-based color (lighter at peaks)
                // Calculate relative height from wave (approximate)
                float waveHeight = i.worldPos.y - (-2.0); // Assuming base level is -2
                float heightFactor = smoothstep(-_WaveHeight, _WaveHeight, waveHeight);
                
                // 2. Fresnel / Rim effect (Foam edges)
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float NdotV = dot(normalize(i.normal), viewDir);
                float fresnel = 1.0 - saturate(NdotV);
                fresnel = pow(fresnel, 3.0);

                // 3. Specular / Sun reflection (Fake)
                float3 lightDir = normalize(float3(1, 1, -1)); // Fake sun direction
                float3 reflectDir = reflect(-lightDir, i.normal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
                
                // Combine
                fixed4 baseColor = lerp(_Color * 0.8, _Color * 1.2, heightFactor); // Darker troughs, lighter peaks
                fixed4 finalColor = lerp(baseColor, _FoamColor, fresnel * 0.5);
                finalColor += spec * 0.5; // Add specular highlight

                return finalColor;
            }
            ENDCG
        }
    }
}

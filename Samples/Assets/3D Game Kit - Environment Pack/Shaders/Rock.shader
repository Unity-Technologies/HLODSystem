// Implements correct triplanar normals in a Surface Shader with out computing or passing additional information from the
// vertex shader. Instead works around some oddities with how Surface Shaders handle the tangent space vectors. Attempting
// to directly access the tangent matrix data results in a shader generation error. This works around the issue by tricking
// the surface shader into not using those vectors until actually in the generated shader code. - Ben Golus 2017

Shader "Custom/Rock" {
    Properties {

        _MainTex ("Albedo", 2D) = "white" {}
        [NoScaleOffset]_BumpMap("Normal", 2D) = "bump" {}
        [NoScaleOffset]_DetailBump("Detail Normal", 2D) = "bump" {}
        _DetailScale("Detail Scale", Float) = 1.0
        [NoScaleOffset]_OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [NoScaleOffset]_MetallicRough ("Metallic/Roughness (RGBA)", 2D) = "white" {}
        [Gamma] _Metallic("Metallic", Range(0, 1)) = 0
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
        [NoScaleOffset]_TopAlbedo ("Top Albedo", 2D) = "white" {}
        [NoScaleOffset]_TopNormal("Top Normal", 2D) = "bump" {}
        [NoScaleOffset]_TopMetallicRough ("Metallic/Roughness (RGBA)", 2D) = "white" {}
        [Gamma] _TopMetallic("Metallic", Range(0, 1)) = 0
        _TopGlossiness("Smoothness", Range(0, 1)) = 0.5
        [NoScaleOffset]_Noise ("Noise", 2D) = "white" {}

    }
    SubShader {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        
        CGPROGRAM
        #pragma multi_compile _ LOD_FADE_CROSSFADE

        #pragma surface surf Standard fullforwardshadows
        #pragma target 5.0
        #include "UnityStandardUtils.cginc"
        #include "UnityCG.cginc" 
        #include "AutoLight.cginc"
       
        // #define TRIPLANAR_UV_OFFSET
        // hack to work around the way Unity passes the tangent to world matrix to surface shaders to prevent compiler errors
        #if defined(INTERNAL_DATA) && (defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD) || defined(UNITY_PASS_DEFERRED) || defined(UNITY_PASS_META))
            #define WorldToTangentNormalVector(data,normal) mul(normal, half3x3(data.internalSurfaceTtoW0, data.internalSurfaceTtoW1, data.internalSurfaceTtoW2))
        #else
            #define WorldToTangentNormalVector(data,normal) normal
        #endif

        // Reoriented Normal Mapping
        // http://blog.selfshadow.com/publications/blending-in-detail/
        // Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
        half3 blend_rnm(half3 n1, half3 n2)
        {
            n1.z += 1;
            n2.xy = -n2.xy;

            return n1 * dot(n1, n2) / n1.z - n2;
        }


        sampler2D _MainTex, _TopAlbedo, _BumpMap, _TopNormal, _Noise, _OcclusionMap, _MetallicRough, _TopMetallicRough, _DetailBump;
        float4 _Top_ST;
        float4 _UVOffset;
        half _Glossiness, _FresnelAmount, _FresnelPower, _TopScale, _NoiseAmount, _NoiseFallOff, _Metallic, _TopMetallic, _TopGlossiness, _OcclusionStrength, _noiseScale;
        half _DetailScale;

        struct Input {
            float4 screenPos;
            float3 worldPos;
            float3 viewDir;
            float3 worldNormal;
            float2 uv_MainTex;
            INTERNAL_DATA
        };
     

        void surf (Input IN, inout SurfaceOutputStandard o) {

            IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));

            // top down UVs
            float2 uvY = IN.worldPos.xz * _TopScale;

            fixed4 noisetex = tex2D(_Noise, uvY * _noiseScale);
            half blend =  clamp(0 , 1, IN.worldNormal.y);
            blend = smoothstep(noisetex.r, 1, blend);
            half noiseBlend = smoothstep(0.1, 0.2, blend);

            // tangent space normal map
            half3 tnormalY = UnpackNormal(tex2D(_TopNormal, uvY));
            half3 normalMain = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            half3 detailNormal = UnpackNormal(tex2D(_DetailBump, IN.uv_MainTex * _DetailScale));
            normalMain = blend_rnm(normalMain, detailNormal);

            // flip normal maps' x axis to account for flipped UVs
            half3 absVertNormal = abs(IN.worldNormal);
            // swizzle world normals to match tangent space and apply reoriented normal mapping blend
            tnormalY = blend_rnm(half3(IN.worldNormal.xz, absVertNormal.y), tnormalY);
            // sizzle tangent normals to match world normal and blend together
            half3 worldNormal = normalize(tnormalY.xzy);

            // convert world space normals into tangent normals
            float3 tangentNormal = WorldToTangentNormalVector(IN, worldNormal);

            //Albedo
            //float fresnel = (dot(tangentNormal, IN.viewDir));
            //fresnel = clamp(pow(1-fresnel, _FresnelPower), 0, 6);
            fixed4 colY = tex2D(_TopAlbedo, uvY);
            fixed4 colMain = tex2D(_MainTex, IN.uv_MainTex);

            //Occlusion
            half occ =  lerp(1, tex2D(_OcclusionMap, IN.uv_MainTex), _OcclusionStrength);

            //Metallic/Smoothness
            half4 metallicSmoothness = tex2D(_MetallicRough, IN.uv_MainTex);
            half4 TopMetallicSmoothness = tex2D(_TopMetallicRough, uvY);
            half m = lerp(metallicSmoothness.r * _Metallic, noisetex.r * _TopMetallic, noiseBlend);
            half s = lerp(metallicSmoothness.a * _Glossiness, TopMetallicSmoothness.a * _TopGlossiness, noiseBlend);

            #ifdef LOD_FADE_CROSSFADE
            float2 vpos = IN.screenPos.xy / IN.screenPos.w * _ScreenParams.xy;
            UnityApplyDitherCrossFade(vpos);
            #endif

            // set surface ouput properties
            o.Albedo = lerp(colMain, colY, noiseBlend);
            o.Occlusion = occ;
            o.Metallic = m;
            o.Smoothness = s;
            o.Normal = lerp(normalMain, tangentNormal, noiseBlend);
            
        }
        ENDCG

    }
    FallBack "Diffuse"
}

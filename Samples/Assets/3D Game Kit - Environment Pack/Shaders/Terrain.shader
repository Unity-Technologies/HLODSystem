// Implements correct triplanar normals in a Surface Shader with out computing or passing additional information from the
// vertex shader. Instead works around some oddities with how Surface Shaders handle the tangent space vectors. Attempting
// to directly access the tangent matrix data results in a shader generation error. This works around the issue by tricking
// the surface shader into not using those vectors until actually in the generated shader code. - Ben Golus 2017

Shader "Custom/Terrain" {
    Properties {
        
        
        _WaterColor ("Water Color", Color) = (1,1,1,1)
        _WaterEdge("Water Edge Hardness", Range(0, 1)) = 0.2
        [NoScaleOffset]_WaterRoughness ("Water Roughness", 2D) = "white" {}
        _ParallaxStrength("Parallax Strength", Range(0, 0.1)) = 0.05


        //01
        [NoScaleOffset]_Albedo01 ("Albedo 01", 2D) = "white" {}
        [NoScaleOffset]_Normal01("Normal 01", 2D) = "bump" {}
        [NoScaleOffset]_MRHAO01 ("Metal/Rough/Height/AO 01", 2D) = "white" {}
        _TextureScale01("Texture Scale 01", Float) = 1.0
        _Falloff01("Blend Falloff 01", Range(0, 1)) = 0.2
        //02
        [NoScaleOffset]_Albedo02 ("Albedo 02", 2D) = "white" {}
        [NoScaleOffset]_Normal02("Normal 02", 2D) = "bump" {}
        [NoScaleOffset]_Normal02Detail("Normal 02 Detail", 2D) = "bump" {}
        [NoScaleOffset]_MRHAO02 ("Metal/Rough/Height/AO 02", 2D) = "white" {}
        _TextureScale02("Texture Scale 01", Float) = 1.0
        _Falloff02("Blend Falloff 01", Range(0, 1)) = 0.2
        //03
        [NoScaleOffset]_Albedo03 ("Albedo 03", 2D) = "white" {}
        [NoScaleOffset]_Normal03("Normal 03", 2D) = "bump" {}
        [NoScaleOffset]_MRHAO03 ("Metal/Rough/Height/AO 03", 2D) = "white" {}
        _TextureScale03("Texture Scale 01", Float) = 1.0
        _Falloff03("Blend Falloff 01", Range(0, 1)) = 0.2
        //04
        //[NoScaleOffset]_Albedo04 ("Albedo 04", 2D) = "white" {}
        //[NoScaleOffset]_Normal04("Normal 04", 2D) = "bump" {}
        //[NoScaleOffset]_MRHAO04 ("Metal/Rough/Height/AO 04", 2D) = "white" {}
        //_TextureScale04("Texture Scale 01", Float) = 1.0
        //_Falloff04("Blend Falloff 01", Range(0, 1)) = 0.2
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 5.0
        #include "UnityStandardUtils.cginc"
        #include "UnityCG.cginc" 
        #include "AutoLight.cginc"
        #include "Tessellation.cginc"

       
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


        sampler2D _Albedo01, _Normal01, _MRHAO01;
        sampler2D _Albedo02, _Normal02, _MRHAO02;
        sampler2D _Albedo03, _Normal03, _MRHAO03;

        sampler2D _Normal02Detail;
        sampler2D _WaterRoughness;

        half _WaterEdge, _ParallaxStrength;
        half _Falloff01, _Falloff02, _Falloff03, _Falloff04;
        half _TextureScale01, _TextureScale02, _TextureScale03;
        half4 _WaterColor;


        struct vertdata {
		    float4 vertex : POSITION;
		    float4 tangent : TANGENT;
		    float3 normal : NORMAL;
		    float4 texcoord : TEXCOORD0;
		    float4 texcoord1 : TEXCOORD1;
		    float4 texcoord2 : TEXCOORD2;
		    fixed4 color : COLOR;
		    float3 tangentViewDir : TEXCOORD3;
		    UNITY_VERTEX_INPUT_INSTANCE_ID
		};

        struct Input {
            float3 worldPos;
            float3 viewDir;
            float3 worldNormal;
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 tangentViewDir : TEXCOORD3;
            INTERNAL_DATA
        };


        void vert (inout vertdata v, out Input o) {
        	UNITY_INITIALIZE_OUTPUT(Input,o);

        	float3 worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;


        	float3x3 objectToTangent = float3x3(v.tangent.xyz,cross(v.normal, v.tangent.xyz) * v.tangent.w,v.normal);
			v.tangentViewDir = mul(objectToTangent, ObjSpaceViewDir(v.vertex));

			o.tangentViewDir = normalize(v.tangentViewDir);
			o.tangentViewDir.xy /= o.tangentViewDir.z;

    	  }
     

        void surf (Input IN, inout SurfaceOutputStandard o) {

            IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));


            // top down UVs
            float2 UVY = IN.worldPos.xz;   

            fixed4 vertCol = IN.color;

            //Albedo

            fixed4 Albedo01 = tex2D(_Albedo01, UVY * _TextureScale01);
            fixed4 Albedo02 = tex2D(_Albedo02, UVY * _TextureScale02);
            fixed4 Albedo03 = tex2D(_Albedo03, UVY * _TextureScale03);
            //fixed4 Albedo04 = tex2D(_Albedo04, UVY * _TextureScale04);

           

            half blend01 = smoothstep(vertCol.r, vertCol.r-_Falloff01, 1-Albedo01.a);
            half blend02 = smoothstep(vertCol.g, vertCol.g-_Falloff02, 1-Albedo03.a);

            float2 UVY2 = IN.worldPos.xz;  
            UVY2 += IN.tangentViewDir * _ParallaxStrength * blend01;


            Albedo02 = tex2D(_Albedo02, UVY2 * _TextureScale02);
            Albedo03 = tex2D(_Albedo03, UVY * _TextureScale03);


            
            fixed4 AlbedoFinal = Albedo01;
            AlbedoFinal = lerp(AlbedoFinal, Albedo02, blend01);
            AlbedoFinal = lerp(AlbedoFinal, Albedo01, blend02);
            //AlbedoFinal = lerp(AlbedoFinal, Albedo04, blend03);

            // tangent space normal map
            half3 Normal01 = UnpackNormal(tex2D(_Normal01, UVY * _TextureScale01));
            half3 Normal02 = UnpackNormal(tex2D(_Normal02, UVY2 * _TextureScale02));
            half3 Normal03 = UnpackNormal(tex2D(_Normal03, UVY * _TextureScale03));
            //half3 Normal04 = UnpackNormal(tex2D(_Normal04, UVY * _TextureScale04));

            half3 Normal02Detail = UnpackNormal(tex2D(_Normal02Detail, UVY*0.1));

            // flip normal maps' x axis to account for flipped UVs
            half3 absVertNormal = abs(IN.worldNormal);
            // swizzle world normals to match tangent space and apply reoriented normal mapping blend
            half3 tangentNormal = lerp(Normal01,Normal02, blend01);
            tangentNormal = lerp(tangentNormal, Normal03, blend02);
            //tangentNormal = lerp(tangentNormal, Normal04, blend03);
            tangentNormal = blend_rnm(half3(IN.worldNormal.xz, absVertNormal.y), tangentNormal);
            // sizzle tangent normals to match world normal and blend together
            half3 worldNormal = normalize(tangentNormal.xzy);
            // convert world space normals into tangent normals
            float3 NormalFinal = WorldToTangentNormalVector(IN, worldNormal);


            //Roughness
            half Roughness01 = tex2D(_MRHAO01, UVY * _TextureScale01).a;
            half Roughness02 = tex2D(_MRHAO02, UVY2 * _TextureScale02).a;
            half Roughness03 = tex2D(_MRHAO03, UVY * _TextureScale03).a;
            //half Roughness04 = tex2D(_MRHAO04, UVY * _TextureScale04).a;
            fixed4 RoughnessFinal = lerp(0.3, Roughness02, blend01);
            RoughnessFinal = lerp(RoughnessFinal, Roughness03, blend02);
            //RoughnessFinal = lerp(RoughnessFinal, Roughness04, blend03);
           
            /*
            //waterLevel
            half blendY = 1-smoothstep(0.96, 1, absVertNormal.y);
            half waterLevel = smoothstep(IN.color.r, IN.color.r - 0.3, clamp(0, 1, colY.a + blendY));
            half waterLevel2 = smoothstep(0, 0.1, waterLevel);
            //half waterLevel = blendY;
            */

            
            //water
 			float blend = 1-Albedo01.a;

            half3 upNormal = WorldToTangentNormalVector(IN, float3(0,1,0));
            AlbedoFinal = lerp(AlbedoFinal*_WaterColor, AlbedoFinal, smoothstep(IN.color.a+_WaterEdge, IN.color.a+1,  blend));
            NormalFinal = lerp(upNormal, NormalFinal, smoothstep(IN.color.a+_WaterEdge, IN.color.a, blend));
            RoughnessFinal = lerp( tex2D(_WaterRoughness, UVY * 0.3).a * 0.95, RoughnessFinal, smoothstep(IN.color.a+_WaterEdge, IN.color.a, blend));
            

            // set surface ouput properties
            o.Albedo = AlbedoFinal;
            //o.Occlusion = occ;
            o.Metallic = 0;
            o.Smoothness = RoughnessFinal;
            o.Normal = NormalFinal; 
            
        }
        ENDCG

    }
    FallBack "Diffuse"
}

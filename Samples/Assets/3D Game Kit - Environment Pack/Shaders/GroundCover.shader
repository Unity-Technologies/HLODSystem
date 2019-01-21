// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/GroundCover" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo", 2D) = "white" {}
		[NoScaleOffset]_MetallicSmooth ("_MetallicSmooth", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		[NoScaleOffset]_Normal("Normal", 2D) = "bump" {}
		_NormalScale ("Normal Scale", Float) = 1.0
		[NoScaleOffset]_Occlusion ("Occlusion", 2D) = "white" {}
		_OcclusionScale ("Occlusion Scale", Range(0, 1)) = 1.0
		[NoScaleOffset]_WindNoise ("_WindNoise", 2D) = "white" {}
		_NoiseAmount ("Noise Amount", Vector) = (0,0,0,0)
		_NoiseScale ("Noise Texture Scale", Float) = 1.0
		
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert addshadow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex, _WindNoise, _Normal, _MetallicSmooth, _Occlusion;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float4 vertCol : COLOR;
			INTERNAL_DATA
		};

		half _Glossiness;
		half _NormalScale, _OcclusionScale;
		half _Metallic, _NoiseScale;
		fixed4 _Color;
		half4 _NoiseAmount;

		void vert (inout appdata_full v, out Input o) {
        	UNITY_INITIALIZE_OUTPUT(Input,o);

        	float3 worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
        	float3 offset = float3(0, 0.1, 0.2);

        	float2 UV = worldPos.xz;
        	UV.xy += _Time*2;
        	float3 windNoise = tex2Dlod(_WindNoise, float4(UV, 0, 0) * _NoiseScale).rgb;


        	v.vertex.z += sin(_Time*20) * v.color.a * _NoiseAmount.z * v.color.r * windNoise.g;
        	v.vertex.z += sin(_Time*15) * v.color.a * _NoiseAmount.z * v.color.g * windNoise.g;
        	v.vertex.z += sin(_Time*25) * v.color.a * _NoiseAmount.z * v.color.b * windNoise.g;

        	v.vertex.x += sin(_Time*20) * v.color.a * _NoiseAmount.x * v.color.r * windNoise.r;
        	v.vertex.x += sin(_Time*15) * v.color.a * _NoiseAmount.x * v.color.g * windNoise.r;
        	v.vertex.x += sin(_Time*25) * v.color.a * _NoiseAmount.x * v.color.b * windNoise.r;

        	v.vertex.y += windNoise.r * _NoiseAmount.y * v.color.a * v.color.r;
        	v.vertex.y += windNoise.g * _NoiseAmount.y * v.color.a * v.color.g;
        	v.vertex.y += windNoise.b * _NoiseAmount.y * v.color.a * v.color.b;
    	  }

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 noiseTex = tex2D (_WindNoise, IN.worldPos.xz * 0.1);
			c.rgb *= lerp(_Color, float4(1,1,1,1), noiseTex);
			o.Albedo = c.rgb;
			o.Normal = UnpackScaleNormal(tex2D(_Normal, IN.uv_MainTex), _NormalScale);
			o.Occlusion = lerp(1, tex2D (_Occlusion, IN.uv_MainTex), _OcclusionScale);
			
			fixed4 metallicSmooth = tex2D (_MetallicSmooth, IN.uv_MainTex);
			o.Metallic = _Metallic * metallicSmooth.r;
			o.Smoothness = _Glossiness * metallicSmooth.a;
			o.Alpha = c.a;

			clip(c.a - 0.5);
		}
		ENDCG
	}
	FallBack "Diffuse"
}

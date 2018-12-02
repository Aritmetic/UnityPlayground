Shader "Custom/GR3DCarPaint" {
	Properties {

		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
		_ColOut ("Outer Color", Color) = (1,1,1,1)
		_GlossOut ("Outer Gloss", Range(0,1)) = 0.97
		_ColIn ("Inner Color", Color) = (1,1,1,1)
		_GlossIn ("Inner Gloss", Range (0,1)) = 0.95
		_Highlight ("Highlight", Range(0,0.5)) = 0.1

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
		};

		half _Highlight;
		half _GlossOut;
		half _GlossIn;
		fixed4 _Color;
		float4 _ColOut;
		float4 _ColIn;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			half brdr = saturate(dot (normalize(IN.viewDir), o.Normal));
			float fsp = 0.2f * pow (brdr, 4) * _Highlight;
			fixed4 ssp = tex2D (_MainTex, IN.uv_MainTex) * _Color * lerp (_ColOut, _ColIn, pow (brdr, 4)) + fsp;
			o.Albedo = ssp.rgb;
			o.Smoothness = lerp (_GlossOut, _GlossIn, pow (brdr, 4));
		}
		ENDCG
	}
	FallBack "Diffuse"
}

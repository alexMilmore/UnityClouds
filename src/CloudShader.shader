// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/CloudShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_BumpMap("Bumpmap", 2D) = "bump" {}
		_ColorTint("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_ShadowTint("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_BaseLight("Base Luminosity", Range(0, 1)) = 0.5
		_CelShading("Cel Shade", Float) = 1
		_Cutoff("Effect Amount", Range(0, 1)) = 0.01
		_CelShadingAlpha("Cel Shade Alpha", Float) = 1
		_CutoffAlpha("Effect Amount", Range(0, 1)) = 0.01
		_Outline("Include outline", Float) = 0
		_OutlineThickness("Outline Thickness", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
			uniform float _BaseLight;
			uniform float _CelShading;
			uniform float _Cutoff;
			uniform float _CelShadingAlpha;
			uniform float _CutoffAlpha;
			fixed4 _ColorTint;
			fixed4 _ShadowTint;
			uniform float _Outline;
			uniform float _OutlineThickness;

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				float4 worldSpacePos : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
			sampler2D _BumpMap;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldSpacePos = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				// sample normal map
				half3 tNormal = UnpackNormal(tex2D(_BumpMap, i.uv));
				// calculate light
        float3 distanceFromLight = float3(_WorldSpaceLightPos0.x - i.worldSpacePos.x, _WorldSpaceLightPos0.y - i.worldSpacePos.y, 0);
				float attenuation = 1 / length(distanceFromLight);
				half lightIntensity = max(0, dot(tNormal, distanceFromLight)) * attenuation;
				if (_CelShading == 1) {
					lightIntensity = smoothstep(0 + _Cutoff, 0.01 + _Cutoff, lightIntensity);
				}

				// Allow cel shading for Alpha
				if (_CelShadingAlpha == 1) {
					col.a = smoothstep(0 + _CutoffAlpha, 0.01 + _CutoffAlpha, col.a);
				}


				// Allow outline
				if (_Outline == 1) {
					col.rgb = col.rgb * _ColorTint * lightIntensity + col.rgb * _ShadowTint * (1 - lightIntensity);
					//col.rgb = col.rgb;

					col.rgb = col.rgb * smoothstep(_OutlineThickness, 1, col.a);
				}
				else {
					col.rgb = col.rgb * _ColorTint * lightIntensity + col.rgb * _ShadowTint * (1 - lightIntensity);
					//col.rgb = col.rgb;
				}


                return col;
            }
            ENDCG
        }
    }
}

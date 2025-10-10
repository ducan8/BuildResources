Shader "Custom/TwoLayerOgreShaderNoWhite"
{
	Properties
	{
		_Layer0Tex("Layer 0 Texture", 2D) = "white" {}
		_Layer1Tex("Layer 1 Texture", 2D) = "white" {}
		_DiffuseColor("Diffuse Color", Color) = (1,1,1,0)
		_SpecularColor("Specular Color", Color) = (1,1,1,0)
	}
		SubShader
		{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			Blend One OneMinusSrcAlpha
			ZWrite On
			LOD 200
			Cull Back
			Pass
			{
				
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				//#pragma multi_compile_fog    // ✅ Thêm dòng này để shader hỗ trợ nhiều kiểu fog

				sampler2D _Layer0Tex;
				sampler2D _Layer1Tex;
				float4 _DiffuseColor;
				float4 _SpecularColor;


				struct VertexInput
				{
					float4 vertex : POSITION;
					float2 uv0 : TEXCOORD0;
					float2 uv1 : TEXCOORD1;
					//UNITY_FOG_COORDS(2);
				};

				struct VertexOutput
				{
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float2 uv1 : TEXCOORD1;
					//UNITY_FOG_COORDS(2)      // ✅ Gửi tọa độ fog từ vertex -> fragment
				};

				VertexOutput vert(VertexInput v)
				{
					VertexOutput o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv0 = v.uv0;
					o.uv1 = v.uv1;
					//UNITY_TRANSFER_FOG(o, o.pos);
					return o;
				}

				float4 frag(VertexOutput i) : SV_Target
				{
					// Lấy texture từ 2 layer
					float4 c0 = tex2D(_Layer0Tex, i.uv0); // Layer 0
					float4 c1 = tex2D(_Layer1Tex, i.uv1); // Layer 1

					// Nếu alpha không hợp lệ, mặc định là 1
					c0.a = saturate(c0.a);
					c1.a = saturate(c1.a);

					// Làm mượt vùng giao nhau
					float blendFactor = smoothstep(0.05, 0.95, c1.a);

					// Thực hiện hòa trộn màu sắc với gamma correction
					float3 c0Linear = pow(c0.rgb, 2.2); // Chuyển sang không gian linear
					float3 c1Linear = pow(c1.rgb, 2.2); // Chuyển sang không gian linear
					float3 blendedRGBLinear = lerp(c0Linear, c1Linear, blendFactor);
					float3 blendedRGB = pow(blendedRGBLinear, 1.0 / 2.2); // Trở lại không gian gamma

					// Alpha kết hợp
					float blendedAlpha = max(c0.a, c1.a);
					// UNITY_APPLY_FOG(i.fogCoord, blendedAlpha);

					// Trả về màu cuối cùng với alpha
					return float4(blendedRGB, 1.0f);
				}
				ENDCG

			}
		}
			FallBack "Transparent/Diffuse"
}

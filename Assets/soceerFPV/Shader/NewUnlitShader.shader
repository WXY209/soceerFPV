Shader "Custom/SimpleIntenseHalo"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        // 光圈效果
        [HDR] _HaloColor ("Halo Color", Color) = (1, 0.9, 0.2, 1)
        _HaloIntensity ("Halo Intensity", Range(0, 20)) = 8.0
        _HaloSize ("Halo Size", Range(0.05, 0.8)) = 0.3
        _HaloThickness ("Halo Thickness", Range(0.01, 0.3)) = 0.1
        
        // 动态效果
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.5
        _Distortion ("Distortion", Range(0, 0.5)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry+10" // 在最后渲染
            "RenderPipeline" = "UniversalPipeline"
        }
        
        // Base Pass
        Pass
        {
            Name "Base"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite On
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.worldPos = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = texColor * _Color;
                
                // 基础光照
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 lighting = mainLight.color * NdotL * mainLight.distanceAttenuation;
                
                return float4(baseColor.rgb * lighting, baseColor.a);
            }
            ENDHLSL
        }
        
        // Halo Pass - 强烈的加法混合
        Pass
        {
            Name "IntenseHalo"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend One One
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vertHalo
            #pragma fragment fragHalo
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _HaloColor;
            float _HaloIntensity;
            float _HaloSize;
            float _HaloThickness;
            float _PulseSpeed;
            float _PulseAmount;
            float _Distortion;
            CBUFFER_END

            Varyings vertHalo(Attributes input)
            {
                Varyings output;
                
                // 添加噪波扭曲
                float time = _Time.y;
                float3 distortion = float3(
                    sin(time + input.positionOS.y * 10.0) * _Distortion,
                    cos(time + input.positionOS.z * 8.0) * _Distortion,
                    sin(time + input.positionOS.x * 12.0) * _Distortion
                );
                
                float3 distortedPos = input.positionOS.xyz + distortion * 0.1;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(distortedPos);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.worldPos = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.uv = input.uv;
                
                return output;
            }

            float4 fragHalo(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                
                // 强烈的多层光环
                float ring1 = smoothstep(_HaloSize - _HaloThickness, _HaloSize, fresnel);
                ring1 *= smoothstep(_HaloSize + _HaloThickness * 2.0, _HaloSize, fresnel);
                
                float ring2 = smoothstep(_HaloSize * 0.7 - _HaloThickness * 0.5, _HaloSize * 0.7, fresnel);
                ring2 *= smoothstep(_HaloSize * 0.7 + _HaloThickness, _HaloSize * 0.7, fresnel);
                
                float innerGlow = pow(fresnel, 3.0) * 0.5;
                float outerGlow = pow(fresnel, 0.5) * 0.3;
                
                // 组合所有效果
                float halo = (ring1 + ring2 * 0.7 + innerGlow + outerGlow);
                
                // 强烈脉冲
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount + (1.0 - _PulseAmount);
                halo *= pulse * _HaloIntensity;
                
                // 添加UV噪波变化
                float noise = sin(input.uv.x * 50.0 + _Time.y * 3.0) * 0.1 + 0.9;
                halo *= noise;
                
                // HDR颜色输出
                float3 haloColor = _HaloColor.rgb * halo;
                
                return float4(haloColor, 1.0);
            }
            ENDHLSL
        }
    }
}
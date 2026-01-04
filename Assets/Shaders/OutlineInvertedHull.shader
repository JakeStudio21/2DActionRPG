Shader "Custom/OutlineInvertedHull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.03
    
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            // Inverted Hull 기법: 앞면 컬링, 뒷면만 렌더링
            Cull Front
            ZWrite Off  // 깊이 버퍼 쓰기 끄기 (원본 캐릭터가 가리도록)
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 노멀 방향으로 정점 확장 (Inverted Hull)
                float3 normalOS = normalize(IN.normalOS);
                float3 positionOS = IN.positionOS.xyz + normalOS * _OutlineWidth;

                // 월드 → 클립 스페이스 변환
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                OUT.positionCS = vertexInput.positionCS;

                // 깊이 살짝 밀어내기 (카메라에서 멀어지도록)
                // 이렇게 하면 원본 캐릭터에 완전히 가려짐
                #if UNITY_REVERSED_Z
                    OUT.positionCS.z -= 0.0001; // Reversed Z (대부분의 플랫폼)
                #else
                    OUT.positionCS.z += 0.0001; // Normal Z
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}


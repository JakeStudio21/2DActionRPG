// 미니맵 맵 리빌(Map Reveal) 셰이더.
//
// [역할]
//   bgImage(Image 컴포넌트)에 머티리얼로 적용된다.
//   FogTexture(_FogMask)의 alpha 값으로 메인 맵 스프라이트의 가시성을 제어한다.
//
//   FinalAlpha = MainTex.a * (1.0 - FogMask.a)
//     → FogMask.a = 1 (미탐험): 맵 alpha = 0   → 보이지 않음
//     → FogMask.a = 0 (탐험됨): 맵 alpha = 원본 → 정상 표시
//
// [UV 매핑]
//   맵 스프라이트와 FogTexture는 모두 월드 worldMin~worldMax를 0~1 UV로 동일하게
//   사용하므로, 별도의 UV 변환 없이 IN.texcoord를 공유한다.
//   ※ 스프라이트는 텍스처 아틀라스에 포함되지 않은(standalone) 상태여야 한다.
//
// [어두운 배경]
//   맵이 투명해진 미탐험 구역 아래로 DarkBackground(Image)가 비쳐 보이도록
//   UI 계층에서 bgImage 아래에 어두운 배경 Image를 배치한다.

Shader "UI/MapReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Map Sprite", 2D) = "white" {}
        _FogMask  ("Fog Mask (RenderTexture)", 2D) = "black" {}
        _Color    ("Tint", Color) = (1,1,1,1)

        // Unity UI 내부 스텐실/마스크 지원 프로퍼티
        _StencilComp     ("Stencil Comparison", Float) = 8
        _Stencil         ("Stencil ID",         Float) = 0
        _StencilOp       ("Stencil Operation",  Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask",  Float) = 255
        _ColorMask       ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _FogMask;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;
            float4    _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex        = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord      = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color         = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 메인 맵 스프라이트 샘플링
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // FogMask 샘플링 (동일한 UV 사용 — 월드 0~1 좌표 일치)
                // alpha = 1: 미탐험(맵 숨김), alpha = 0: 탐험됨(맵 표시)
                half fogAlpha = tex2D(_FogMask, IN.texcoord).a;

                // 맵 alpha를 안개에 따라 감소
                color.a *= (1.0 - fogAlpha);

                // RectMask2D 클리핑
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

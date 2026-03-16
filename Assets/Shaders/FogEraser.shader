// 미니맵 안개(Fog of War) 지우개 셰이더.
//
// [역할]
//   RenderTexture(fogTexture)에 소프트 브러시를 스탬프 찍을 때 사용.
//   브러시 alpha가 강한 곳의 fog alpha를 0으로 깎아내 탐험된 영역을 투명하게 만든다.
//
// [블렌드 수식 — alpha 채널]
//   dst.a_new = dst.a_old × (1 - src.a)
//   → src.a = 1 (브러시 중심) : dst.a → 0  (완전 투명, 탐험됨)
//   → src.a = 0 (브러시 가장자리) : dst.a 변화 없음 (미탐험 유지)
//
// [사용처]
//   MinimapManager.UpdateFog() 내부에서 GL.Begin/End로 브러시 쿼드를 그릴 때 적용.

Shader "Minimap/FogEraser"
{
    Properties
    {
        _MainTex ("Brush Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            // alpha 채널만 기록 (RGB는 건드리지 않음 → fog 색상 유지)
            ColorMask A

            // dst.a = src.a * 0 + dst.a * (1 - src.a)
            Blend Zero OneMinusSrcAlpha

            ZWrite Off
            ZTest  Always
            Cull   Off

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 brush = tex2D(_MainTex, i.uv);
                // ColorMask A 로 인해 alpha 값만 렌더 타겟에 기록됨.
                // Blend Zero OneMinusSrcAlpha 에 의해 dst.a *= (1 - brush.a)
                return fixed4(0.0, 0.0, 0.0, brush.a);
            }
            ENDCG
        }
    }
}

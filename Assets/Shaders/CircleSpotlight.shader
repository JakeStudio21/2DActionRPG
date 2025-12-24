Shader "UI/CircleSpotlight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Overlay Color", Color) = (0,0,0,0.7)
        _HoleCenter ("Hole Center (Normalized 0~1)", Vector) = (0.5, 0.5, 0, 0)
        _HoleRadius ("Hole Radius (Pixels)", Float) = 150
        _SoftEdge ("Soft Edge (Pixels)", Float) = 50
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 screenPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _HoleCenter;
            float _HoleRadius;
            float _SoftEdge;
            
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                
                // 스크린 좌표 계산 (0~1 정규화)
                float4 screenPos = ComputeScreenPos(o.vertex);
                o.screenPos = screenPos.xy / screenPos.w;
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // 텍스처 샘플링
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 현재 픽셀의 스크린 좌표 (0~1)
                float2 pixelPos = i.screenPos;
                
                // 구멍 중심과의 거리 계산 (픽셀 단위)
                float2 diff = (pixelPos - _HoleCenter.xy) * float2(_ScreenParams.x, _ScreenParams.y);
                float dist = length(diff);
                
                // 구멍 반지름 안쪽: 완전 투명 (알파 0)
                // 구멍 반지름 + 경계 바깥: 완전 불투명 (설정된 알파)
                // 경계 영역: smoothstep으로 부드러운 페이드
                float alpha = smoothstep(_HoleRadius, _HoleRadius + _SoftEdge, dist);
                
                // 최종 색상 (검은색 오버레이)
                col.rgb = _Color.rgb;
                col.a = _Color.a * alpha * i.color.a;
                
                return col;
            }
            ENDCG
        }
    }
}


using UnityEngine;
using System;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 Step 타입 정의 (MVP 5종)
    /// </summary>
    public enum CutsceneStepType
    {
        Image,      // 배경/초상 이미지 표시
        Dialogue,   // 대사 텍스트 표시
        Wait,       // 지정 시간 대기
        SFX,        // 효과음 재생 (CueSystem 연동)
        Callback    // 컷신 종료 후 처리
    }

    /// <summary>
    /// 컷신의 개별 Step 데이터 구조
    /// 각 Step은 하나의 독립적인 연출 단위
    /// </summary>
    [System.Serializable]
    public class CutsceneStep
    {
        [Header("=== 기본 정보 ===")]
        [Tooltip("이 Step의 타입")]
        public CutsceneStepType stepType;
        
        [Header("=== Wait/SFX Step 설정 ===")]
        [Tooltip("대기 시간 (초) - Wait Step: 대기 시간 / SFX Step: 효과음 재생 후 대기 시간")]
        public float duration = 1f;
        
        [Header("=== Image Step 설정 ===")]
        [Tooltip("표시할 이미지 스프라이트")]
        public Sprite imageSprite;
        
        [Tooltip("Fade 레이어 사용 (배경/초상 위에 표시, Fade to Black 등)")]
        public bool isFade = false;
        
        [Tooltip("초상 이미지인가? (false = 배경 이미지) - isFade가 true면 무시됨")]
        public bool isPortrait = false;
        
        [Tooltip("페이드 인 효과 적용")]
        public bool fadeIn = true;
        
        [Tooltip("이미지 위치 (앵커 기준)")]
        public Vector2 imagePosition = Vector2.zero;
        
        [Tooltip("이미지 스케일")]
        public float imageScale = 1f;
        
        [Tooltip("이미지 표시 지속 시간 (초) - Image Step 타입일 때 사용")]
        public float imageDuration = 2f;
        
        [Header("=== Dialogue Step 설정 ===")]
        [Tooltip("화자 이름 (비어있으면 이름 숨김)")]
        public string speakerName = "";
        
        [Tooltip("대사 텍스트")]
        [TextArea(3, 10)]
        public string dialogueText = "";
        
        [Tooltip("타이핑 속도 (글자/초, 0 = 즉시 표시)")]
        public float typingSpeed = 20f;
        
        [Header("=== SFX Step 설정 ===")]
        [Tooltip("CueSystem 이벤트 키 (예: bgm_intro, image_appear)")]
        public string sfxEventKey = "";
        
        [Tooltip("CueSystem 도메인 (기본: Cutscene)")]
        public string sfxDomain = "Cutscene";
        
        [Header("=== Callback Step 설정 ===")]
        [Tooltip("호출할 메서드 이름 (CutsceneManager에 등록된 콜백)")]
        public string callbackMethodName = "";
        
        [Header("=== 디버그 ===")]
        [Tooltip("Inspector에서 이 Step을 설명하는 메모")]
        [TextArea(2, 4)]
        public string note = "";
        
        /// <summary>
        /// 이 Step의 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            switch (stepType)
            {
                case CutsceneStepType.Image:
                    if (imageSprite == null)
                        return false;
                    if (imageDuration <= 0)
                        return false;
                    return true;
                
                case CutsceneStepType.Dialogue:
                    return !string.IsNullOrEmpty(dialogueText);
                
                case CutsceneStepType.Wait:
                    if (duration <= 0)
                        return false;
                    return true;
                
                case CutsceneStepType.SFX:
                    return !string.IsNullOrEmpty(sfxEventKey);
                
                case CutsceneStepType.Callback:
                    return !string.IsNullOrEmpty(callbackMethodName);
                
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Inspector에서 표시할 Step 이름
        /// </summary>
        public string GetDisplayName()
        {
            switch (stepType)
            {
                case CutsceneStepType.Image:
                    string imageType = isFade ? "Fade" : (isPortrait ? "초상" : "배경");
                    string imageName = imageSprite != null ? imageSprite.name : "없음";
                    return $"[이미지] {imageType}: {imageName}";
                
                case CutsceneStepType.Dialogue:
                    string speaker = !string.IsNullOrEmpty(speakerName) ? speakerName : "???";
                    string preview = dialogueText.Length > 20 
                        ? dialogueText.Substring(0, 20) + "..." 
                        : dialogueText;
                    return $"[대사] {speaker}: {preview}";
                
                case CutsceneStepType.Wait:
                    return $"[대기] {duration}초";
                
                case CutsceneStepType.SFX:
                    return $"[효과음] {sfxEventKey} ({duration}초)";
                
                case CutsceneStepType.Callback:
                    return $"[콜백] {callbackMethodName}";
                
                default:
                    return "[알 수 없음]";
            }
        }
    }
}

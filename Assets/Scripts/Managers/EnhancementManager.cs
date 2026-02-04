using UnityEngine;
using System;
using Systems;

namespace Managers
{
    /// <summary>
    /// 강화 관리자 (경고 팝업 표시)
    /// </summary>
    public class EnhancementManager : MonoBehaviour
    {
        private static EnhancementManager _instance;
        public static EnhancementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EnhancementManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EnhancementManager");
                        _instance = go.AddComponent<EnhancementManager>();
                        DontDestroyOnLoad(go);
                        Debug.Log("✨ [EnhancementManager] 자동 생성 및 초기화");
                    }
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 파괴 경고 표시 (파괴 구간 강화 시)
        /// </summary>
        public void ShowDestructionWarning(EnhancementWarningData data, Action<bool> onComplete)
        {
            // 테스트용: UI 없이 콘솔 경고 + 즉시 확인
            Debug.LogWarning($"[EnhancementManager] 강화 경고:\n{data.warningMessage}\n(테스트 모드: 자동 확인)");
            onComplete?.Invoke(true);
            
            // TODO: 추가 예정 - 전용 EnhancementWarningPopup UI 구현
            // if (enhancementWarningPopup != null)
            // {
            //     enhancementWarningPopup.Show(data, onComplete);
            // }
        }
        
        /// <summary>
        /// 하락 경고 표시 (하락 구간 강화 시)
        /// </summary>
        public void ShowDowngradeWarning(EnhancementWarningData data, Action<bool> onComplete)
        {
            // 테스트용: UI 없이 콘솔 경고 + 즉시 확인
            Debug.LogWarning($"[EnhancementManager] 강화 경고 (하락):\n{data.warningMessage}\n(테스트 모드: 자동 확인)");
            onComplete?.Invoke(true);
        }
    }
}


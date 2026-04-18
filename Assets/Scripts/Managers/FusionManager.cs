using UnityEngine;
using System;
using System.Collections.Generic;
using UI.Popups;
using Systems;

namespace Managers
{
    /// <summary>
    /// 합성 관리자 (경고 팝업 표시)
    /// </summary>
    public class FusionManager : MonoBehaviour
    {
        private static FusionManager _instance;
        public static FusionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FusionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("FusionManager");
                        _instance = go.AddComponent<FusionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private BindWarningPopup genericWarningPopup; // 재활용 가능한 범용 팝업

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (genericWarningPopup == null)
            {
                // 씬에서 BindWarningPopup을 찾아 재활용 (임시)
                genericWarningPopup = FindObjectOfType<BindWarningPopup>(true);
                if (genericWarningPopup == null)
                {
                    Debug.LogWarning("[FusionManager] 범용 경고 팝업 (BindWarningPopup)을 찾을 수 없습니다. UI가 표시되지 않습니다.");
                }
            }
        }

        /// <summary>
        /// 강화 경고 표시 (강화된 아이템 합성 시)
        /// </summary>
        public void ShowEnhancementWarning(FusionWarningData data, Action<bool> onComplete)
        {
            // 테스트용: UI 없이 콘솔 경고 + 즉시 확인
            Debug.LogWarning($"[FusionManager] 합성 경고:\n{data.warningMessage}\n(테스트 모드: 자동 확인)");
            onComplete?.Invoke(true);
            
            // TODO: 추가 예정 - 전용 FusionWarningPopup UI 구현
            // if (fusionWarningPopup != null)
            // {
            //     fusionWarningPopup.Show(data, onComplete);
            // }
        }
    }
}


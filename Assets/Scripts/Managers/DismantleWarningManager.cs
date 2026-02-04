using UnityEngine;
using System;

namespace Managers
{
    /// <summary>
    /// 분해 경고 팝업 관리자
    /// </summary>
    public class DismantleWarningManager : MonoBehaviour
    {
        private static DismantleWarningManager _instance;
        public static DismantleWarningManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DismantleWarningManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DismantleWarningManager");
                        _instance = go.AddComponent<DismantleWarningManager>();
                        DontDestroyOnLoad(go);
                        Debug.Log("✨ [DismantleWarningManager] 자동 생성 및 초기화");
                    }
                }
                return _instance;
            }
        }
        
        // TODO: DismantleWarningPopup 프리팹 연결 필요 (Phase 5 이후)
        // [SerializeField] private DismantleWarningPopup dismantleWarningPopup;
        
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
        /// 분해 경고 표시 (UI 미구현 시 콘솔 로그)
        /// </summary>
        /// <param name="data">경고 데이터</param>
        /// <param name="onComplete">완료 콜백 (confirmed, dontShowAgain)</param>
        public void ShowWarning(DismantleWarningData data, Action<bool> onComplete)
        {
            // TODO: UI 프리팹 구현 시 주석 해제
            // if (dismantleWarningPopup != null)
            // {
            //     dismantleWarningPopup.Show(data, (confirmed) =>
            //     {
            //         onComplete?.Invoke(confirmed);
            //     });
            //     return;
            // }
            
            // UI 없을 때: 콘솔 로그로 대체
            Debug.LogWarning($"[DismantleWarningManager] 분해 경고:\n{data.warningMessage}");
            Debug.LogWarning("[DismantleWarningManager] DismantleWarningPopup UI가 없어 자동 승인됨");
            
            // UI 없으므로 자동 승인 (테스트 목적)
            onComplete?.Invoke(true);
        }
        
        /// <summary>
        /// 분해 경고 필요 여부 확인 + 표시
        /// </summary>
        public void ShowWarningIfNeeded(ItemInstanceId instanceId, Action<bool> onComplete)
        {
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null)
            {
                Debug.LogError($"[DismantleWarningManager] 아이템을 찾을 수 없음: {instanceId}");
                onComplete?.Invoke(false);
                return;
            }
            
            // 경고 필요 여부 확인
            if (!Systems.DismantleSystem.NeedsWarning(instanceId))
            {
                // 경고 불필요 → 즉시 승인
                onComplete?.Invoke(true);
                return;
            }
            
            // 경고 데이터 생성
            var template = ItemTemplateResolver.Load(itemData.templateName);
            if (template == null)
            {
                Debug.LogError($"[DismantleWarningManager] 템플릿을 찾을 수 없음: {itemData.templateName}");
                onComplete?.Invoke(false);
                return;
            }
            
            var bindInfo = account.GetBindInfo(instanceId);
            var warningData = DismantleWarningData.CreateWarning(
                instanceId,
                template.equipmentName,
                template.itemGrade,
                itemData.enhancementLevel,
                bindInfo.isBound,
                bindInfo.characterSlotIndex
            );
            
            // 경고 표시
            ShowWarning(warningData, onComplete);
        }
    }
}


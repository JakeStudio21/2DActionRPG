using UnityEngine;
using StageSystem;
using System.Collections.Generic;

/// <summary>
/// Phase 1 챕터 진행도 테스트 스크립트
/// Lobby 씬의 빈 GameObject에 추가
/// </summary>
public class TestChapterProgress : MonoBehaviour
{
    private int updateFrameCount = 0;
    
    private void Update()
    {
        updateFrameCount++;
        
        // 키 입력 전체 디버깅
        if (Input.anyKeyDown)
        {
            Debug.Log($"🔑 [Frame {updateFrameCount}] 키 입력 감지됨");
        }
        // F1 키: 챕터 1 완료 처리
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F1 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F1 - 챕터 1 완료 처리");
            
            if (StageProgressManager.Instance == null)
            {
                Debug.LogError("❌ StageProgressManager.Instance가 null입니다!");
                return;
            }
            
            if (PlayerDataManager.Instance == null)
            {
                Debug.LogError("❌ PlayerDataManager.Instance가 null입니다!");
                return;
            }
            
            try
            {
                // PlayerSlotData 인스턴스 추적
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData == null)
                {
                    Debug.LogError("❌ GetCurrentSlotData()가 null을 반환했습니다!");
                    return;
                }
                
                Debug.Log($"🔍 PlayerSlotData 인스턴스: {slotData.GetHashCode()}");
                Debug.Log($"🔍 슬롯 인덱스: {slotData.slotIndex}, 캐릭터: {slotData.playerName}");
                Debug.Log($"🔍 clearedChapters 리스트 인스턴스: {slotData.clearedChapters.GetHashCode()}");
                Debug.Log($"🔍 clearedChapters.Count (처리 전): {slotData.clearedChapters.Count}");
                Debug.Log($"🔍 clearedChapters 내용 (처리 전): [{string.Join(", ", slotData.clearedChapters)}]");
                
                // 완료 처리 전 상태 확인
                Debug.Log("🔍 완료 처리 전 클리어한 챕터 목록:");
                var beforeClearedChapters = StageProgressManager.Instance.GetClearedChapters();
                Debug.Log($"🎯 GetClearedChapters() 결과: [{string.Join(", ", beforeClearedChapters)}]");
                
                Debug.Log("🔍 CompleteChapter(1) 호출 시작...");
                StageProgressManager.Instance.CompleteChapter(1);
                Debug.Log("✅ CompleteChapter(1) 호출 완료");
                
                // CompleteChapter 실행 후 같은 인스턴스 확인
                var slotDataAfter = PlayerDataManager.Instance.GetCurrentSlotData();
                Debug.Log($"🔍 PlayerSlotData 인스턴스 (처리 후): {slotDataAfter.GetHashCode()}");
                Debug.Log($"🔍 인스턴스 동일 여부: {slotData.GetHashCode() == slotDataAfter.GetHashCode()}");
                Debug.Log($"🔍 clearedChapters.Count (처리 후): {slotDataAfter.clearedChapters.Count}");
                Debug.Log($"🔍 clearedChapters 내용 (처리 후): [{string.Join(", ", slotDataAfter.clearedChapters)}]");
                
                // 완료 처리 후 상태 확인
                Debug.Log("🔍 완료 처리 후 GetClearedChapters() 호출:");
                var afterClearedChapters = StageProgressManager.Instance.GetClearedChapters();
                Debug.Log($"🎯 GetClearedChapters() 결과: [{string.Join(", ", afterClearedChapters)}]");
                
                // 직접 확인
                Debug.Log($"🔍 slotDataAfter.IsChapterCleared(1): {slotDataAfter.IsChapterCleared(1)}");
                
                // 챕터 2 해금 여부 확인
                bool isChapter2Unlocked = StageProgressManager.Instance.IsChapterUnlocked(2);
                Debug.Log($"🔍 Chapter 2 해금 상태: {isChapter2Unlocked}");
                
                Debug.Log($"✅ [Frame {updateFrameCount}] ========== F1 완료 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ CompleteChapter 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F2 키: 챕터 1~5 해금 상태 출력
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F2 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F2 - 챕터 해금 상태 체크 시작");
            
            if (StageProgressManager.Instance == null)
            {
                Debug.LogError("❌ StageProgressManager.Instance가 null입니다!");
                return;
            }
            
            Debug.Log("✅ StageProgressManager Instance 존재 확인");
            
            if (!StageProgressManager.Instance.IsInitialized)
            {
                Debug.LogWarning("⚠️ StageProgressManager가 아직 초기화되지 않았습니다!");
                return;
            }
            
            Debug.Log("✅ StageProgressManager 초기화 완료");
            
            if (PlayerDataManager.Instance == null)
            {
                Debug.LogError("❌ PlayerDataManager.Instance가 null입니다!");
                return;
            }
            
            try
            {
                // PlayerSlotData 인스턴스 확인
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData == null)
                {
                    Debug.LogError("❌ GetCurrentSlotData()가 null을 반환했습니다!");
                    return;
                }
                
                Debug.Log($"🔍 PlayerSlotData 인스턴스: {slotData.GetHashCode()}");
                Debug.Log($"🔍 슬롯 인덱스: {slotData.slotIndex}, 캐릭터: {slotData.playerName}");
                Debug.Log($"🔍 clearedChapters.Count: {slotData.clearedChapters.Count}");
                Debug.Log($"🔍 clearedChapters 내용: [{string.Join(", ", slotData.clearedChapters)}]");
                
                Debug.Log("=== 챕터 해금 상태 ===");
                for (int i = 1; i <= 5; i++)
                {
                    Debug.Log($"🔍 Chapter {i} 체크 중...");
                    bool unlocked = StageProgressManager.Instance.IsChapterUnlocked(i);
                    Debug.Log($"  → 해금: {unlocked}");
                    bool cleared = StageProgressManager.Instance.IsChapterCleared(i);
                    Debug.Log($"  → 클리어: {cleared}");
                    Debug.Log($"📊 Chapter {i}: 해금={unlocked}, 클리어={cleared}");
                }
                
                Debug.Log("🔍 GetClearedChapters() 호출 중...");
                var clearedChapters = StageProgressManager.Instance.GetClearedChapters();
                Debug.Log($"🎯 GetClearedChapters() 결과: [{string.Join(", ", clearedChapters)}]");
                
                Debug.Log($"✅ [Frame {updateFrameCount}] ========== F2 체크 완료 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 챕터 상태 체크 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F3 키: CH01_ST01 강제 해금
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("🧪 [Test] F3 - CH01_ST01 강제 해금");
            
            if (StageProgressManager.Instance == null)
            {
                Debug.LogError("❌ StageProgressManager.Instance가 null입니다!");
                return;
            }
            
            try
            {
                StageProgressManager.Instance.UnlockStage("CH01_ST01");
                
                // 해금 확인
                bool isUnlocked = StageProgressManager.Instance.IsStageUnlocked("CH01_ST01");
                Debug.Log($"✅ CH01_ST01 해금 완료: {isUnlocked}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 스테이지 해금 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F4 키: CH01_ST10 클리어 처리 (챕터 1 완료)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("🔍 [Test] ========== F4 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F4 - CH01_ST10 클리어 처리");
            
            if (StageProgressManager.Instance == null)
            {
                Debug.LogError("❌ StageProgressManager.Instance가 null입니다!");
                return;
            }
            
            try
            {
                // 클리어 처리 전 상태 확인
                Debug.Log("🔍 클리어 처리 전 챕터 1 상태:");
                bool isChapter1ClearedBefore = StageProgressManager.Instance.IsChapterCleared(1);
                Debug.Log($"  → 챕터 1 클리어 상태: {isChapter1ClearedBefore}");
                
                // CH01_ST10이 존재하는지 확인
                Debug.Log("🔍 CH01_ST10 진행도 확인 중...");
                var progress = StageProgressManager.Instance.GetStageProgress("CH01_ST10");
                if (progress == null)
                {
                    Debug.LogWarning("⚠️ CH01_ST10 진행도가 없습니다. 강제로 해금 후 클리어 처리합니다.");
                    StageProgressManager.Instance.UnlockStage("CH01_ST10");
                    Debug.Log("✅ CH01_ST10 해금 완료");
                }
                
                Debug.Log("🔍 CompleteStage(CH01_ST10, 120f) 호출 시작...");
                StageProgressManager.Instance.CompleteStage("CH01_ST10", 120f);
                Debug.Log("✅ CompleteStage 호출 완료");
                
                // 클리어 확인
                bool isCompleted = StageProgressManager.Instance.IsStageCompleted("CH01_ST10");
                Debug.Log($"🎯 CH01_ST10 클리어 상태: {isCompleted}");
                
                // 챕터 1 클리어 상태 확인
                bool isChapter1ClearedAfter = StageProgressManager.Instance.IsChapterCleared(1);
                Debug.Log($"🎯 챕터 1 클리어 상태: {isChapter1ClearedAfter}");
                
                // 챕터 2 해금 상태 확인
                bool isChapter2Unlocked = StageProgressManager.Instance.IsChapterUnlocked(2);
                Debug.Log($"🎯 챕터 2 해금 상태: {isChapter2Unlocked}");
                
                Debug.Log("✅ [Test] ========== F4 완료 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 스테이지 클리어 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F5 키: PlayerSlotData 직접 확인 (추가)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F5 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F5 - PlayerSlotData 직접 확인");
            
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                Debug.LogError("❌ PlayerDataManager 또는 선택된 슬롯이 없습니다!");
                return;
            }
            
            try
            {
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData != null)
                {
                    Debug.Log($"🔍 PlayerSlotData 인스턴스: {slotData.GetHashCode()}");
                    Debug.Log($"🎮 슬롯 인덱스: {slotData.slotIndex}");
                    Debug.Log($"👤 캐릭터: {slotData.playerName} ({slotData.playerType})");
                    Debug.Log($"🎯 클리어한 챕터: [{string.Join(", ", slotData.clearedChapters)}]");
                    Debug.Log($"📊 스테이지 진행도 개수: {slotData.stageProgresses.Count}");
                }
                else
                {
                    Debug.LogError("❌ GetCurrentSlotData()가 null을 반환했습니다!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ PlayerSlotData 확인 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F6 키: Update() 메서드 생존 확인 (디버깅용)
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log($"✅ [Frame {updateFrameCount}] Update() 메서드 정상 작동 중! (F6 키 입력 감지)");
        }
        
        // F7 키: ChapterManager 테스트 (Phase 2)
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F7 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F7 - ChapterManager 테스트");
            
            if (StageSystem.ChapterManager.Instance == null)
            {
                Debug.LogError("❌ ChapterManager.Instance가 null입니다!");
                Debug.LogWarning("💡 Lobby 씬에 ChapterManager GameObject가 있는지 확인하세요!");
                return;
            }
            
            try
            {
                Debug.Log("✅ ChapterManager Instance 존재 확인");
                
                // 초기화 확인
                if (!StageSystem.ChapterManager.Instance.IsInitialized)
                {
                    Debug.LogWarning("⚠️ ChapterManager가 아직 초기화되지 않았습니다!");
                    return;
                }
                
                Debug.Log("✅ ChapterManager 초기화 완료");
                
                // 챕터 개수 확인
                int chapterCount = StageSystem.ChapterManager.Instance.GetChapterCount();
                Debug.Log($"📚 총 챕터 개수: {chapterCount}");
                
                // 모든 챕터 목록 가져오기
                var allChapters = StageSystem.ChapterManager.Instance.GetAllChapters();
                Debug.Log($"📚 로드된 챕터 목록:");
                foreach (var chapter in allChapters)
                {
                    if (chapter != null)
                    {
                        Debug.Log($"  - {chapter.chapterTitle} (ID: {chapter.chapterId}, 스테이지: {chapter.stageCount}개)");
                    }
                }
                
                Debug.Log("=== 챕터 1 상세 정보 ===");
                StageSystem.ChapterManager.Instance.PrintChapterInfo(1);
                
                Debug.Log("=== 챕터 2 상세 정보 ===");
                StageSystem.ChapterManager.Instance.PrintChapterInfo(2);
                
                // 다음 플레이 가능한 스테이지 확인
                Debug.Log("🎯 다음 플레이 가능한 스테이지:");
                for (int i = 1; i <= 5; i++)
                {
                    string nextStage = StageSystem.ChapterManager.Instance.GetNextPlayableStage(i);
                    bool unlocked = StageSystem.ChapterManager.Instance.IsChapterUnlocked(i);
                    
                    if (unlocked)
                    {
                        Debug.Log($"  - Chapter {i}: {nextStage ?? "없음"}");
                    }
                    else
                    {
                        Debug.Log($"  - Chapter {i}: 🔒 잠김");
                    }
                }
                
                Debug.Log($"✅ [Frame {updateFrameCount}] ========== F7 테스트 완료 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ ChapterManager 테스트 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F8 키: CutsceneManager + 컷신 재생 테스트 (Phase 4)
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F8 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F8 - 컷신 시스템 테스트 (Phase 4)");
            
            if (CutsceneSystem.CutsceneManager.Instance == null)
            {
                Debug.LogError("❌ CutsceneManager.Instance가 null입니다!");
                return;
            }
            
            try
            {
                Debug.Log("✅ CutsceneManager Instance 존재 확인");
                
                // CH01_START 컷신 시청 여부 확인
                string cutsceneId = "CH01_START";
                bool hasSeen = CutsceneSystem.CutsceneManager.Instance.HasSeenCutscene(cutsceneId);
                
                Debug.Log($"🎬 컷신 '{cutsceneId}' 시청 여부: {hasSeen}");
                
                if (hasSeen)
                {
                    Debug.Log("⚠️ 이미 시청한 컷신입니다. 테스트를 위해 강제로 재생합니다.");
                    Debug.Log("💡 시청 기록을 초기화하려면 F9 키를 누르세요.");
                }
                else
                {
                    Debug.Log("✅ 첫 시청입니다. 컷신을 재생합니다.");
                }
                
                // 컷신 재생
                Debug.Log($"🎬 컷신 재생 시작: {cutsceneId}");
                CutsceneSystem.CutsceneManager.Instance.PlayCutscene(cutsceneId);
                
                // 컷신 종료 이벤트 구독 (일회성)
                System.Action<string> onCutsceneEnd = null;
                onCutsceneEnd = (endedCutsceneId) =>
                {
                    if (endedCutsceneId == cutsceneId)
                    {
                        Debug.Log($"🎬 컷신 종료: {cutsceneId}");
                        
                        // 시청 여부 재확인
                        bool hasSeenAfter = CutsceneSystem.CutsceneManager.Instance.HasSeenCutscene(cutsceneId);
                        Debug.Log($"🎬 컷신 '{cutsceneId}' 시청 후 기록: {hasSeenAfter}");
                        
                        if (hasSeenAfter)
                        {
                            Debug.Log("✅ 시청 기록이 정상적으로 저장되었습니다!");
                        }
                        else
                        {
                            Debug.LogError("❌ 시청 기록 저장 실패!");
                        }
                        
                        // 이벤트 구독 해제
                        CutsceneSystem.CutsceneManager.Instance.OnCutsceneEnd -= onCutsceneEnd;
                    }
                };
                
                CutsceneSystem.CutsceneManager.Instance.OnCutsceneEnd += onCutsceneEnd;
                
                Debug.Log($"✅ [Frame {updateFrameCount}] ========== F8 테스트 시작 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 컷신 테스트 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F9 키: 컷신 시청 기록 초기화 (디버그용)
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F9 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F9 - 컷신 시청 기록 초기화");
            
            try
            {
                CutsceneSystem.CutsceneProgressTracker.ClearAllProgress();
                Debug.Log("✅ 모든 컷신 시청 기록이 초기화되었습니다!");
                Debug.Log("💡 이제 F8 키를 눌러 컷신을 다시 재생할 수 있습니다.");
                
                Debug.Log($"✅ [Frame {updateFrameCount}] ========== F9 완료 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 시청 기록 초기화 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /* ========================================
         * 🧪 Phase 5 테스트 코드 (주석처리됨 - 필요시 활성화)
         * ========================================
         
        // F12 키: 챕터 1 완료 시뮬레이션 (Phase 5 테스트)
        if (Input.GetKeyDown(KeyCode.F12))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F12 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F12 - 챕터 1 완료 시뮬레이션 (CH01_ST10 클리어)");
            
            try
            {
                if (PlayerDataManager.Instance == null)
                {
                    Debug.LogError("❌ PlayerDataManager.Instance가 null입니다!");
                    return;
                }
                
                var currentSlotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (currentSlotData == null)
                {
                    Debug.LogError("❌ 현재 슬롯 데이터가 없습니다!");
                    return;
                }
                
                // CH01_ST10 클리어 처리
                string stageId = "CH01_ST10";
                Debug.Log($"🎯 {stageId} 클리어 처리 중...");
                
                if (StageProgressManager.Instance != null)
                {
                    StageProgressManager.Instance.CompleteStage(stageId, 100); // 100초로 클리어
                    Debug.Log($"✅ {stageId} 클리어 완료");
                    
                    // 챕터 1 완료 기록
                    StageProgressManager.Instance.CompleteChapter(1);
                    Debug.Log("✅ 챕터 1 완료 기록");
                }
                
                // 🎬 Phase 5: 챕터 종료 컷신 예약 (SelectedPlayerData에 직접 설정)
                if (PlayerDataManager.Instance.selectedPlayerData != null)
                {
                    PlayerDataManager.Instance.selectedPlayerData.pendingCutsceneId = "CH01_CLEAR";
                    PlayerDataManager.Instance.selectedPlayerData.pendingChapterId = 1;
                    PlayerDataManager.Instance.SaveCurrentSlot();
                    
                    Debug.Log($"🎬 챕터 종료 컷신 예약: CH01_CLEAR (SelectedPlayerData)");
                }
                else
                {
                    Debug.LogError("❌ selectedPlayerData가 null입니다!");
                }
                Debug.Log("💡 로비로 돌아가면 챕터 종료 컷신이 자동 재생됩니다.");
                Debug.Log("💡 F11 키를 눌러 예약된 컷신을 확인하세요.");
                
                Debug.Log($"✅ [Frame {updateFrameCount}] ========== F12 완료 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 챕터 1 완료 시뮬레이션 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // F11 키: 예약된 컷신 확인 (Phase 5 테스트)
        if (Input.GetKeyDown(KeyCode.F11))
        {
            Debug.Log($"🔍 [Frame {updateFrameCount}] ========== F11 키 입력 감지 ==========");
            Debug.Log("🧪 [Test] F11 - 예약된 컷신 확인");
            
            try
            {
                if (PlayerDataManager.Instance == null)
                {
                    Debug.LogError("❌ PlayerDataManager.Instance가 null입니다!");
                    return;
                }
                
                var currentSlotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (currentSlotData == null)
                {
                    Debug.LogError("❌ 현재 슬롯 데이터가 없습니다!");
                    return;
                }
                
                Debug.Log("=== 예약된 컷신 정보 ===");
                Debug.Log($"  - pendingCutsceneId: {currentSlotData.pendingCutsceneId ?? "없음"}");
                Debug.Log($"  - pendingChapterId: {currentSlotData.pendingChapterId}");
                
                if (!string.IsNullOrEmpty(currentSlotData.pendingCutsceneId))
                {
                    Debug.Log("✅ 예약된 컷신이 있습니다!");
                    Debug.Log("💡 로비 씬으로 돌아가면 자동 재생됩니다.");
                }
                else
                {
                    Debug.Log("⚠️ 예약된 컷신이 없습니다.");
                    Debug.Log("💡 F12 키를 눌러 챕터 1 완료를 시뮬레이션하세요.");
                }
                
                Debug.Log($"✅ [Frame {updateFrameCount}] ========== F11 완료 ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 예약된 컷신 확인 에러: {e.Message}\n{e.StackTrace}");
            }
        }
        
        ======================================== */
    }
}
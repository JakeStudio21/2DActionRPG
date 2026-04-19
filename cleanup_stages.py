# -*- coding: utf-8 -*-
"""
Stages 폴더 Debug.Log 정리 스크립트
- if (enableDebugLogs/showDebugLogs) { Debug.Log(...); } 블록 처리
- 지정한 핵심 로그 → Dbg.Log 단독 라인으로 변환
- 나머지 블록 전체 제거
- enableDebugLogs 필드 선언 제거
"""
import re, os

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts\Stages"

def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()

def write(path, content):
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

def process_file(path, keep_substrings=None):
    if not os.path.exists(path):
        print(f"  [SKIP] {path} 없음")
        return
    
    content = read(path)
    original = content
    keep_substrings = keep_substrings or []
    
    # ── 1. if (enableDebugLogs/showDebugLogs) { ... } 블록 처리 ──
    # 단일 { } 블록 (중첩 없음 가정)
    def handle_debug_block(m):
        block_inner = m.group(0)
        indent = m.group(1)
        
        for keep_sub in keep_substrings:
            if keep_sub in block_inner:
                # Debug.Log( → Dbg.Log( 로 변환하여 if 블록 없이 바로 반환
                converted = block_inner
                # 블록 내에서 Debug.Log 라인만 추출하여 들여쓰기 맞추기
                log_match = re.search(r'([ \t]*)Debug\.Log\((.+?)(?:\n[ \t]*\$".+?)?\);', converted, re.DOTALL)
                if log_match:
                    log_line = log_match.group(0).strip()
                    dbg_line = log_line.replace("Debug.Log(", "Dbg.Log(")
                    return f"\n{indent}{dbg_line}\n"
        return ""  # 제거
    
    pattern = re.compile(
        r'\n([ \t]*)if\s*\(\s*(?:enableDebugLogs|showDebugLogs|debugMode)\s*\)\s*\n[ \t]*\{[^{}]*\}',
        re.DOTALL
    )
    content = pattern.sub(handle_debug_block, content)
    
    # ── 2. 블록 없이 단독으로 남은 Debug.Log 라인 제거 (잔여 처리) ──
    # 멀티라인 Debug.Log (끝에 ; 없이 다음 줄에서 이어지는 경우)
    lines = content.split("\n")
    result = []
    skip_cont = False
    for line in lines:
        s = line.strip()
        if re.search(r'Debug\.Log\(', line) and not re.search(r'Debug\.Log(?:Warning|Error)', line):
            if not s.endswith(';'):
                skip_cont = True
            continue
        if skip_cont:
            if s.endswith(');') or s.endswith('");'):
                skip_cont = False
            continue
        result.append(line)
    content = "\n".join(result)
    
    # ── 3. enableDebugLogs / showDebugLogs 필드 선언 제거 ──
    content = re.sub(
        r'[ \t]*(?:\[SerializeField\]\s*)?(?:public|private|protected)?\s*bool\s+(?:enableDebugLogs|showDebugLogs)\s*=\s*(?:true|false);[ \t]*(?:\/\/[^\n]*)?\n',
        '', content
    )
    # 디버그 전용 [Header] 제거
    content = re.sub(r'[ \t]*\[Header\("[^"]*[Dd]ebug[^"]*"\)\]\s*\n', '', content)
    
    # ── 4. 고아 if (enableDebugLogs) 라인 (블록 없는 것) 제거 ──
    content = re.sub(r'[ \t]*if\s*\(\s*(?:enableDebugLogs|showDebugLogs)\s*\)\s*\n', '', content)
    
    # ── 5. #region 디버그 / #endregion 제거 ──
    content = re.sub(r'[ \t]*#region[^\n]*[Dd]ebug[^\n]*\n', '', content)
    content = re.sub(r'[ \t]*#endregion[^\n]*[Dd]ebug[^\n]*\n', '', content)
    
    if content != original:
        write(path, content)
        print(f"  ✅ {os.path.basename(path)}")
    else:
        print(f"  -- {os.path.basename(path)} (변경 없음)")

# ===============================================================
# StageManager.cs
# ===============================================================
process_file(
    os.path.join(BASE, "Core", "StageManager.cs"),
    keep_substrings=[
        "초기화 완료",
        "스테이지 시작: {config.StageID}",
        "웨이브 {currentWaveIndex + 1}",
        "개별 웨이브 완료: Wave[{waveIndex}]",
        "웨이브 완료: {completedWave.WaveID}",
        "승리! VictorySequence 시작",
        "패배 감지 - 플레이어 사망",
        "패배 감지 - 제한시간 초과",
        "스테이지 {(success",
        "보상 처리 완료: 골드 {rewardResult",
        "VictorySequence 완료 후 CompleteStage",
        "Survival 승리!",
        "타임오버!",
    ]
)

# ===============================================================
# WaveController.cs
# ===============================================================
process_file(
    os.path.join(BASE, "Core", "WaveController.cs"),
    keep_substrings=[
        "웨이브 시작: {waveConfig.WaveID}",
        "개별 웨이브 완료: {wave.WaveID}",
        "웨이브 완료: {currentWave.WaveID}",
    ]
)

# ===============================================================
# StageProgressManager.cs
# ===============================================================
process_file(
    os.path.join(BASE, "Core", "StageProgressManager.cs"),
    keep_substrings=[
        "진행도 시스템 초기화",
        "스테이지 클리어: {stageId}",
        "챕터",
        "자동 해금",
        "진행도 저장",
    ]
)

# ===============================================================
# RewardSystem.cs
# ===============================================================
process_file(
    os.path.join(BASE, "RewardSystem.cs"),
    keep_substrings=[
        "보상 처리 완료",
        "초기화 완료",
    ]
)

# ===============================================================
# 나머지 — 모두 제거
# ===============================================================
no_keep_files = [
    ("Core", "ChapterManager.cs"),
    ("Core", "StageVolumeController.cs"),
    ("Pool", "StagePoolCalculator.cs"),
    ("Pool", "MonsterIdMapper.cs"),
    ("Boulder", "RollingBoulder.cs"),
    ("Boulder", "BoulderSpawner.cs"),
    ("Boulder", "BoulderSpawnerEndTrigger.cs"),
    ("Boulder", "BoulderWaveController.cs"),
    ("Boulder", "BoulderWaveControllerEndTrigger.cs"),
    ("Boulder", "BoulderPathData.cs"),
    ("UI", "StageUI.cs"),
    ("UI", "BossHealthUI.cs"),
    ("UI", "StageTimerUI.cs"),
    ("UI", "KillCountUI.cs"),
    ("Scene", "SpawnPointManager.cs"),
    ("Scene", "StageExitPortal.cs"),
    ("Scene", "SpawnPoint.cs"),
    ("Triggers", "WaveTriggerZone.cs"),
    ("Utils", "StageProgressDebugger.cs"),
    ("Utils", "CsvDataLoader.cs"),
    ("Utils", "StageIdValidator.cs"),
    ("Utils", "StageProgressValidator.cs"),
    ("Utils", "SpawnPointGizmos.cs"),
    ("Data", "WaveConfig.cs"),
    ("Data", "StageConfig.cs"),
    ("Data", "ChapterData.cs"),
    ("Data", "DropTable.cs"),
    ("Data", "DungeonProgress.cs"),
    ("Data", "RewardLevelRangeTable.cs"),
    ("Data", "SpawnGroup.cs"),
    ("Data", "StageDataStructs.cs"),
    ("Data", "StageDataTypes.cs"),
    ("Data", "StageProgress.cs"),
    ("RewardCalculator.cs",),
    ("Editor", "SpawnPointEditor.cs"),
]

print("\n=== 나머지 파일 ===")
for entry in no_keep_files:
    if len(entry) == 1:
        p = os.path.join(BASE, entry[0])
    else:
        p = os.path.join(BASE, *entry)
    process_file(p)

print("\n✅ Stages 폴더 정리 완료")

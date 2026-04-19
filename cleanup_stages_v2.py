# -*- coding: utf-8 -*-
"""
Stages 폴더 Debug.Log 정리 스크립트 v2
- 줄 단위 파서로 if (enableDebugLogs) { } 블록 처리
- {config.ID} 같은 중괄호 포함 문자열도 올바르게 처리
"""
import re, os

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts\Stages"

def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()

def write(path, content):
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

DEBUG_FLAGS = re.compile(r'if\s*\(\s*(enableDebugLogs|showDebugLogs|debugMode)\s*\)')

def process_file(path, keep_substrings=None):
    if not os.path.exists(path):
        print(f"  [SKIP] {path} 없음")
        return
    
    keep_substrings = keep_substrings or []
    content = read(path)
    original = content
    lines = content.split("\n")
    result = []
    i = 0
    n = len(lines)
    
    while i < n:
        line = lines[i]
        stripped = line.strip()
        
        # ── enableDebugLogs/showDebugLogs if 블록 감지 ──
        if DEBUG_FLAGS.search(stripped) and not re.search(r'&&|&&', stripped):
            # 단순 if (enableDebugLogs) 또는 if (showDebugLogs)
            # 다음 줄에 { 있는지 확인
            j = i + 1
            # skip blank lines
            while j < n and lines[j].strip() == '':
                j += 1
            
            if j < n and lines[j].strip() == '{':
                # 블록 시작 찾음 - 닫는 } 찾기
                brace_depth = 1
                block_lines = [lines[j]]  # opening {
                j += 1
                while j < n and brace_depth > 0:
                    block_lines.append(lines[j])
                    brace_depth += lines[j].count('{') - lines[j].count('}')
                    j += 1
                
                block_text = "\n".join(block_lines)
                
                # keep 여부 확인
                kept = False
                indent = re.match(r'^([ \t]*)', line).group(1)
                for sub in keep_substrings:
                    if sub in block_text:
                        # Debug.Log 라인 추출 후 Dbg.Log 변환
                        dbg_lines = []
                        for bl in block_lines:
                            bl_s = bl.strip()
                            if re.search(r'Debug\.Log\(', bl_s) and not re.search(r'Debug\.Log(?:Warning|Error)', bl_s):
                                dbg_line = bl.replace("Debug.Log(", "Dbg.Log(")
                                dbg_lines.append(dbg_line)
                        if dbg_lines:
                            # 멀티라인 로그인 경우 모두 포함
                            result.extend(dbg_lines)
                        kept = True
                        break
                
                if not kept:
                    pass  # 블록 전체 제거 (result에 추가 안 함)
                
                i = j  # 블록 다음으로 건너뜀
                continue
            
            elif j < n and '{' in lines[j]:
                # 같은 줄에 { 있는 경우: if (enableDebugLogs) { Debug.Log...  }
                brace_depth = lines[j].count('{') - lines[j].count('}')
                block_lines = [lines[j]]
                j += 1
                while j < n and brace_depth > 0:
                    block_lines.append(lines[j])
                    brace_depth += lines[j].count('{') - lines[j].count('}')
                    j += 1
                block_text = "\n".join(block_lines)
                
                kept = False
                indent = re.match(r'^([ \t]*)', line).group(1)
                for sub in keep_substrings:
                    if sub in block_text:
                        dbg_lines = []
                        for bl in block_lines:
                            if re.search(r'Debug\.Log\(', bl) and not re.search(r'Debug\.Log(?:Warning|Error)', bl):
                                dbg_lines.append(bl.replace("Debug.Log(", "Dbg.Log("))
                        if dbg_lines:
                            result.extend(dbg_lines)
                        kept = True
                        break
                if not kept:
                    pass  # 제거
                i = j
                continue
            else:
                # { 없음 - 단순 if 라인만 제거
                i += 1
                continue
        
        # ── 고아 { } 블록 제거 (앞 라인이 if였고 블록만 남은 경우) ──
        # 이전 처리에서 if 라인이 제거됐지만 { } 남은 경우
        # (이미 위에서 처리되므로 여기서는 단순 Debug.Log 라인만 처리)
        
        # ── 단독 Debug.Log 라인 제거 ──
        if re.search(r'Debug\.Log\(', line) and not re.search(r'Debug\.Log(?:Warning|Error)', line):
            # 멀티라인인 경우
            if not stripped.endswith(';'):
                i += 1
                while i < n:
                    s = lines[i].strip()
                    i += 1
                    if s.endswith(');') or s.endswith('");'):
                        break
            else:
                i += 1
            continue
        
        result.append(line)
        i += 1
    
    content = "\n".join(result)
    
    # ── 필드 선언 제거 ──
    content = re.sub(
        r'[ \t]*(?:\[SerializeField\]\s*)?(?:public|private|protected)?\s*bool\s+(?:enableDebugLogs|showDebugLogs)\s*=\s*(?:true|false);[ \t]*(?:\/\/[^\n]*)?\n',
        '', content
    )
    content = re.sub(r'[ \t]*\[Header\("[^"]*[Dd]ebug[^"]*"\)\]\s*\n', '', content)
    
    # ── 고아 if (enableDebugLogs) 라인 (블록 없는 것) 제거 ──
    content = re.sub(r'[ \t]*if\s*\(\s*(?:enableDebugLogs|showDebugLogs)\s*\)\s*\n', '', content)
    
    # ── enableDebugLogs && ... 복합 조건 if 라인 (단순 break만 있는 것) ──
    # 이미 별도로 처리됨
    
    # ── #region 디버그 제거 ──
    content = re.sub(r'[ \t]*#region[^\n]*[Dd]ebug[^\n]*\n', '', content)
    content = re.sub(r'[ \t]*#endregion[^\n]*[Dd]ebug[^\n]*\n', '', content)
    
    # ── 연속 빈 줄 정리 (3줄 이상 → 2줄) ──
    content = re.sub(r'\n{4,}', '\n\n\n', content)
    
    if content != original:
        write(path, content)
        print(f"  OK {os.path.basename(path)}")
    else:
        print(f"  -- {os.path.basename(path)} (변경 없음)")

# ===============================================================
# 핵심 파일 — Dbg.Log 유지
# ===============================================================
process_file(
    os.path.join(BASE, "Core", "StageManager.cs"),
    keep_substrings=[
        "초기화 완료",
        "스테이지 시작: {config.StageID}",
        "웨이브 {currentWaveIndex",
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

process_file(
    os.path.join(BASE, "Core", "WaveController.cs"),
    keep_substrings=[
        "웨이브 시작: {waveConfig.WaveID}",
        "개별 웨이브 완료: {wave.WaveID}",
        "웨이브 완료: {currentWave.WaveID}",
    ]
)

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
print("\n=== 나머지 파일 ===")
no_keep = [
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
    ("Editor", "SpawnPointEditor.cs"),
]
for entry in no_keep:
    process_file(os.path.join(BASE, *entry))

# RewardCalculator.cs
process_file(os.path.join(BASE, "RewardCalculator.cs"))

print("\n완료!")

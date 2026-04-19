# -*- coding: utf-8 -*-
"""
UI 폴더 2차 정리: 복합 조건 잔여 처리
"""
import re, os

def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()

def write(path, content):
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

COMPOUND_FLAG = re.compile(r'if\s*\([^)]*(?:enableDebugLogs|showDebugLogs)[^)]*\)')

def remove_debug_compound_blocks(path):
    if not os.path.exists(path):
        return
    content = read(path)
    original = content
    lines = content.split("\n")
    result = []
    i = 0
    n = len(lines)

    while i < n:
        line = lines[i]
        stripped = line.strip()

        # ── 복합 조건 if (... && showDebugLogs) 또는 if (!showDebugLogs) return ──
        if COMPOUND_FLAG.search(stripped):
            # if (!showDebugLogs) return; 패턴 → 라인만 제거
            if re.search(r'if\s*\(\s*!?\s*(?:enableDebugLogs|showDebugLogs)\s*\)\s*return', stripped):
                i += 1
                continue

            # 블록이 있는 경우 탐색
            j = i + 1
            while j < n and lines[j].strip() == '':
                j += 1

            if j < n and lines[j].strip() == '{':
                brace_depth = 1
                j += 1
                while j < n and brace_depth > 0:
                    brace_depth += lines[j].count('{') - lines[j].count('}')
                    j += 1
                i = j
                continue
            else:
                # 블록 없는 단순 if 라인 제거
                i += 1
                continue

        # ── 빈 디버그 메서드 제거 (if (!showDebugLogs) return; 가 처음에 오는 메서드) ──
        result.append(line)
        i += 1

    content = "\n".join(result)

    # showDebugLogs / enableDebugLogs 필드 선언 제거 (복합 조건에서 남은 것)
    content = re.sub(
        r'[ \t]*(?:\[SerializeField\]\s*)?(?:public|private|protected)?\s*bool\s+(?:enableDebugLogs|showDebugLogs)\s*=\s*(?:true|false);[ \t]*(?:\/\/[^\n]*)?\n',
        '', content)
    content = re.sub(r'[ \t]*\[Header\("[^"]*[Dd]ebug[^"]*"\)\]\s*\n', '', content)
    content = re.sub(r'\n{4,}', '\n\n\n', content)

    if content != original:
        write(path, content)
        print(f"  OK {os.path.basename(path)}")
    else:
        print(f"  -- {os.path.basename(path)}")

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts\UI"

files = [
    r"Inventory\LobbyInventoryUI.cs",
    r"MenuUI\BackgroundOverlayHandler.cs",
    r"MenuUI\DungeonSelectPanelController.cs",
    r"MenuUI\TutorialStepController.cs",
    r"Shop\ShopInventoryUI.cs",
    r"Shop\ShopUIController.cs",
    r"Skills\SkillTabController.cs",
    r"Workshop\BeforeAfterComparisonUI.cs",
    r"Workshop\EnhancementUI.cs",
    r"Workshop\WorkshopInventoryUI.cs",
]

for f in files:
    remove_debug_compound_blocks(os.path.join(BASE, f))

print("\n완료!")

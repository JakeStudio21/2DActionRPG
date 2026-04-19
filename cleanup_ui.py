# -*- coding: utf-8 -*-
"""
UI 폴더 Debug.Log 전체 정리 스크립트
"""
import re, os, glob

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts\UI"

def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()

def write(path, content):
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

DEBUG_FLAGS = re.compile(r'if\s*\(\s*(enableDebugLogs|showDebugLogs|debugMode)(?:\s*&&[^)]+)?\s*\)')

def process_file(path, keep_substrings=None):
    if not os.path.exists(path):
        return False
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

        # ── if (enableDebugLogs/showDebugLogs) 블록 처리 ──
        if DEBUG_FLAGS.search(stripped):
            j = i + 1
            while j < n and lines[j].strip() == '':
                j += 1
            if j < n and lines[j].strip() == '{':
                brace_depth = 1
                block_lines = [lines[j]]
                j += 1
                while j < n and brace_depth > 0:
                    block_lines.append(lines[j])
                    brace_depth += lines[j].count('{') - lines[j].count('}')
                    j += 1
                block_text = "\n".join(block_lines)
                kept = False
                for sub in keep_substrings:
                    if sub in block_text:
                        for bl in block_lines:
                            if re.search(r'Debug\.Log\(', bl) and not re.search(r'Debug\.Log(?:Warning|Error)', bl):
                                result.append(bl.replace("Debug.Log(", "Dbg.Log("))
                        kept = True
                        break
                i = j
                continue
            else:
                i += 1
                continue

        # ── 단독 Debug.Log 라인 처리 ──
        if re.search(r'Debug\.Log\(', line) and not re.search(r'Debug\.Log(?:Warning|Error)', line):
            kept = False
            for sub in keep_substrings:
                if sub in line:
                    result.append(line.replace("Debug.Log(", "Dbg.Log("))
                    kept = True
                    break
            if not kept:
                if not stripped.endswith(';'):
                    i += 1
                    while i < n:
                        s = lines[i].strip()
                        i += 1
                        if s.endswith(');') or s.endswith('");'):
                            break
                else:
                    i += 1
            else:
                i += 1
            continue

        result.append(line)
        i += 1

    content = "\n".join(result)

    # 필드 선언 / 고아 속성 제거
    content = re.sub(
        r'[ \t]*(?:\[SerializeField\]\s*)?(?:public|private|protected)?\s*bool\s+(?:enableDebugLogs|showDebugLogs)\s*=\s*(?:true|false);[ \t]*(?:\/\/[^\n]*)?\n',
        '', content)
    content = re.sub(r'[ \t]*\[Header\("[^"]*[Dd]ebug[^"]*"\)\]\s*\n', '', content)
    content = re.sub(r'[ \t]*if\s*\(\s*(?:enableDebugLogs|showDebugLogs)\s*\)\s*\n', '', content)
    content = re.sub(r'[ \t]*#region[^\n]*[Dd]ebug[^\n]*\n', '', content)
    content = re.sub(r'[ \t]*#endregion[^\n]*[Dd]ebug[^\n]*\n', '', content)
    content = re.sub(r'\n{4,}', '\n\n\n', content)

    if content != original:
        write(path, content)
        return True
    return False

# ================================================================
# 선별 유지 파일 (핵심 흐름 로그 Dbg.Log 유지)
# ================================================================
selective = {
    # MenuUI
    r"MenuUI\LobbyInitializer.cs":              ["로비 초기화 완료"],
    r"MenuUI\LoadingSceneController.cs":         ["다음 로드할 씬:", "초기화 완료"],
    r"MenuUI\LobbyUIController.cs":             ["게임 시작:", "던전 입장"],
    r"MenuUI\LobbyPanelManager.cs":             [],
    r"MenuUI\CharacterCreationController.cs":   ["캐릭터 생성 완료", "캐릭터 생성:"],
    r"MenuUI\LobbySlotSystem\CharacterSlotController.cs": ["슬롯 선택", "슬롯 로드 완료"],
    r"MenuUI\LobbySlotSystem\CharacterSlotItem.cs": [],
    r"MenuUI\StageSelectPanelController.cs":    ["스테이지 선택:"],
    r"MenuUI\StageSelectUIController.cs":       ["스테이지 진입"],
    r"MenuUI\DungeonSelectPanelController.cs":  ["던전 입장", "던전 선택:"],
    r"MenuUI\IntroSceneController.cs":          [],
    r"MenuUI\TutorialSceneController.cs":       ["튜토리얼 완료", "씬 시작"],
    r"MenuUI\TutorialStepController.cs":        ["튜토리얼 단계 완료", "튜토리얼 완료"],
    r"MenuUI\ConfirmationPopup.cs":             [],
    r"MenuUI\ChapterMapUI.cs":                  ["챕터 선택:"],
    r"MenuUI\CharacterCreation\ClassSelectionView.cs": [],
    r"MenuUI\CharacterCreation\NameInputView.cs": [],
    # Inventory
    r"Inventory\LobbyInventoryUI.cs":           ["인벤토리 초기화 완료"],
    r"Inventory\LobbyEquippedItemsUI.cs":       ["장착 아이템 갱신 완료"],
    r"Inventory\EquippedItemsUI.cs":            ["장착 갱신 완료"],
    r"Inventory\ActiveInventory.cs":            ["아이템 추가:", "아이템 제거:"],
    r"Inventory\InventoryController.cs":        ["초기화 완료"],
    r"Inventory\IntegratedInventoryController.cs": ["탭 전환:"],
    # PlayerUI
    r"PlayerUI\PlayerUIController.cs":          ["UI 초기화 완료"],
    r"PlayerUI\CharacterInfoUI.cs":             ["장비 정보 로드 완료"],
    # Workshop
    r"Workshop\FusionUI.cs":                    ["합성 결과:"],
    r"Workshop\EnhancementUI.cs":               ["강화 완료", "아이템 선택:"],
    r"Workshop\DismantleUI.cs":                 ["분해 완료"],
    r"Workshop\WorkshopUI.cs":                  ["워크샵 초기화 완료"],
    r"Workshop\WorkshopInventoryUI.cs":         ["선택 완료"],
    # Shop
    r"Shop\ShopUIController.cs":               ["구매 완료", "판매 완료"],
    r"Shop\ShopBuyPanel.cs":                   ["구매 확인"],
    r"Shop\ShopInventoryUI.cs":                ["초기화 완료"],
    # Skills
    r"Skills\SkillTabController.cs":           ["스킬 장착 완료"],
    r"Skills\SkillBookPanelUI.cs":             ["스킬 목록 로드 완료"],
    r"Skills\SkillListItemUI.cs":              [],
    # SpiritBlessing
    r"SpiritBlessing\SpiritBlessingTabController.cs": ["정령 축복 적용 완료"],
    r"SpiritBlessing\SpiritBlessingItemUI.cs": [],
    # System
    r"System\ResultPopupController.cs":        ["결과 팝업 표시"],
    r"System\PauseMenuController.cs":          [],
    # Popups
    r"Popups\ItemDetailPopup.cs":              [],
    r"Popups\ContentEntryWarningPopup.cs":     [],
    # Manager
    r"Manager\NotificationManager.cs":         ["알림:"],
    # Common
    r"Common\UIButtonClickEffect.cs":          [],
    # PlayerUI
    r"PlayerUI\PlayerStatsUI.cs":              [],
    r"PlayerUI\HealthUI.cs":                   [],
    # Debug
    r"Debug\V2InventoryDebugUI.cs":            [],
    r"MenuUI\StageProgressCheatTool.cs":       [],
}

changed = []
unchanged = []

for rel_path, keeps in selective.items():
    full_path = os.path.join(BASE, rel_path)
    if process_file(full_path, keeps):
        changed.append(os.path.basename(rel_path))
    else:
        if os.path.exists(full_path):
            unchanged.append(os.path.basename(rel_path))

# ================================================================
# 나머지 전체 파일 — glob으로 모두 처리
# ================================================================
all_cs = glob.glob(os.path.join(BASE, "**", "*.cs"), recursive=True)
processed_paths = {os.path.join(BASE, p.replace("\\", os.sep)).lower() for p in selective.keys()}

for cs_path in all_cs:
    if cs_path.lower() in processed_paths:
        continue
    if process_file(cs_path, []):
        changed.append(os.path.basename(cs_path))

print(f"\n변경: {len(changed)}개")
print(f"변경없음: {len(unchanged)}개")
for f in sorted(changed):
    print(f"  OK {f}")
print("\nUI 폴더 정리 완료!")

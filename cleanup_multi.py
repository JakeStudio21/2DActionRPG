# -*- coding: utf-8 -*-
"""Weapon/Shop/Debug/Tutorial/Rendering/Utils/Environment/Misc Debug.Log cleanup"""
import re, os

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts"

def read(p):
    with open(p, encoding="utf-8-sig") as f: return f.read()

def write(p, c):
    with open(p, "w", encoding="utf-8") as f: f.write(c)

def process_file(path, keep_substrings=None, convert_all_to_dbg=False):
    try:
        content = read(path)
    except Exception as e:
        print(f"  ERR {os.path.basename(path)}: {e}")
        return

    original = content
    lines = content.split("\n")
    result = []
    i = 0
    n = len(lines)

    while i < n:
        line = lines[i]
        stripped = line.strip()

        # Debug flag field removal (bool showDebugLogs / enableDebugLogs / showLogs)
        if re.search(r'\bbool\s+(?:showDebugLogs|enableDebugLogs|showLogs)\b', stripped):
            # Remove preceding [Header] / [Tooltip] lines
            while result and re.match(r'\s*\[(?:Header|Tooltip)\(', result[-1]):
                result.pop()
            i += 1
            continue

        # if (showDebugLogs) Debug.Log(...) on same line — remove
        if re.match(r'if\s*\(\s*(?:showDebugLogs|enableDebugLogs|showLogs)\s*\)\s*(?:UnityEngine\.)?Debug\.Log', stripped):
            i += 1
            continue

        # if (showDebugLogs) { ... } block — remove block, but extract keep/convert logs
        if re.match(r'if\s*\(\s*(?:showDebugLogs|enableDebugLogs|showLogs)\s*\)', stripped) \
                and '&&' not in stripped and '||' not in stripped:
            # { 는 같은 줄 또는 바로 다음 줄에만 허용 (멀리 탐색 금지)
            has_brace_same = '{' in stripped
            next_stripped = lines[i + 1].strip() if i + 1 < n else ''
            has_brace_next = (next_stripped == '{' or
                              (next_stripped.startswith('{') and len(next_stripped) <= 2))
            if has_brace_same or has_brace_next:
                # 블록 형태
                j = i if has_brace_same else i + 1
                block_inner = []
                depth = 0
                k = j
                while k < n:
                    depth += lines[k].count('{') - lines[k].count('}')
                    if depth > 0:
                        block_inner.append(lines[k])
                    k += 1
                    if depth <= 0:
                        break
                for bl in block_inner:
                    if ('Debug.Log(' in bl or 'UnityEngine.Debug.Log(' in bl) \
                            and 'Debug.LogError' not in bl and 'Debug.LogWarning' not in bl:
                        if convert_all_to_dbg:
                            result.append(re.sub(r'(?:UnityEngine\.)?Debug\.Log\(', 'Dbg.Log(', bl))
                        elif keep_substrings and any(sub in bl for sub in keep_substrings):
                            result.append(re.sub(r'(?:UnityEngine\.)?Debug\.Log\(', 'Dbg.Log(', bl))
                i = k
            else:
                # 단일 바디 라인: if (flag)\n    statement;
                i += 2  # if 라인 + body 라인만 건너뜀
            continue

        # Debug.Log detection (skip LogError / LogWarning / Dbg.Log wrapper in Dbg.cs)
        if ('Debug.Log(' in line or 'UnityEngine.Debug.Log(' in line) \
                and 'Debug.LogError' not in line and 'Debug.LogWarning' not in line:

            # Collect multiline statement by tracking paren balance
            stmt_lines = [line]
            balance = line.count('(') - line.count(')')
            j = i + 1
            while balance > 0 and j < n:
                stmt_lines.append(lines[j])
                balance += lines[j].count('(') - lines[j].count(')')
                j += 1

            full_stmt = ' '.join(l.strip() for l in stmt_lines)

            if convert_all_to_dbg:
                first = re.sub(r'(?:UnityEngine\.)?Debug\.Log\(', 'Dbg.Log(', stmt_lines[0])
                result.append(first)
                for extra in stmt_lines[1:]:
                    result.append(extra)
            elif keep_substrings and any(sub in full_stmt for sub in keep_substrings):
                first = re.sub(r'(?:UnityEngine\.)?Debug\.Log\(', 'Dbg.Log(', stmt_lines[0])
                result.append(first)
                for extra in stmt_lines[1:]:
                    result.append(extra)
            # else: remove

            i = j
            continue

        result.append(line)
        i += 1

    content = "\n".join(result)
    content = re.sub(r'\n{3,}', '\n\n', content)

    if content != original:
        write(path, content)
        print(f"  OK {os.path.basename(path)}")
    else:
        print(f"  -- {os.path.basename(path)}")


# ── 파일 설정 ──────────────────────────────────────────────────────────────
# keep: 이 문자열을 포함한 Debug.Log → Dbg.Log로 변환
# convert_all: True이면 모든 Debug.Log → Dbg.Log
FILE_CONFIGS = {
    # Weapon ─ 전량 제거
    "Weapon/Bow.cs":        {},
    "Weapon/Projectile.cs": {},
    "Weapon/Sword.cs":      {},

    # Shop ─ 구매/판매/초기화 완료만 유지
    "Shop/ShopController.cs":      {"keep": ["구매 성공", "판매 성공", "초기화 완료"]},
    "Shop/ShopInventoryManager.cs": {"keep": ["초기화 완료"]},
    "Shop/EquipmentPriceProvider.cs": {},
    "Shop/ShopInventoryData.cs":   {},
    "Shop/ShopItemPool.cs":        {},

    # Debug ─ 치트 도구이므로 핵심 동작 로그 Dbg.Log 유지
    "Debug/CheatService.cs":       {"convert_all": True},
    "Debug/DebugCheatManager.cs":  {"keep": ["초기화 완료", "결과 출력"]},
    "Debug/DebugCheatToggler.cs":  {"keep": ["치트 패널", "버튼 클릭", "버튼 숨김"]},
    "Debug/DebugCheatUI.cs":       {"keep": ["결과"]},
    "Debug/MobilePlayerDebugPanel.cs": {"convert_all": True},

    # Tutorial ─ 초기화 완료 / 완료 / 로비 이동만 유지
    "Tutorial/TutorialManager.cs":      {"keep": ["초기화 완료", "Tutorial 완료", "복귀 중"]},
    "Tutorial/TutorialPlayerSpawner.cs": {"keep": ["스폰 완료"]},
    "Tutorial/TutorialSpotlight.cs":    {},

    # Rendering ─ 전량 제거
    "Rendering/AlphaFader.cs":                    {},
    "Rendering/AnimationRenderer8Direction.cs":   {},
    "Rendering/AnimationRenderer8Direction_Rotate.cs": {},
    "Rendering/EightCams_8Dir_BatchCapture.cs":   {},
    "Rendering/FootPositionSorter.cs":            {},
    "Rendering/OcclusionDetector.cs":             {},

    # Utils ─ Dbg.cs 제외, SceneBGMStarter 완료 로그만 유지
    "Utils/IsometricHelper.cs":  {},
    "Utils/IsometricSettings.cs": {},
    "Utils/IsometricSorting.cs": {},
    "Utils/SceneBGMStarter.cs":  {"keep": ["완료"]},
    "Utils/TilemapLayerSetup.cs": {},
    "Utils/WallLayerSetup.cs":   {},

    # Environment ─ 전량 제거
    "Environment/GrassAutoSpawner.cs": {},

    # Misc ─ EconomyManager 골드 초기화만 유지
    "Misc/ColliderGizmosDrawer.cs": {},
    "Misc/EconomyManager.cs":       {"keep": ["UI 초기화"]},
    "Misc/Flash.cs":                {},
    "Misc/Knockback.cs":            {},
    "Misc/MiniBossGate.cs":         {},
    "Misc/ParticleAutoReturn.cs":   {},
    "Misc/ScreenShakeManager.cs":   {},
}

print("=== Multi-folder Debug.Log Cleanup ===")
for rel_path, config in FILE_CONFIGS.items():
    full_path = os.path.join(BASE, rel_path.replace('/', os.sep))
    if not os.path.exists(full_path):
        print(f"  SKIP {rel_path} (not found)")
        continue
    keep = config.get("keep", None)
    cvt_all = config.get("convert_all", False)
    process_file(full_path, keep_substrings=keep, convert_all_to_dbg=cvt_all)

print("\n완료!")

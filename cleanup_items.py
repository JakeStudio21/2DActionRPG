# -*- coding: utf-8 -*-
"""
Items 폴더 Debug.Log 정리 스크립트
"""
import re, os

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts\Items"

def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()

def write(path, content):
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

DEBUG_FLAGS = re.compile(r'if\s*\(\s*(enableDebugLogs|showDebugLogs)(?:\s*&&[^)]+)?\s*\)')

def process_file(path, keep_substrings=None):
    if not os.path.exists(path):
        print(f"  [SKIP] {path}")
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

    # 필드 선언 제거
    content = re.sub(
        r'[ \t]*(?:\[SerializeField\]\s*)?(?:public|private|protected)?\s*bool\s+(?:enableDebugLogs|showDebugLogs)\s*=\s*(?:true|false);[ \t]*(?:\/\/[^\n]*)?\n',
        '', content
    )
    content = re.sub(r'[ \t]*\[Header\("[^"]*[Dd]ebug[^"]*"\)\]\s*\n', '', content)
    content = re.sub(r'[ \t]*if\s*\(\s*(?:enableDebugLogs|showDebugLogs)\s*\)\s*\n', '', content)
    content = re.sub(r'\n{4,}', '\n\n\n', content)

    if content != original:
        write(path, content)
        print(f"  OK {os.path.basename(path)}")
    else:
        print(f"  -- {os.path.basename(path)} (변경없음)")

# ================================================================
# EquipmentPickup.cs — 장비 획득 완료 유지
# ================================================================
process_file(
    os.path.join(BASE, "EquipmentPickup.cs"),
    keep_substrings=["획득 (V2):"]
)

# ================================================================
# MaterialPickup.cs — 재료 획득 완료 유지
# ================================================================
process_file(
    os.path.join(BASE, "MaterialPickup.cs"),
    keep_substrings=["획득 후 캐릭터 적용:"]
)

# ================================================================
# CurrencyPickup.cs — 골드/체력 획득 유지
# ================================================================
process_file(
    os.path.join(BASE, "CurrencyPickup.cs"),
    keep_substrings=["골드 획득:", "체력 회복:"]
)

# ================================================================
# GoldItemData.cs — 골드 획득 유지
# ================================================================
process_file(
    os.path.join(BASE, "Data", "GoldItemData.cs"),
    keep_substrings=["골드 획득:"]
)

# ================================================================
# 전체 제거 파일
# ================================================================
print("\n=== 전체 제거 ===")
remove_all = [
    ("Pickup.cs",),
    ("PickUpSpawner.cs",),
    ("EquipmentData.cs",),
    ("DropResolver.cs",),
    ("EquipmentDataAdapter.cs",),
]
for entry in remove_all:
    process_file(os.path.join(BASE, *entry))

print("\n완료!")

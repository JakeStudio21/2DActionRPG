# -*- coding: utf-8 -*-
"""
Combat 폴더 Debug.Log 정리 스크립트
"""
import re, os

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts\Combat"

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

        # ── if (enableDebugLogs...) 블록 처리 ──
        if DEBUG_FLAGS.search(stripped):
            j = i + 1
            while j < n and lines[j].strip() == '':
                j += 1

            if j < n and lines[j].strip() == '{':
                # 닫는 } 찾기
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
                # { 없는 단순 if — 라인만 제거
                i += 1
                continue

        # ── 단독 Debug.Log 라인 제거 ──
        if re.search(r'Debug\.Log\(', line) and not re.search(r'Debug\.Log(?:Warning|Error)', line):
            # keep 여부 확인
            kept = False
            for sub in keep_substrings:
                if sub in line:
                    result.append(line.replace("Debug.Log(", "Dbg.Log("))
                    kept = True
                    break
            if not kept:
                if not stripped.endswith(';'):
                    # 멀티라인
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

    # ── 필드 선언 제거 ──
    content = re.sub(
        r'[ \t]*(?:\[SerializeField\]\s*)?(?:public|private|protected)?\s*bool\s+(?:enableDebugLogs|showDebugLogs)\s*=\s*(?:true|false);[ \t]*(?:\/\/[^\n]*)?\n',
        '', content
    )
    content = re.sub(r'[ \t]*\[Header\("[^"]*[Dd]ebug[^"]*"\)\]\s*\n', '', content)
    content = re.sub(r'[ \t]*if\s*\(\s*(?:enableDebugLogs|showDebugLogs)\s*\)\s*\n', '', content)
    content = re.sub(r'[ \t]*#region[^\n]*[Dd]ebug[^\n]*\n', '', content)
    content = re.sub(r'[ \t]*#endregion[^\n]*[Dd]ebug[^\n]*\n', '', content)
    content = re.sub(r'\n{4,}', '\n\n\n', content)

    if content != original:
        write(path, content)
        print(f"  OK {os.path.basename(path)}")
    else:
        print(f"  -- {os.path.basename(path)} (변경없음)")

def remove_method_block(path, method_signature):
    """특정 메서드 블록 전체 제거 (summary + 속성 + 메서드)"""
    if not os.path.exists(path):
        return
    content = read(path)
    # summary 블록 포함한 메서드 제거
    pattern = re.compile(
        r'[ \t]*/// <summary>.*?/// </summary>\s*\n'
        r'(?:[ \t]*\[[^\]]+\]\s*\n)*'
        r'[ \t]*' + re.escape(method_signature) + r'[^\n]*\n'
        r'[ \t]*\{[^}]*(?:\{[^}]*\}[^}]*)*\}[ \t]*\n?',
        re.DOTALL
    )
    new_content = pattern.sub('', content)
    if new_content != content:
        write(path, new_content)

# ================================================================
# EquipmentManager.cs — 장착/해제 완료 Dbg.Log 유지
# ================================================================
process_file(
    os.path.join(BASE, "Equipment", "EquipmentManager.cs"),
    keep_substrings=[
        "착용 완료 (슬롯:",
        "해제 완료 (슬롯:",
    ]
)

# ================================================================
# DynamicEquipmentGenerator.cs — 생성 완료 요약 1줄 유지
# ================================================================
process_file(
    os.path.join(BASE, "Equipment", "DynamicEquipmentGenerator.cs"),
    keep_substrings=[
        "생성 완료!",
    ]
)

# ================================================================
# ConditionalModifierDatabase.cs — 초기화 완료 유지, PrintDebugInfo 제거
# ================================================================
process_file(
    os.path.join(BASE, "Conditional", "ConditionalModifierDatabase.cs"),
    keep_substrings=[
        "초기화 완료",
    ]
)

# ================================================================
# ItemDatabase.cs — 초기화 완료 유지, DebugCacheInfo 제거
# ================================================================
process_file(
    os.path.join(BASE, "Database", "ItemDatabase.cs"),
    keep_substrings=[
        "초기화 완료",
    ]
)

# ================================================================
# StatDefinitions.cs — 로드 완료 유지
# ================================================================
process_file(
    os.path.join(BASE, "Stats", "StatDefinitions.cs"),
    keep_substrings=[
        "로드 완료",
    ]
)

# ================================================================
# 전체 제거 파일
# ================================================================
print("\n=== 전체 제거 ===")
remove_all = [
    ("", "CombatFormula.cs"),
    ("Equipment", "EquipmentInstance.cs"),
    ("Equipment", "EquipmentInstanceConverter.cs"),
    ("StatusEffect", "BaseStatusEffect.cs"),
    ("StatusEffect", "BindEffect.cs"),
    ("StatusEffect", "BurnEffect.cs"),
    ("StatusEffect", "PoisonEffect.cs"),
    ("StatusEffect", "SlowEffect.cs"),
]
for folder, fname in remove_all:
    p = os.path.join(BASE, folder, fname) if folder else os.path.join(BASE, fname)
    process_file(p)

print("\n완료!")

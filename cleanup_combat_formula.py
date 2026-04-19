# -*- coding: utf-8 -*-
"""
CombatFormula.cs EnableDetailedLogs 블록 및 프로퍼티 제거
"""
import re, os

PATH = r"d:\Unity\2DActionRPG\Assets\Scripts\Combat\CombatFormula.cs"

def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()

def write(path, content):
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

FLAG_RE = re.compile(r'if\s*\(\s*(?:EnableDetailedLogs|enableDetailedLogs)(?:\s*&&[^)]+)?\s*\)')

content = read(PATH)
lines = content.split("\n")
result = []
i = 0
n = len(lines)

while i < n:
    line = lines[i]
    stripped = line.strip()

    # ── if (EnableDetailedLogs...) 블록 전체 제거 ──
    if FLAG_RE.search(stripped):
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

    # ── 단독 Debug.Log 라인 제거 (혹시 남은 것) ──
    if re.search(r'Debug\.Log\(', line) and not re.search(r'Debug\.Log(?:Warning|Error)', line):
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

# EnableDetailedLogs 프로퍼티 블록 제거 (summary + property)
content = re.sub(
    r'[ \t]*/// <summary>\s*\n[ \t]*/// 상세 로그 활성화 여부\s*\n[ \t]*/// </summary>\s*\n'
    r'[ \t]*private static bool EnableDetailedLogs\s*\n[ \t]*\{[^}]*(?:\{[^}]*\}[^}]*)*\}\s*\n?',
    '', content, flags=re.DOTALL
)

# enableDetailedLogs Config 참조 필드 제거
content = re.sub(r'[ \t]*(?:public|private)?\s*bool\s+enableDetailedLogs\s*=\s*(?:true|false);[ \t]*\n', '', content)

# _forceDetailedLog 필드 제거
content = re.sub(r'[ \t]*(?:public|private|static)?\s*(?:static\s+)?bool\s+_forceDetailedLog\s*=\s*(?:true|false);[ \t]*(?:\/\/[^\n]*)?\n', '', content)

# 고아 if (조건만 남은 경우) 제거 — 바디 없는 if
content = re.sub(r'[ \t]*if\s*\([^)]+\)\s*\n([ \t]*\n)*[ \t]*\}', 
    lambda m: '' if '{' not in m.group(0).split('if')[0] else m.group(0), 
    content)

# 빈 블록 정리
content = re.sub(r'\n{4,}', '\n\n\n', content)

write(PATH, content)
print("CombatFormula.cs 완료")

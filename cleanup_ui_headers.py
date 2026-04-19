# -*- coding: utf-8 -*-
"""고아 [Header] 속성 제거"""
import re, os

def read(p):
    with open(p, encoding="utf-8-sig") as f: return f.read()

def write(p, c):
    with open(p, "w", encoding="utf-8") as f: f.write(c)

def remove_orphan_headers(path):
    content = read(path)
    original = content
    lines = content.split("\n")
    result = []
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        # [Header(...)] 감지
        if re.match(r'[ \t]*\[Header\(', line):
            # 다음 비어있지 않은 줄 확인
            j = i + 1
            while j < n and lines[j].strip() == '':
                j += 1
            # 다음 줄이 필드 선언이 아니면 (SerializeField, 타입 선언 없음) → 고아
            next_line = lines[j].strip() if j < n else ''
            is_field = (
                re.match(r'\[SerializeField\]', next_line) or
                re.match(r'(public|private|protected)\s+(static\s+)?(readonly\s+)?(int|float|bool|string|Vector|Color|Image|Text|Button|Transform|GameObject|Sprite|Animator|Audio|Slider|Scroll|TMP|RectTransform|Canvas|Camera)', next_line)
            )
            if not is_field:
                i += 1  # 고아 Header 라인 건너뜀
                continue
        result.append(line)
        i += 1
    content = "\n".join(result)
    if content != original:
        write(path, content)
        print(f"  OK {os.path.basename(path)}")
    else:
        print(f"  -- {os.path.basename(path)}")

BASE = r"d:\Unity\2DActionRPG\Assets\Scripts\UI"
files = [
    r"Shop\ShopItemSlot.cs",
    r"Skills\SkillDetailPanel.cs",
    r"Skills\SkillEquipSlotUI.cs",
    r"Skills\SkillListItemUI.cs",
    r"System\ItemSlotUI.cs",
    r"System\ResultPopupController.cs",
    r"PlayerUI\DashButtonController.cs",
    r"Inventory\InventoryController.cs",
    r"Inventory\LobbyEquippedItemsUI.cs",
]
for f in files:
    remove_orphan_headers(os.path.join(BASE, f))
print("완료!")

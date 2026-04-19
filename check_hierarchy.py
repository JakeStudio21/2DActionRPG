import io, sys, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

with open('Assets/Scenes/Lobby.unity', 'r', encoding='utf-8', errors='replace') as f:
    content = f.read()

def get_go_name(fileid):
    pattern = r'--- !u!1 &' + fileid + r'\b'
    m = re.search(pattern, content)
    if m:
        chunk = content[m.start():m.start()+300]
        nm = re.search(r'm_Name:\s*(.+)', chunk)
        if nm: return nm.group(1).strip()
    return '?'

def get_comp_go(comp_fileid):
    pattern = r'--- !u!114 &' + comp_fileid + r'\b'
    m = re.search(pattern, content)
    if m:
        chunk = content[m.start():m.start()+400]
        go_m = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)\}', chunk)
        if go_m: return go_m.group(1)
    return None

def get_parent_chain(go_id, depth=6):
    chain = [get_go_name(go_id)]
    cur_go = go_id
    for _ in range(depth):
        # Find RectTransform or Transform for this GO
        t_found = None
        for t_type in ['4', '224']:
            pattern = r'--- !u!' + t_type + r' &(\d+)\n'
            for m in re.finditer(pattern, content):
                chunk = content[m.start():m.start()+400]
                if 'm_GameObject: {fileID: ' + cur_go + '}' in chunk:
                    t_found = (m.group(1), chunk)
                    break
            if t_found:
                break
        if not t_found:
            break
        _, t_chunk = t_found
        p_m = re.search(r'm_Father:\s*\{fileID:\s*(\d+)\}', t_chunk)
        if not p_m or p_m.group(1) == '0':
            break
        p_trans_id = p_m.group(1)
        # Find GO of parent transform
        for t_type in ['4', '224']:
            pat2 = r'--- !u!' + t_type + r' &' + p_trans_id + r'\b'
            m2 = re.search(pat2, content)
            if m2:
                chunk2 = content[m2.start():m2.start()+400]
                go_m2 = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)\}', chunk2)
                if go_m2:
                    cur_go = go_m2.group(1)
                    chain.append(get_go_name(cur_go))
                    break
    return chain

for comp_id, label in [('1219714666', 'LobbyInventoryUI refs'), ('1558184058', 'CharacterInfoUI refs')]:
    go_id = get_comp_go(comp_id)
    if go_id:
        chain = get_parent_chain(go_id)
        print(f'{label}: {" > ".join(reversed(chain))}')
    else:
        print(f'{label}: GO not found')

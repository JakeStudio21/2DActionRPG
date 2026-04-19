import re, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

with open('Assets/Scenes/Lobby.unity', 'r', encoding='utf-8', errors='replace') as f:
    content = f.read()

for comp_id in ['1219714666', '1558184058']:
    idx = content.find('--- !u!114 &' + comp_id)
    if idx < 0:
        print(comp_id + ': not found')
        continue
    chunk = content[idx:idx+400]
    go_m = re.search(r'm_GameObject: \{fileID: (\d+)\}', chunk)
    if not go_m:
        print(comp_id + ': no GO')
        continue
    go_id = go_m.group(1)
    go_idx = content.find('--- !u!1 &' + go_id)
    nm = re.search(r'm_Name: (.+)', content[go_idx:go_idx+300]) if go_idx >= 0 else None
    go_name = nm.group(1).strip() if nm else 'unknown'
    
    # Walk up hierarchy to find parent panel names
    parents = [go_name]
    cur_go = go_id
    for _ in range(8):
        # find transform for cur_go
        found_trans = None
        for ut in ['4', '224']:
            search = '--- !u!' + ut + ' &'
            pos = 0
            while True:
                p = content.find(search, pos)
                if p < 0: break
                end = content.find('\n--- !u!', p+1)
                seg = content[p:end if end > 0 else p+600]
                if 'm_GameObject: {fileID: ' + cur_go + '}' in seg:
                    found_trans = seg
                    break
                pos = p + 1
            if found_trans: break
        if not found_trans: break
        f_m = re.search(r'm_Father: \{fileID: (\d+)\}', found_trans)
        if not f_m or f_m.group(1) == '0': break
        p_trans_id = f_m.group(1)
        # find GO of parent transform
        for ut in ['4', '224']:
            p_idx = content.find('--- !u!' + ut + ' &' + p_trans_id)
            if p_idx >= 0:
                seg2 = content[p_idx:p_idx+400]
                go_m2 = re.search(r'm_GameObject: \{fileID: (\d+)\}', seg2)
                if go_m2:
                    cur_go = go_m2.group(1)
                    go_idx2 = content.find('--- !u!1 &' + cur_go)
                    nm2 = re.search(r'm_Name: (.+)', content[go_idx2:go_idx2+200]) if go_idx2 >= 0 else None
                    parents.append(nm2.group(1).strip() if nm2 else '?')
                    break
    
    print(comp_id + ': ' + ' > '.join(reversed(parents)))

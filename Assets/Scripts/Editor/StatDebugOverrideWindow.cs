using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// 📊 스탯 임시 오버라이드 & 전투 로그 디버그 도구
///
/// ▸ Assets/Scripts/Editor/ 하위이므로 빌드에 절대 포함되지 않습니다.
/// ▸ 오버라이드 값은 PlayerRuntimeStats._debugBonus 필드에만 저장되며,
///   장비/스킬/SelectedPlayerData 어디에도 기록되지 않습니다.
/// ▸ 장비 변경 등으로 RecalculateStats()가 호출되어도 보너스는 자동 재적용됩니다.
/// </summary>
public class StatDebugOverrideWindow : EditorWindow
{
    // ─── 섹션 Foldout 상태 ────────────────────────────────────────────
    private bool _showCurrentStats = true;
    private bool _showOverride     = true;
    private bool _showCombatLog    = true;
    
    // ─── 스크롤 ───────────────────────────────────────────────────────
    private Vector2 _scroll;
    
    // ─── 오버라이드 델타 입력값 (에디터 창 내부 상태) ─────────────────
    // 공격 관련
    private float _dAtkFlat     = 0f;   // ATK_FLAT
    private float _dAtkPercent  = 0f;   // ATK_PERCENT (곱연산, 0.5 = +50%)
    // 기본 전투 스탯
    private float _dMaxHp       = 0f;   // HP_FLAT
    private float _dDefense     = 0f;   // DEF_FLAT
    private float _dMoveSpeed   = 0f;   // MOVE_SPEED
    private float _dAtkSpeed    = 0f;   // ASPD
    private float _dCritChance  = 0f;   // CRIT_RATE  (0.1 = 10%)
    private float _dCritDmg     = 0f;   // CRIT_DMG   (0.5 = +50%)
    private float _dHealMult    = 0f;   // HEAL_MULT
    // 특수 스탯 10종
    private float _dSkillDmg    = 0f;   // SKILL_DMG_PERCENT
    private float _dCdr         = 0f;   // COOLDOWN_REDUCTION (cap 0.5)
    private float _dDmgRed      = 0f;   // DAMAGE_REDUCTION_PERCENT (cap 0.8)
    private float _dHpRegen     = 0f;   // HP_REGEN (HP/sec)
    private float _dLifeSteal   = 0f;   // LIFESTEAL
    private float _dArmorPen    = 0f;   // ARMOR_PENETRATION
    private float _dDodge       = 0f;   // DODGE_CHANCE
    private float _dBlock       = 0f;   // BLOCK_CHANCE
    private float _dExpGain     = 0f;   // EXP_GAIN_PERCENT
    private float _dStatusResist = 0f;  // STATUS_RESIST_ALL
    private float _dPierceRetention = 0f; // PIERCE_DAMAGE_RETENTION
    
    // ─── GUI 스타일 캐시 ──────────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _valueStyle;
    private GUIStyle _warnStyle;
    private bool _stylesInitialized;
    
    // ─── 컬럼 너비 ────────────────────────────────────────────────────
    private const float COL_LABEL   = 210f;
    private const float COL_CURRENT = 85f;
    private const float COL_DELTA   = 85f;
    
    [MenuItem("Tools/Player/📊 스탯 디버그 도구 (임시, 저장 안됨)")]
    public static void ShowWindow()
    {
        var win = GetWindow<StatDebugOverrideWindow>("📊 스탯 디버그");
        win.minSize = new Vector2(430, 700);
    }
    
    // 약 10fps 자동 갱신 (씬 뷰 영향 없음)
    private void OnInspectorUpdate() => Repaint();
    
    private void InitStyles()
    {
        if (_stylesInitialized) return;
        
        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };
        
        _valueStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.4f, 0.9f, 0.4f) }
        };
        
        _warnStyle = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = new Color(1f, 0.8f, 0.2f) }
        };
        
        _stylesInitialized = true;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    void OnGUI()
    {
        InitStyles();
        
        // ── 타이틀 ──────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        GUILayout.Label("📊 스탯 디버그 도구", _headerStyle);
        EditorGUILayout.Space(2);
        
        // ── 플레이 모드 체크 ─────────────────────────────────────────
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ 플레이 모드에서만 사용 가능합니다!", MessageType.Warning);
            return;
        }
        
        // ── PlayerRuntimeStats 탐색 ──────────────────────────────────
        var stats = FindObjectOfType<PlayerRuntimeStats>();
        if (stats == null)
        {
            EditorGUILayout.HelpBox(
                "❌ PlayerRuntimeStats를 씬에서 찾을 수 없습니다.\n" +
                "플레이어 캐릭터가 스폰된 전투 씬에서 사용해 주세요.",
                MessageType.Error);
            return;
        }
        
        // ── 저장 안됨 경고 ────────────────────────────────────────────
        EditorGUILayout.HelpBox(
            "✅ 이 창의 오버라이드 값은 장비/스킬 데이터에 영향을 주지 않습니다.\n" +
            "   게임 재시작 또는 [전체 초기화] 버튼으로 즉시 원복됩니다.",
            MessageType.Info);
        
        EditorGUILayout.Space(4);
        
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        
        // ── Section 1: 현재 최종 스탯 현황 ──────────────────────────
        DrawCurrentStatsSection(stats);
        
        EditorGUILayout.Space(6);
        
        // ── Section 2: 임시 오버라이드 ───────────────────────────────
        DrawOverrideSection(stats);
        
        EditorGUILayout.Space(6);
        
        // ── Section 3: 전투 로그 설정 ────────────────────────────────
        DrawCombatLogSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // Section 1 — 현재 최종 스탯 현황
    // ═══════════════════════════════════════════════════════════════════
    private void DrawCurrentStatsSection(PlayerRuntimeStats s)
    {
        _showCurrentStats = EditorGUILayout.BeginFoldoutHeaderGroup(_showCurrentStats, "📊 현재 최종 스탯 (PlayerRuntimeStats)");
        if (!_showCurrentStats) { EditorGUILayout.EndFoldoutHeaderGroup(); return; }
        
        EditorGUILayout.BeginVertical("box");
        
        // 오버라이드 활성 여부 표시
        var bonus = s.GetDebugBonus();
        if (HasAnyBonus(bonus))
        {
            EditorGUILayout.HelpBox(
                "🔧 DEBUG 오버라이드 활성 중 — 아래 수치에 임시 보너스가 반영되어 있습니다.",
                MessageType.Warning);
        }
        
        // 헤더
        DrawColumnHeader();
        
        // ── 기본 스탯 ──
        GUILayout.Label("  ▶ 기본 스탯", EditorStyles.miniBoldLabel);
        DrawReadOnlyRow("⚔️  공격력      ATK_FLAT",    s.FinalAttackDamage,    "F1");
        DrawReadOnlyRow("❤️  최대체력    HP_FLAT",     s.FinalMaxHealth,       "F0");
        DrawReadOnlyRow("🛡️  방어력      DEF_FLAT",    s.FinalDefense,         "F1");
        DrawReadOnlyRow("🏃  이동속도    MOVE_SPEED",  s.FinalMoveSpeed,       "F2");
        DrawReadOnlyRow("⚡  공격속도    ASPD",        s.FinalAttackSpeed,     "F2");
        DrawReadOnlyRow("🎯  치명확률    CRIT_RATE",   s.FinalCriticalChance,  "P1");
        DrawReadOnlyRow("💥  치명배율    CRIT_DMG",    s.FinalCriticalDamage,  "F2");
        DrawReadOnlyRow("💚  회복효율    HEAL_MULT",   s.FinalHealMultiplier,  "F2");
        
        EditorGUILayout.Space(4);
        
        // ── 특수 스탯 ──
        GUILayout.Label("  ▶ 특수 스탯 (10종)", EditorStyles.miniBoldLabel);
        DrawReadOnlyRow("✨  스킬피해    SKILL_DMG%",  s.FinalSkillDamageBonus,  "P1");
        DrawReadOnlyRow("⏱️  쿨다운감소  CDR",        s.FinalCooldownReduction, "P1");
        DrawReadOnlyRow("🔰  피해감소    DMG_RED%",   s.FinalDamageReduction,   "P1");
        DrawReadOnlyRow("💊  HP재생      HP_REGEN",   s.FinalHpRegen,           "F1");
        DrawReadOnlyRow("🩸  흡혈        LIFESTEAL",  s.FinalLifeSteal,         "P1");
        DrawReadOnlyRow("🔓  방어관통    ARMOR_PEN",  s.FinalArmorPenetration,  "P1");
        DrawReadOnlyRow("💨  회피        DODGE",      s.FinalDodgeChance,       "P1");
        DrawReadOnlyRow("🛑  블록        BLOCK",      s.FinalBlockChance,       "P1");
        DrawReadOnlyRow("⭐  경험치+     EXP_GAIN%",  s.FinalExpGainBonus,      "P1");
        DrawReadOnlyRow("🔮  상태이상저항 STATUS_R",  s.FinalStatusResist,      "P1");
        DrawReadOnlyRow("🏹  관통유지율   PIERCE_RET", s.FinalPierceDamageRetention, "P1");
        
        EditorGUILayout.Space(4);
        
        // 버튼 행
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🖨️ 콘솔에 전체 출력", GUILayout.Height(28)))
            LogAllStatsToConsole(s);
        if (GUILayout.Button("🔄 새로고침", GUILayout.Width(80), GUILayout.Height(28)))
            Repaint();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // Section 2 — 임시 오버라이드
    // ═══════════════════════════════════════════════════════════════════
    private void DrawOverrideSection(PlayerRuntimeStats s)
    {
        _showOverride = EditorGUILayout.BeginFoldoutHeaderGroup(_showOverride, "🔧 임시 스탯 오버라이드 (저장 안됨)");
        if (!_showOverride) { EditorGUILayout.EndFoldoutHeaderGroup(); return; }
        
        EditorGUILayout.BeginVertical("box");
        
        // 컬럼 헤더
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("스탯 (StatId)", EditorStyles.miniBoldLabel, GUILayout.Width(COL_LABEL));
        GUILayout.Label("현재값", EditorStyles.miniBoldLabel, GUILayout.Width(COL_CURRENT));
        GUILayout.Label("+델타(추가량)", EditorStyles.miniBoldLabel, GUILayout.Width(COL_DELTA));
        EditorGUILayout.EndHorizontal();
        DrawSeparator();
        
        // ── 기본 스탯 ──────────────────────────────────────────────
        GUILayout.Label("  ▶ 기본 스탯", EditorStyles.miniBoldLabel);
        DrawOverrideRow("⚔️  ATK_FLAT  (공격력)",      s.FinalAttackDamage,   ref _dAtkFlat,     "F1");
        DrawAtkPercentRow(s);  // ATK_PERCENT는 별도 처리
        DrawOverrideRow("❤️  HP_FLAT   (최대체력)",    s.FinalMaxHealth,      ref _dMaxHp,       "F0");
        DrawOverrideRow("🛡️  DEF_FLAT  (방어력)",      s.FinalDefense,        ref _dDefense,     "F1");
        DrawOverrideRow("🏃  MOVE_SPEED(이동속도)",    s.FinalMoveSpeed,      ref _dMoveSpeed,   "F2");
        DrawOverrideRow("⚡  ASPD      (공격속도)",    s.FinalAttackSpeed,    ref _dAtkSpeed,    "F2");
        DrawOverrideRow("🎯  CRIT_RATE (치명확률)",    s.FinalCriticalChance, ref _dCritChance,  "P1");
        DrawOverrideRow("💥  CRIT_DMG  (치명배율)",    s.FinalCriticalDamage, ref _dCritDmg,     "F2");
        DrawOverrideRow("💚  HEAL_MULT (회복효율)",    s.FinalHealMultiplier, ref _dHealMult,    "F2");
        
        EditorGUILayout.Space(4);
        
        // ── 특수 스탯 ──────────────────────────────────────────────
        GUILayout.Label("  ▶ 특수 스탯 (소수 입력: 0.1 = 10%)", EditorStyles.miniBoldLabel);
        DrawOverrideRow("✨  SKILL_DMG_PERCENT",         s.FinalSkillDamageBonus,  ref _dSkillDmg,    "P1");
        DrawOverrideRow("⏱️  COOLDOWN_REDUCTION(max0.5)", s.FinalCooldownReduction, ref _dCdr,         "P1");
        DrawOverrideRow("🔰  DAMAGE_REDUCTION(max0.8)",  s.FinalDamageReduction,   ref _dDmgRed,      "P1");
        DrawOverrideRow("💊  HP_REGEN     (HP/sec)",     s.FinalHpRegen,           ref _dHpRegen,     "F1");
        DrawOverrideRow("🩸  LIFESTEAL",                 s.FinalLifeSteal,         ref _dLifeSteal,   "P1");
        DrawOverrideRow("🔓  ARMOR_PENETRATION",         s.FinalArmorPenetration,  ref _dArmorPen,    "P1");
        DrawOverrideRow("💨  DODGE_CHANCE",              s.FinalDodgeChance,       ref _dDodge,       "P1");
        DrawOverrideRow("🛑  BLOCK_CHANCE",              s.FinalBlockChance,       ref _dBlock,       "P1");
        DrawOverrideRow("⭐  EXP_GAIN_PERCENT",          s.FinalExpGainBonus,      ref _dExpGain,     "P1");
        DrawOverrideRow("🔮  STATUS_RESIST_ALL",         s.FinalStatusResist,      ref _dStatusResist,"P1");
        DrawOverrideRow("🏹  PIERCE_DAMAGE_RETENTION",  s.FinalPierceDamageRetention, ref _dPierceRetention, "P1");
        
        EditorGUILayout.Space(6);
        
        // 적용 / 초기화 버튼
        EditorGUILayout.BeginHorizontal();
        var applyColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("✅ 전체 오버라이드 적용", GUILayout.Height(30)))
        {
            ApplyBonus(s);
        }
        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("🔁 전체 초기화", GUILayout.Width(110), GUILayout.Height(30)))
        {
            ResetAll(s);
        }
        GUI.backgroundColor = applyColor;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(4);
        
        // 빠른 테스트 프리셋
        GUILayout.Label("  ▶ 빠른 테스트 프리셋", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("공격력 +100", GUILayout.Height(24)))
        {
            _dAtkFlat += 100f;
            ApplyBonus(s);
        }
        if (GUILayout.Button("ATK% +50%", GUILayout.Height(24)))
        {
            _dAtkPercent += 0.5f;
            ApplyBonus(s);
        }
        if (GUILayout.Button("크리 100%", GUILayout.Height(24)))
        {
            _dCritChance = Mathf.Clamp01(1f - s.FinalCriticalChance + _dCritChance);
            ApplyBonus(s);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("흡혈 30%", GUILayout.Height(24)))
        {
            _dLifeSteal += 0.3f;
            ApplyBonus(s);
        }
        if (GUILayout.Button("관통 50%", GUILayout.Height(24)))
        {
            _dArmorPen += 0.5f;
            ApplyBonus(s);
        }
        if (GUILayout.Button("CDR 50%", GUILayout.Height(24)))
        {
            _dCdr = 0.5f;
            ApplyBonus(s);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // Section 3 — 전투 로그 설정
    // ═══════════════════════════════════════════════════════════════════
    private void DrawCombatLogSection()
    {
        _showCombatLog = EditorGUILayout.BeginFoldoutHeaderGroup(_showCombatLog, "🎯 CombatFormula 전투 로그");
        if (!_showCombatLog) { EditorGUILayout.EndFoldoutHeaderGroup(); return; }
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.HelpBox(
            "활성화 시 매 타격마다 Step 1~7 전 과정 + Phase 7 흡혈이 콘솔에 출력됩니다.\n" +
            "CombatFormulaConfig 에셋은 변경되지 않으므로 저장 안됩니다.",
            MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("상세 전투 로그 (Step 1~7):", GUILayout.Width(200));
        
        bool currentFlag = CombatFormula._forceDetailedLog;
        var toggleColor  = currentFlag ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.8f, 0.8f, 0.8f);
        GUI.backgroundColor = toggleColor;
        
        if (GUILayout.Button(currentFlag ? "■ ON  (클릭 → OFF)" : "□ OFF (클릭 → ON)", GUILayout.Height(24)))
        {
            CombatFormula._forceDetailedLog = !currentFlag;
            Debug.Log($"🎯 [StatDebug] CombatFormula 상세 로그: {(CombatFormula._forceDetailedLog ? "✅ 활성" : "❌ 비활성")}");
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        if (currentFlag)
        {
            EditorGUILayout.HelpBox(
                "📋 로그 항목:\n" +
                "  Step 1: 기본 공격력\n  Step 2: 스킬 배율\n  Step 3: 버서커 등 동적 보너스\n" +
                "  Step 4: 백어택\n  Step 5: 크리티컬\n" +
                "  Step 6: 방어관통 + 방어력 감소\n  Phase 7: 흡혈",
                MessageType.None);
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // UI 헬퍼 메서드
    // ═══════════════════════════════════════════════════════════════════
    
    private void DrawColumnHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("스탯", EditorStyles.miniBoldLabel, GUILayout.Width(COL_LABEL));
        GUILayout.Label("현재 최종값", EditorStyles.miniBoldLabel, GUILayout.Width(COL_CURRENT + COL_DELTA));
        EditorGUILayout.EndHorizontal();
        DrawSeparator();
    }
    
    private void DrawReadOnlyRow(string label, float value, string format)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(COL_LABEL));
        GUILayout.Label(FormatValue(value, format), _valueStyle, GUILayout.Width(COL_CURRENT + COL_DELTA));
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawOverrideRow(string label, float currentValue, ref float delta, string format)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(COL_LABEL));
        GUILayout.Label(FormatValue(currentValue, format), _valueStyle, GUILayout.Width(COL_CURRENT));
        delta = EditorGUILayout.FloatField(delta, GUILayout.Width(COL_DELTA));
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>ATK_PERCENT는 별도 행: 현재값이 없으므로 현재 공식을 표시</summary>
    private void DrawAtkPercentRow(PlayerRuntimeStats s)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("⚔️  ATK_PERCENT(공격력%배율)", GUILayout.Width(COL_LABEL));
        // 현재 ATK_PERCENT 값은 PlayerRuntimeStats에 별도 필드가 없으므로 "N/A" 표시
        GUILayout.Label("N/A", _valueStyle, GUILayout.Width(COL_CURRENT));
        _dAtkPercent = EditorGUILayout.FloatField(_dAtkPercent, GUILayout.Width(COL_DELTA));
        EditorGUILayout.EndHorizontal();
        // 힌트: 현재 입력값 표시
        if (_dAtkPercent != 0f)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(COL_LABEL + 4);
            EditorGUILayout.HelpBox($"   → 공격력 × {1f + _dAtkPercent:F2}  ({_dAtkPercent:+P0;-P0;±0} 적용)", MessageType.None);
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f));
    }
    
    private string FormatValue(float v, string format)
    {
        return format switch
        {
            "P1" => $"{v * 100f:F1}%",
            "F0" => $"{v:F0}",
            "F1" => $"{v:F1}",
            "F2" => $"{v:F2}",
            _    => v.ToString(format)
        };
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // 보너스 적용 / 초기화
    // ═══════════════════════════════════════════════════════════════════
    
    private void ApplyBonus(PlayerRuntimeStats s)
    {
        var bonus = new PlayerRuntimeStats.DebugStatBonus
        {
            atkFlat      = _dAtkFlat,
            atkPercent   = _dAtkPercent,
            maxHp        = _dMaxHp,
            defense      = _dDefense,
            moveSpeed    = _dMoveSpeed,
            atkSpeed     = _dAtkSpeed,
            critChance   = _dCritChance,
            critDmg      = _dCritDmg,
            healMult     = _dHealMult,
            skillDmg     = _dSkillDmg,
            cdr          = _dCdr,
            dmgRed       = _dDmgRed,
            hpRegen      = _dHpRegen,
            lifeSteal    = _dLifeSteal,
            armorPen     = _dArmorPen,
            dodge        = _dDodge,
            block        = _dBlock,
            expGain      = _dExpGain,
            statusResist    = _dStatusResist,
            pierceRetention = _dPierceRetention,
        };
        
        s.SetDebugBonus(bonus);
        
        Debug.Log($"🔧 [StatDebug] 오버라이드 적용 완료 — " +
                  $"ATK+{_dAtkFlat:F1}(x{1f+_dAtkPercent:F2}), HP+{_dMaxHp:F0}, DEF+{_dDefense:F1}");
        
        Repaint();
    }
    
    private void ResetAll(PlayerRuntimeStats s)
    {
        _dAtkFlat = _dAtkPercent = _dMaxHp = _dDefense = _dMoveSpeed = _dAtkSpeed = 0f;
        _dCritChance = _dCritDmg = _dHealMult = _dSkillDmg = _dCdr = _dDmgRed = 0f;
        _dHpRegen = _dLifeSteal = _dArmorPen = _dDodge = _dBlock = _dExpGain = _dStatusResist = _dPierceRetention = 0f;
        
        s.ClearDebugBonus();
        Debug.Log("🔧 [StatDebug] 모든 오버라이드 초기화 완료 → PlayerRuntimeStats 재계산");
        Repaint();
    }
    
    private bool HasAnyBonus(PlayerRuntimeStats.DebugStatBonus b)
    {
        return b.atkFlat != 0 || b.atkPercent != 0 || b.maxHp != 0 || b.defense != 0 ||
               b.moveSpeed != 0 || b.atkSpeed != 0 || b.critChance != 0 || b.critDmg != 0 ||
               b.healMult != 0 || b.skillDmg != 0 || b.cdr != 0 || b.dmgRed != 0 ||
               b.hpRegen != 0 || b.lifeSteal != 0 || b.armorPen != 0 || b.dodge != 0 ||
               b.block != 0 || b.expGain != 0 || b.statusResist != 0 || b.pierceRetention != 0;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // 콘솔 전체 출력
    // ═══════════════════════════════════════════════════════════════════
    
    private void LogAllStatsToConsole(PlayerRuntimeStats s)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📊 [StatDebug] ============ 현재 최종 스탯 현황 ============");
        sb.AppendLine($"   플레이어 레벨 : Lv.{s.CurrentLevel}");
        
        // 기본 스탯
        sb.AppendLine("   ─── 기본 스탯 ───────────────────────────────────");
        sb.AppendLine($"   ⚔️  공격력      (ATK_FLAT)      : {s.FinalAttackDamage:F1}");
        sb.AppendLine($"   ❤️  최대체력    (HP_FLAT)       : {s.FinalMaxHealth:F0}");
        sb.AppendLine($"   🛡️  방어력      (DEF_FLAT)      : {s.FinalDefense:F1}");
        sb.AppendLine($"   🏃  이동속도    (MOVE_SPEED)    : {s.FinalMoveSpeed:F2}");
        sb.AppendLine($"   ⚡  공격속도    (ASPD)          : {s.FinalAttackSpeed:F2}");
        sb.AppendLine($"   🎯  치명확률    (CRIT_RATE)     : {s.FinalCriticalChance * 100f:F1}%");
        sb.AppendLine($"   💥  치명배율    (CRIT_DMG)      : {s.FinalCriticalDamage:F2}x");
        sb.AppendLine($"   💚  회복효율    (HEAL_MULT)     : {s.FinalHealMultiplier:F2}x");
        
        // 특수 스탯
        sb.AppendLine("   ─── 특수 스탯 (10종) ───────────────────────────");
        sb.AppendLine($"   ✨  스킬피해    (SKILL_DMG%)    : {s.FinalSkillDamageBonus * 100f:F1}%");
        sb.AppendLine($"   ⏱️  쿨다운감소  (CDR, cap 50%)  : {s.FinalCooldownReduction * 100f:F1}%");
        sb.AppendLine($"   🔰  피해감소    (DMG_RED, cap80%): {s.FinalDamageReduction * 100f:F1}%");
        sb.AppendLine($"   💊  HP재생      (HP_REGEN)      : {s.FinalHpRegen:F1} HP/s");
        sb.AppendLine($"   🩸  흡혈        (LIFESTEAL)     : {s.FinalLifeSteal * 100f:F1}%");
        sb.AppendLine($"   🔓  방어관통    (ARMOR_PEN)     : {s.FinalArmorPenetration * 100f:F1}%");
        sb.AppendLine($"   💨  회피        (DODGE_CHANCE)  : {s.FinalDodgeChance * 100f:F1}%");
        sb.AppendLine($"   🛑  블록        (BLOCK_CHANCE)  : {s.FinalBlockChance * 100f:F1}%");
        sb.AppendLine($"   ⭐  경험치+     (EXP_GAIN%)     : {s.FinalExpGainBonus * 100f:F1}%");
        sb.AppendLine($"   🔮  상태이상저항(STATUS_RESIST) : {s.FinalStatusResist * 100f:F1}%");
        sb.AppendLine($"   🏹  관통유지율  (PIERCE_RET)    : {s.FinalPierceDamageRetention * 100f:F1}%");
        
        // 디버그 오버라이드 상태
        var b = s.GetDebugBonus();
        if (HasAnyBonus(b))
        {
            sb.AppendLine("   ─── 🔧 DEBUG 오버라이드 반영분 ─────────────────");
            if (b.atkFlat    != 0) sb.AppendLine($"       ATK+{b.atkFlat:F1}");
            if (b.atkPercent != 0) sb.AppendLine($"       ATK%x{1f+b.atkPercent:F2}");
            if (b.maxHp      != 0) sb.AppendLine($"       HP+{b.maxHp:F0}");
            if (b.defense    != 0) sb.AppendLine($"       DEF+{b.defense:F1}");
            if (b.critChance != 0) sb.AppendLine($"       CritRate+{b.critChance*100:F1}%");
            if (b.lifeSteal  != 0) sb.AppendLine($"       LifeSteal+{b.lifeSteal*100:F1}%");
            if (b.armorPen   != 0) sb.AppendLine($"       ArmorPen+{b.armorPen*100:F1}%");
        }
        
        sb.AppendLine("   ====================================================");
        Debug.Log(sb.ToString());
    }
}

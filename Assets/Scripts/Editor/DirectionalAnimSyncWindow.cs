// File: Assets/Editor/DirectionalAnimSyncWindow.cs
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DirectionalAnimSyncWindow : EditorWindow
{
    [Header("1) 기준 클립 (한 방향)")]
    public AnimationClip baseClip;

    [Header("2) 대상 클립들 (나머지 7방향)")]
    public AnimationClip[] targetClips = new AnimationClip[7];

    [Header("옵션")]
    public bool syncFrameRate = true;        // 기준과 같은 frameRate로 통일
    public bool copyEvents = true;           // Animation Events 복사
    public bool retimeSpriteKeys = true;     // 스프라이트 키 타이밍을 기준과 일치시키기
    public bool retimeOtherCurves = false;   // (선택) Transform 등 다른 곡선들도 타이밍만 맞추기

    private const string SPRITE_PROP = "m_Sprite";

    [MenuItem("Tools/Sprites/Directional Anim Sync")]
    private static void Open()
    {
        GetWindow<DirectionalAnimSyncWindow>("Directional Anim Sync");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("8방향 동기화 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        baseClip = (AnimationClip)EditorGUILayout.ObjectField("Base Clip", baseClip, typeof(AnimationClip), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target Clips (나머지 7개 방향)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        for (int i = 0; i < targetClips.Length; i++)
        {
            targetClips[i] = (AnimationClip)EditorGUILayout.ObjectField($"Target {i + 1}", targetClips[i], typeof(AnimationClip), false);
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("옵션", EditorStyles.boldLabel);
        syncFrameRate     = EditorGUILayout.ToggleLeft("FrameRate를 기준과 동일하게", syncFrameRate);
        copyEvents        = EditorGUILayout.ToggleLeft("Animation Events 복사", copyEvents);
        retimeSpriteKeys  = EditorGUILayout.ToggleLeft("Sprite 키 타이밍 동기화 (이미지는 유지)", retimeSpriteKeys);
        retimeOtherCurves = EditorGUILayout.ToggleLeft("(선택) 기타 곡선 타이밍도 동기화", retimeOtherCurves);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(baseClip == null || targetClips.All(c => c == null)))
        {
            if (GUILayout.Button("Sync All (Events + Timing)", GUILayout.Height(32)))
            {
                try
                {
                    SyncAll();
                    EditorUtility.DisplayDialog("완료", "동기화가 끝났습니다!", "OK");
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    EditorUtility.DisplayDialog("에러", ex.Message, "OK");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "- 기준 클립의 이벤트와 키프레임 '시간'을 복사합니다.\n" +
            "- 각 방향 클립의 스프라이트 '값(이미지)'는 그대로 유지합니다.\n" +
            "- 스프라이트 개수가 달라도 비율로 최대한 매칭해 줍니다.",
            MessageType.Info);
    }

    private void SyncAll()
    {
        if (baseClip == null) throw new Exception("Base Clip을 지정해 주세요.");

        // 1) 기준의 스프라이트 키 타임 가져오기
        var baseSpriteTimes = GetSpriteKeyTimes(baseClip);

        // 2) (옵션) 기준의 기타 곡선(Transform 등) 키 타임도 가져오기
        var baseOtherCurves = retimeOtherCurves ? GetOtherCurvesKeyTimes(baseClip) : null;

        // 3) (옵션) 프레임레이트
        float baseFrameRate = baseClip.frameRate;

        // 4) (옵션) 이벤트
        var baseEvents = AnimationUtility.GetAnimationEvents(baseClip);

        foreach (var target in targetClips)
        {
            if (target == null || target == baseClip) continue;

            Undo.RecordObject(target, "Directional Anim Sync");

            if (syncFrameRate)
            {
                target.frameRate = baseFrameRate;
                EditorUtility.SetDirty(target);
            }

            if (copyEvents)
            {
                AnimationUtility.SetAnimationEvents(target, baseEvents);
                EditorUtility.SetDirty(target);
            }

            if (retimeSpriteKeys)
            {
                RetimeSpriteCurve(target, baseSpriteTimes);
            }

            if (retimeOtherCurves && baseOtherCurves != null)
            {
                RetimeOtherCurves(target, baseOtherCurves);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 스프라이트 커브 타이밍 동기화 (값은 유지, 시간만 기준과 동일하게)
    // ─────────────────────────────────────────────────────────────────────────────
    private void RetimeSpriteCurve(AnimationClip target, float[] baseTimes)
    {
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(target);
        var spriteBinding = bindings.FirstOrDefault(b =>
            b.type == typeof(SpriteRenderer) && b.propertyName == SPRITE_PROP);

        if (spriteBinding.type == null)
        {
            Debug.LogWarning($"[DirectionalAnimSync] '{target.name}' 에 스프라이트 커브가 없습니다.");
            return;
        }

        var targetKeys = AnimationUtility.GetObjectReferenceCurve(target, spriteBinding);
        if (targetKeys == null || targetKeys.Length == 0)
        {
            Debug.LogWarning($"[DirectionalAnimSync] '{target.name}' 스프라이트 키가 없습니다.");
            return;
        }

        // 값(스프라이트)은 유지, 시간은 baseTimes에 맞춰 새로 구성
        var newKeys = new ObjectReferenceKeyframe[baseTimes.Length];

        // 타겟의 스프라이트 순서를 최대한 보존하기 위해 비율 매핑
        // base i번째 시간 → target의 round(i * (nTarget-1)/(nBase-1)) 번째 스프라이트 사용
        int nBase = baseTimes.Length;
        int nTgt  = targetKeys.Length;

        for (int i = 0; i < nBase; i++)
        {
            int idx = (nBase == 1 || nTgt == 1)
                ? 0
                : Mathf.RoundToInt(i * (nTgt - 1f) / (nBase - 1f));

            idx = Mathf.Clamp(idx, 0, nTgt - 1);

            newKeys[i] = new ObjectReferenceKeyframe
            {
                time = baseTimes[i],
                value = targetKeys[idx].value
            };
        }

        AnimationUtility.SetObjectReferenceCurve(target, spriteBinding, newKeys);
        EditorUtility.SetDirty(target);
    }

    // 기준 클립의 스프라이트 키 시간 배열 가져오기
    private float[] GetSpriteKeyTimes(AnimationClip clip)
    {
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        var spriteBinding = bindings.FirstOrDefault(b =>
            b.type == typeof(SpriteRenderer) && b.propertyName == SPRITE_PROP);

        if (spriteBinding.type == null)
            throw new Exception($"'{clip.name}' 에 스프라이트 커브가 없습니다. (SpriteRenderer.m_Sprite)");

        var keys = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
        if (keys == null || keys.Length == 0)
            throw new Exception($"'{clip.name}' 스프라이트 키가 없습니다.");

        return keys.Select(k => k.time).ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // (선택) 기타 곡선(Transform 등) 타이밍 동기화
    // 값 곡선을 건드리지 않고, 키의 '시간'만 base의 타임스탬프에 비율로 재배치
    // ─────────────────────────────────────────────────────────────────────────────
    private (EditorCurveBinding binding, float[] times, AnimationCurve curve)[] GetOtherCurvesKeyTimes(AnimationClip clip)
    {
        var curveBindings = AnimationUtility.GetCurveBindings(clip);
        return curveBindings
            .Where(b => !(b.type == typeof(SpriteRenderer) && b.propertyName == SPRITE_PROP))
            .Select(b =>
            {
                var curve = AnimationUtility.GetEditorCurve(clip, b);
                var times = curve.keys.Select(k => k.time).ToArray();
                return (b, times, curve);
            })
            .ToArray();
    }

    private void RetimeOtherCurves(AnimationClip target, (EditorCurveBinding binding, float[] times, AnimationCurve curve)[] baseCurvesInfo)
    {
        foreach (var (binding, baseTimes, baseCurve) in baseCurvesInfo)
        {
            var tgtCurve = AnimationUtility.GetEditorCurve(target, binding);
            if (tgtCurve == null || tgtCurve.keys.Length == 0)
                continue;

            var tgtKeys = tgtCurve.keys;
            int nBase = baseTimes.Length;
            int nTgt  = tgtKeys.Length;

            if (nBase == 0 || nTgt == 0)
                continue;

            // 값은 타겟의 값을 유지하고, 시간만 기준에 맞춰 재배치(비율 매칭)
            var newKeys = new Keyframe[nBase];
            for (int i = 0; i < nBase; i++)
            {
                int idx = (nBase == 1 || nTgt == 1)
                    ? 0
                    : Mathf.RoundToInt(i * (nTgt - 1f) / (nBase - 1f));
                idx = Mathf.Clamp(idx, 0, nTgt - 1);

                var old = tgtKeys[idx];
                var k = new Keyframe(baseTimes[i], old.value, old.inTangent, old.outTangent, old.inWeight, old.outWeight)
                {
                    weightedMode = old.weightedMode
                };
                newKeys[i] = k;
            }

            var newCurve = new AnimationCurve(newKeys)
            {
                preWrapMode = tgtCurve.preWrapMode,
                postWrapMode = tgtCurve.postWrapMode
            };

            AnimationUtility.SetEditorCurve(target, binding, newCurve);
            EditorUtility.SetDirty(target);
        }
    }
}

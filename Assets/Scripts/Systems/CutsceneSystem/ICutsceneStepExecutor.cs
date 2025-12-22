using DG.Tweening;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 Step Executor 인터페이스
    /// 각 Step 타입별 Executor 구현
    /// </summary>
    public interface ICutsceneStepExecutor
    {
        /// <summary>
        /// Step 실행 (Tween 반환)
        /// SequenceBuilder가 이 Tween을 Append하여 타이밍 제어
        /// </summary>
        Tween Execute(CutsceneStep step, CutsceneContext context);
        
        /// <summary>
        /// 스킵 시 정리 로직 (선택적)
        /// </summary>
        void OnSkip(CutsceneContext context);
    }
}

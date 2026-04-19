using UnityEngine;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 Canvas 로드 관리
    /// Resources에서 Canvas 프리팹을 로드하고 인스턴스 생성/정리
    /// </summary>
    public static class CutsceneCanvasLoader
    {
        private static GameObject currentCanvasInstance;
        
        /// <summary>
        /// Canvas 프리팹 로드 및 인스턴스 생성
        /// </summary>
        /// <param name="prefabPath">Resources 경로 (예: "Prefabs/Cutscene/CutsceneCanvas")</param>
        /// <returns>생성된 Canvas 인스턴스</returns>
        public static GameObject LoadCanvas(string prefabPath = "Prefabs/Cutscene/CutsceneCanvas")
        {
            // 기존 Canvas 정리
            UnloadCanvas();
            
            // 프리팹 로드
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogError($"[CutsceneCanvasLoader] Canvas 프리팹을 찾을 수 없습니다: {prefabPath}");
                Debug.LogError($"[CutsceneCanvasLoader] Resources 폴더에 '{prefabPath}.prefab' 파일이 있는지 확인해주세요.");
                return null;
            }
            
            // 인스턴스 생성
            currentCanvasInstance = Object.Instantiate(prefab);
            currentCanvasInstance.name = "CutsceneCanvas (Runtime)";
            
            // Canvas 설정 확인
            Canvas canvas = currentCanvasInstance.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[CutsceneCanvasLoader] Canvas 컴포넌트가 없습니다. 추가합니다.");
                canvas = currentCanvasInstance.AddComponent<Canvas>();
            }
            
            // Canvas 설정
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 다른 UI 위에 표시
            
            // CanvasScaler 설정
            UnityEngine.UI.CanvasScaler scaler = currentCanvasInstance.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
            {
                scaler = currentCanvasInstance.AddComponent<UnityEngine.UI.CanvasScaler>();
            }
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            
            return currentCanvasInstance;
        }
        
        /// <summary>
        /// 현재 Canvas 인스턴스 가져오기
        /// </summary>
        public static GameObject GetCurrentCanvas()
        {
            return currentCanvasInstance;
        }
        
        /// <summary>
        /// Canvas 인스턴스 정리
        /// </summary>
        public static void UnloadCanvas()
        {
            if (currentCanvasInstance != null)
            {
                Object.Destroy(currentCanvasInstance);
                currentCanvasInstance = null;
            }
        }
        
        /// <summary>
        /// Canvas가 로드되어 있는지 확인
        /// </summary>
        public static bool IsCanvasLoaded()
        {
            return currentCanvasInstance != null && currentCanvasInstance.activeSelf;
        }
    }
}

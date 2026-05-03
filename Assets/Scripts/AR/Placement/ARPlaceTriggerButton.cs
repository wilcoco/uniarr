using UnityEngine;
using UnityEngine.UI;

namespace GuardianAR
{
    /// <summary>
    /// AR 모드에서 "타워 배치" 트리거 버튼.
    /// Editor에서 PersistentListener를 직렬화하면 깨지기 쉬워서 런타임에 Button.onClick에 핸들러를 직접 연결.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ARPlaceTriggerButton : MonoBehaviour
    {
        void Start()
        {
            var btn = GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var placer = ARFixedGuardianPlacer.Instance;
                if (placer == null)
                    placer = Object.FindFirstObjectByType<ARFixedGuardianPlacer>(FindObjectsInactive.Include);

                if (placer == null)
                {
                    Debug.LogWarning("[ARPlaceTrigger] ARFixedGuardianPlacer not found in scene");
                    return;
                }
                if (!placer.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("[ARPlaceTrigger] placer GameObject inactive — AR 모드 진입 후 다시 시도");
                    return;
                }
                placer.StartPlacementMode();
            });
        }
    }
}

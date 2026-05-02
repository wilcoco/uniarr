using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// 맵 모드 ↔ AR 모드 전환 관리
    /// </summary>
    public class ModeController : MonoBehaviour
    {
        public static ModeController Instance { get; private set; }

        public enum GameMode { Map, AR }
        public GameMode CurrentMode { get; private set; } = GameMode.Map;

        [Header("Mode Roots")]
        [SerializeField] private GameObject mapModeRoot;
        [SerializeField] private GameObject arModeRoot;

        [Header("Controllers")]
        [SerializeField] private ARModeController arController;

        [Header("Common UI")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject battleModalRoot;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            SetMode(GameMode.Map);
            if (GameManager.Instance != null && battleModalRoot != null)
            {
                GameManager.Instance.OnBattleTriggered += _ => battleModalRoot.SetActive(true);
                GameManager.Instance.OnBattleEnded     += () => battleModalRoot.SetActive(false);
            }
        }

        public void SwitchToAR()
        {
            if (CurrentMode == GameMode.AR) return;
            SetMode(GameMode.AR);
            if (arController != null) arController.EnterARMode();
        }

        public void SwitchToMap()
        {
            if (CurrentMode == GameMode.Map) return;
            if (arController != null) arController.ExitARMode();
            SetMode(GameMode.Map);
        }

        // WebView 모드(v3): mapModeRoot 없이 동작 — null 안전 처리
        private void SetMode(GameMode mode)
        {
            CurrentMode = mode;
            if (mapModeRoot != null) mapModeRoot.SetActive(mode == GameMode.Map);
            if (arModeRoot  != null) arModeRoot.SetActive(mode == GameMode.AR);
        }
    }
}

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

        [Header("공통 UI")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject battleModalRoot;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            SetMode(GameMode.Map);
            GameManager.Instance.OnBattleTriggered += _ => battleModalRoot.SetActive(true);
            GameManager.Instance.OnBattleEnded     += () => battleModalRoot.SetActive(false);
        }

        public void SwitchToAR()
        {
            if (CurrentMode == GameMode.AR) return;
            SetMode(GameMode.AR);
            arController.EnterARMode();
        }

        public void SwitchToMap()
        {
            if (CurrentMode == GameMode.Map) return;
            arController.ExitARMode();
            SetMode(GameMode.Map);
        }

        private void SetMode(GameMode mode)
        {
            CurrentMode = mode;
            mapModeRoot.SetActive(mode == GameMode.Map);
            arModeRoot.SetActive(mode == GameMode.AR);
        }
    }
}

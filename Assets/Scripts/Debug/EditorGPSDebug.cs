using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace GuardianAR
{
    /// <summary>
    /// 에디터 전용 GPS 주입 패널 — 빌드에는 포함되지 않음
    /// </summary>
    public class EditorGPSDebug : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("서울 주요 좌표 프리셋")]
        private static readonly (string label, double lat, double lng)[] Presets =
        {
            ("강남역",     37.4981, 127.0276),
            ("홍대입구",   37.5572, 126.9248),
            ("광화문",     37.5760, 126.9769),
            ("여의도",     37.5244, 126.9243),
            ("잠실",       37.5133, 127.1001),
        };

        [SerializeField] private TMP_InputField latInput;
        [SerializeField] private TMP_InputField lngInput;
        [SerializeField] private TextMeshProUGUI currentLocText;
        [SerializeField] private GameObject panel;

        private bool isVisible = false;

        void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        // 키보드 이동 (WASD / 화살표)
        [Header("Movement")]
        [SerializeField] private float stepMeters = 20f; // 한 번 누를 때 이동 거리(m)
        [SerializeField] private float holdSpeedMps = 50f; // 홀드 시 m/s

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            // F1: 패널 토글
            if (kb.f1Key.wasPressedThisFrame) TogglePanel();

            // 현재 위치 표시 갱신
            if (isVisible && currentLocText != null)
            {
                var loc0 = LocationManager.Instance?.CurrentLocation;
                currentLocText.text = loc0 != null
                    ? $"Loc: {loc0.lat:F5}, {loc0.lng:F5}\n[WASD/Arrow] move  [Shift] x4 speed"
                    : "Loc: none";
            }

            // 키보드 이동 (Tap = stepMeters, Hold = holdSpeedMps)
            var loc = LocationManager.Instance?.CurrentLocation;
            if (loc == null) return;

            float speed = kb.shiftKey.isPressed ? holdSpeedMps * 4f : holdSpeedMps;
            float step  = kb.shiftKey.isPressed ? stepMeters * 4f  : stepMeters;

            double dLatHold = 0, dLngHold = 0;
            double dLatTap  = 0, dLngTap  = 0;

            // Hold (continuous)
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    dLatHold += MetersToLat (speed * Time.deltaTime);
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  dLatHold -= MetersToLat (speed * Time.deltaTime);
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dLngHold += MetersToLng (speed * Time.deltaTime, loc.lat);
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  dLngHold -= MetersToLng (speed * Time.deltaTime, loc.lat);

            // Tap (one-shot, 더 큰 스텝)
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)    dLatTap += MetersToLat (step);
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)  dLatTap -= MetersToLat (step);
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) dLngTap += MetersToLng (step, loc.lat);
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  dLngTap -= MetersToLng (step, loc.lat);

            double dLat = dLatHold + dLatTap;
            double dLng = dLngHold + dLngTap;
            if (dLat != 0 || dLng != 0)
            {
                LocationManager.Instance.InjectLocation(new LatLng(loc.lat + dLat, loc.lng + dLng));
            }
        }

        // 미터 단위 → 위/경도 차이 변환
        static double MetersToLat(double meters) => meters / 111320.0;
        static double MetersToLng(double meters, double atLat)
            => meters / (111320.0 * System.Math.Cos(atLat * System.Math.PI / 180.0));

        public void TogglePanel()
        {
            isVisible = !isVisible;
            if (panel != null) panel.SetActive(isVisible);
        }

        public void ApplyManual()
        {
            if (double.TryParse(latInput.text, out double lat) &&
                double.TryParse(lngInput.text, out double lng))
            {
                LocationManager.Instance.InjectLocation(new LatLng(lat, lng));
                Debug.Log($"[GPS Debug] 위치 주입: {lat}, {lng}");
            }
            else
            {
                Debug.LogWarning("[GPS Debug] 잘못된 좌표 형식");
            }
        }

        public void ApplyPreset(int index)
        {
            if (index < 0 || index >= Presets.Length) return;
            var p = Presets[index];
            LocationManager.Instance.InjectLocation(new LatLng(p.lat, p.lng));
            if (latInput != null) latInput.text = p.lat.ToString();
            if (lngInput != null) lngInput.text = p.lng.ToString();
            Debug.Log($"[GPS Debug] 프리셋 적용: {p.label} ({p.lat}, {p.lng})");
        }
#endif
    }
}

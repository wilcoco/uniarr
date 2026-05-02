using UnityEngine;

namespace GuardianAR
{
    public class AppBootstrap : MonoBehaviour
    {
        // 매니저들은 씬에 이미 있음 — 여기서는 아무것도 생성하지 않음
        // (이전 Prefab 기반 구조의 잔재 — 필드는 Inspector 호환을 위해 유지)
        [SerializeField] private ApiManager apiManagerPrefab;
        [SerializeField] private LocationManager locationManagerPrefab;
        [SerializeField] private GameManager gameManagerPrefab;
    }
}

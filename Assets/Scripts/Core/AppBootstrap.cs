using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// 씬 최상위 오브젝트에 붙이는 부트스트랩.
    /// DontDestroyOnLoad 싱글톤들을 순서대로 초기화한다.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Header("Core (씬에 반드시 1개)")]
        [SerializeField] private ApiManager apiManagerPrefab;
        [SerializeField] private LocationManager locationManagerPrefab;
        [SerializeField] private GameManager gameManagerPrefab;

        void Awake()
        {
            // 이미 존재하면 생성 생략 (씬 재로드 대비)
            if (ApiManager.Instance == null)
                Instantiate(apiManagerPrefab);

            if (LocationManager.Instance == null)
                Instantiate(locationManagerPrefab);

            if (GameManager.Instance == null)
                Instantiate(gameManagerPrefab);
        }
    }
}

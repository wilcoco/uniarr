using System;
using System.Collections;
using UnityEngine;

namespace GuardianAR
{
    public class LocationManager : MonoBehaviour
    {
        public static LocationManager Instance { get; private set; }

        public LatLng CurrentLocation { get; private set; }
        public bool HasLocation { get; private set; }
        public bool IsTracking { get; private set; }

        public event Action<LatLng> OnLocationUpdated;
        public event Action<string> OnLocationError;

        [SerializeField] private float updateIntervalSeconds = 5f;
        [SerializeField] private float desiredAccuracyMeters = 10f;
        [SerializeField] private float updateDistanceMeters = 5f;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartTracking()
        {
            if (IsTracking) return;
            StartCoroutine(TrackingCoroutine());
        }

        public void StopTracking()
        {
            IsTracking = false;
            Input.location.Stop();
        }

        private IEnumerator TrackingCoroutine()
        {
            // 권한 확인
            if (!Input.location.isEnabledByUser)
            {
                OnLocationError?.Invoke("위치 권한이 필요합니다. 설정에서 권한을 허용해주세요.");
                yield break;
            }

            Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);

            int timeout = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
            {
                yield return new WaitForSeconds(1f);
                timeout--;
            }

            if (timeout <= 0 || Input.location.status == LocationServiceStatus.Failed)
            {
                OnLocationError?.Invoke("위치를 가져올 수 없습니다.");
                yield break;
            }

            IsTracking = true;

            while (IsTracking)
            {
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    var info = Input.location.lastData;
                    var loc = new LatLng(info.latitude, info.longitude);

                    CurrentLocation = loc;
                    HasLocation = true;
                    OnLocationUpdated?.Invoke(loc);
                }

                yield return new WaitForSeconds(updateIntervalSeconds);
            }
        }

        // WebView에서 받은 GPS를 Unity LocationService 없이 주입 (에디터/WebView 전용)
        public void InjectLocation(LatLng loc)
        {
            CurrentLocation = loc;
            HasLocation = true;
            OnLocationUpdated?.Invoke(loc);
        }

        // 두 GPS 좌표 간 거리(미터) 계산
        public static float DistanceMeters(LatLng a, LatLng b)
        {
            const double R = 6371000;
            double dLat = (b.lat - a.lat) * Math.PI / 180;
            double dLng = (b.lng - a.lng) * Math.PI / 180;
            double sinLat = Math.Sin(dLat / 2);
            double sinLng = Math.Sin(dLng / 2);
            double x = sinLat * sinLat +
                       Math.Cos(a.lat * Math.PI / 180) * Math.Cos(b.lat * Math.PI / 180) *
                       sinLng * sinLng;
            return (float)(2 * R * Math.Asin(Math.Sqrt(x)));
        }

        // GPS → Unity 로컬 오프셋(미터) 변환 (AR 모드용)
        public static Vector3 GPSToLocalOffset(LatLng target, LatLng origin)
        {
            double latDiff = target.lat - origin.lat;
            double lngDiff = target.lng - origin.lng;
            float z = (float)(latDiff * 111320.0);
            float x = (float)(lngDiff * 111320.0 * Math.Cos(origin.lat * Math.PI / 180.0));
            return new Vector3(x, 0f, z);
        }
    }
}

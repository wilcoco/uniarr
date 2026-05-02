using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// GPS 좌표에 오브젝트를 고정시키는 컴포넌트.
    /// ARModeController의 원점을 기준으로 매 프레임 월드 좌표를 재계산한다.
    /// </summary>
    public class ARWorldAnchor : MonoBehaviour
    {
        public LatLng GPSPosition { get; private set; }

        private float heightOffset = 0f;
        private bool isSet = false;

        public void SetPosition(LatLng gps, float yOffset = 0f)
        {
            GPSPosition = gps;
            heightOffset = yOffset;
            isSet = true;
            ApplyPosition();
        }

        void Update()
        {
            if (isSet) ApplyPosition();
        }

        private void ApplyPosition()
        {
            var ctrl = ARModeController.Instance;
            if (ctrl == null || GPSPosition == null) return;

            Vector3 worldPos = ctrl.GPSToWorld(GPSPosition);
            worldPos.y = heightOffset;
            transform.position = worldPos;
        }
    }
}

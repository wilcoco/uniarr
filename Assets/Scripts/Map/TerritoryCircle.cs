using UnityEngine;
using UnityEngine.UI;

namespace GuardianAR
{
    /// <summary>
    /// UI Canvas 위에 영역 원을 그리는 컴포넌트 (UI LineRenderer 대체)
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TerritoryCircle : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image borderImage;

        private static readonly Color myColor = new(0f, 1f, 0.53f, 0.25f);
        private static readonly Color myBorder = new(0f, 1f, 0.53f, 0.9f);
        private static readonly Color enemyColor = new(1f, 0.27f, 0.27f, 0.25f);
        private static readonly Color enemyBorder = new(1f, 0.27f, 0.27f, 0.9f);
        // 취약 상태: 노란빛 + 점멸
        private static readonly Color vulnerableFill   = new(1f, 0.85f, 0f, 0.30f);
        private static readonly Color vulnerableBorder = new(1f, 0.85f, 0f, 1f);

        private bool _vulnerable = false;
        private bool _isOwn = false;

        public void SetCircle(float radiusPx, bool isOwn)
        {
            _isOwn = isOwn;
            float diameter = radiusPx * 2f;

            if (fillImage != null)
                fillImage.rectTransform.sizeDelta = new Vector2(diameter, diameter);

            if (borderImage != null)
                borderImage.rectTransform.sizeDelta = new Vector2(diameter + 4f, diameter + 4f);

            ApplyColor();
        }

        public void SetVulnerable(bool vulnerable)
        {
            _vulnerable = vulnerable;
            ApplyColor();
        }

        void ApplyColor()
        {
            Color fill, border;
            if (_vulnerable)
            {
                fill   = vulnerableFill;
                border = vulnerableBorder;
            }
            else if (_isOwn)
            {
                fill   = myColor;
                border = myBorder;
            }
            else
            {
                fill   = enemyColor;
                border = enemyBorder;
            }
            if (fillImage   != null) fillImage.color   = fill;
            if (borderImage != null) borderImage.color = border;
        }

        // 취약 시 점멸 (Update에서 alpha 조절)
        void Update()
        {
            if (!_vulnerable || borderImage == null) return;
            float a = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 4f));
            var c = borderImage.color;
            c.a = a;
            borderImage.color = c;
        }
    }
}

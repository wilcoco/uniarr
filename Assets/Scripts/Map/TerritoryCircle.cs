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

        public void SetCircle(float radiusPx, bool isOwn)
        {
            float diameter = radiusPx * 2f;

            if (fillImage != null)
            {
                fillImage.rectTransform.sizeDelta = new Vector2(diameter, diameter);
                fillImage.color = isOwn ? myColor : enemyColor;
            }

            if (borderImage != null)
            {
                // 테두리는 fill보다 약간 크게
                borderImage.rectTransform.sizeDelta = new Vector2(diameter + 4f, diameter + 4f);
                borderImage.color = isOwn ? myBorder : enemyBorder;
            }
        }
    }
}

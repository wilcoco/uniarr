using TMPro;
using UnityEngine;

namespace GuardianAR
{
    public class FixedGuardianARObject : MonoBehaviour
    {
        [SerializeField] private TextMeshPro ownerLabel;
        [SerializeField] private TextMeshPro typeLabel;
        [SerializeField] private Renderer bodyRenderer;

        private static readonly Color defenseColor = new(0.27f, 0.53f, 1f);
        private static readonly Color productionColor = new(1f, 0.85f, 0f);

        public void Setup(FixedGuardian fg)
        {
            if (ownerLabel != null) ownerLabel.text = $"{fg.owner} DEF:{fg.Def}";
            if (typeLabel != null) typeLabel.text = fg.type == "production" ? "PRD" : "DEF";

            if (bodyRenderer != null)
            {
                var c = fg.type == "production" ? productionColor : defenseColor;
                bodyRenderer.material.color = c;
                bodyRenderer.material.SetColor("_EmissionColor", c * 1.5f);
            }
        }

        void Update()
        {
            if (Camera.main != null)
                transform.LookAt(Camera.main.transform);
        }
    }
}

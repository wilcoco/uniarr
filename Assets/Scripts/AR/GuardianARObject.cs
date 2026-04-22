using TMPro;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// AR 공간에 배치되는 수호신 오브젝트
    /// </summary>
    public class GuardianARObject : MonoBehaviour
    {
        [SerializeField] private TextMeshPro nameLabel;
        [SerializeField] private TextMeshPro typeLabel;
        [SerializeField] private Renderer bodyRenderer;

        // 수호신 타입별 색상
        private static readonly Color animalColor = new(0.2f, 0.8f, 0.2f);
        private static readonly Color robotColor = new(0.2f, 0.6f, 1f);
        private static readonly Color aircraftColor = new(1f, 0.8f, 0.2f);
        private static readonly Color myGuardianGlow = new(1f, 0.84f, 0f); // 금색

        public void Setup(Guardian guardian, bool isMine)
        {
            if (typeLabel != null)
                typeLabel.text = GetTypeEmoji(guardian.type);

            if (nameLabel != null)
                nameLabel.text = isMine ? "나" : "";

            ApplyColor(guardian.type, isMine);
            UpdateScale(guardian.stats);
        }

        public void Setup(NearbyPlayer player)
        {
            if (nameLabel != null)
                nameLabel.text = player.username;

            if (player.guardian != null)
            {
                if (typeLabel != null)
                    typeLabel.text = GetTypeEmoji(player.guardian.type);
                ApplyColor(player.guardian.type, false);
                UpdateScale(player.guardian.stats);
            }
        }

        private void ApplyColor(string type, bool isMine)
        {
            if (bodyRenderer == null) return;
            Color c = isMine ? myGuardianGlow :
                      type == "animal" ? animalColor :
                      type == "robot" ? robotColor : aircraftColor;
            bodyRenderer.material.color = c;
            bodyRenderer.material.SetColor("_EmissionColor", c * 2f);
        }

        // 총 스탯에 비례해 크기 조절 (최소 0.5, 최대 2.0)
        private void UpdateScale(GuardianStats stats)
        {
            if (stats == null) return;
            int total = stats.atk + stats.def + stats.hp;
            float scale = Mathf.Clamp(total / 200f, 0.5f, 2.0f);
            transform.localScale = Vector3.one * scale;
        }

        private string GetTypeEmoji(string type) => type switch
        {
            "animal" => "🦁",
            "robot" => "🤖",
            "aircraft" => "✈",
            _ => "?"
        };

        void Update()
        {
            // 항상 카메라를 향하도록 회전 (빌보드)
            if (Camera.main != null)
                transform.LookAt(Camera.main.transform);
        }
    }
}

using TMPro;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// AR 공간에 배치되는 수호신 박스 오브젝트.
    /// </summary>
    [RequireComponent(typeof(ARWorldAnchor))]
    public class GuardianARObject : MonoBehaviour
    {
        [SerializeField] private TextMeshPro nameLabel;
        [SerializeField] private TextMeshPro typeLabel;
        [SerializeField] private Renderer bodyRenderer;

        private static readonly Color animalColor   = new(0.2f, 0.8f, 0.2f);
        private static readonly Color robotColor    = new(0.2f, 0.6f, 1f);
        private static readonly Color aircraftColor = new(1f, 0.8f, 0.2f);
        private static readonly Color myColor       = new(1f, 0.84f, 0f);

        private ARWorldAnchor anchor;

        void Awake() => anchor = GetComponent<ARWorldAnchor>();

        void Update()
        {
            // 이름 레이블이 항상 카메라를 향하도록
            if (Camera.main != null && nameLabel != null)
                nameLabel.transform.rotation = Camera.main.transform.rotation;
        }

        public void Setup(Guardian guardian, bool isMine)
        {
            if (typeLabel != null) typeLabel.text = GetTypeEmoji(guardian.type);
            if (nameLabel != null) nameLabel.text = isMine ? "Me" : "";
            ApplyColor(isMine ? myColor : GetColor(guardian.type));
            UpdateScale(guardian.stats);
        }

        public void Setup(NearbyPlayer player)
        {
            if (nameLabel != null) nameLabel.text = player.username;
            if (player.guardian != null)
            {
                if (typeLabel != null) typeLabel.text = GetTypeEmoji(player.guardian.type);
                ApplyColor(GetColor(player.guardian.type));
                UpdateScale(player.guardian.stats);
            }
            if (player.location != null)
                anchor.SetPosition(player.location, 0f);
        }

        public void SetGPSPosition(LatLng gps, float yOffset = 0f)
            => anchor.SetPosition(gps, yOffset);

        private void ApplyColor(Color color)
        {
            if (bodyRenderer == null) return;
            bodyRenderer.material.color = color;
            if (bodyRenderer.material.HasProperty("_EmissionColor"))
                bodyRenderer.material.SetColor("_EmissionColor", color * 1.5f);
        }

        private void UpdateScale(GuardianStats stats)
        {
            if (stats == null) return;
            int total = stats.atk + stats.def + stats.hp;
            float scale = Mathf.Clamp(total / 200f, 0.5f, 2.0f);
            transform.localScale = Vector3.one * scale;
        }

        private static Color GetColor(string type) => type switch
        {
            "animal"   => animalColor,
            "robot"    => robotColor,
            "aircraft" => aircraftColor,
            _          => Color.white
        };

        private static string GetTypeEmoji(string type) => type switch
        {
            "animal"   => "A",
            "robot"    => "R",
            "aircraft" => "F",
            _          => "?"
        };
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// AR 공간에 떠있는 HP 바 — 항상 카메라를 향함 (빌보드)
    /// </summary>
    public class ARHPBar : MonoBehaviour
    {
        [SerializeField] private Image hpFill;
        [SerializeField] private TextMeshPro hpText;
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Image ultChargeBar;

        private int maxHp;

        public void Init(string guardianName, int hp, int ult = 0)
        {
            maxHp = hp;
            if (nameText != null) nameText.text = guardianName;
            SetHP(hp);
            SetUlt(ult);
        }

        public void SetHP(int current)
        {
            float ratio = maxHp > 0 ? (float)current / maxHp : 0f;
            if (hpFill != null)
            {
                hpFill.fillAmount = ratio;
                hpFill.color = Color.Lerp(Color.red, Color.green, ratio);
            }
            if (hpText != null) hpText.text = $"{current}/{maxHp}";
        }

        public void SetUlt(int charge)
        {
            if (ultChargeBar != null)
                ultChargeBar.fillAmount = charge / 100f;
        }

        void LateUpdate()
        {
            // 항상 카메라를 향하도록
            if (Camera.main != null)
                transform.LookAt(Camera.main.transform.position);
        }
    }
}

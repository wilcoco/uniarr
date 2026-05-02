using System.Collections;
using TMPro;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// 데미지 숫자 — 위로 떠오르며 페이드 아웃
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class ARDamageNumber : MonoBehaviour
    {
        private TextMeshPro label;

        public static ARDamageNumber Spawn(GameObject prefab, Vector3 worldPos, int damage, bool isCritical)
        {
            var go = Instantiate(prefab, worldPos + Vector3.up * 0.3f, Quaternion.identity);
            var dmg = go.GetComponent<ARDamageNumber>();
            dmg.Play(damage, isCritical);
            return dmg;
        }

        void Awake() => label = GetComponent<TextMeshPro>();

        public void Play(int damage, bool isCritical)
        {
            label.text = isCritical ? $"CRIT {damage}!" : damage.ToString();
            label.color = isCritical ? Color.yellow : Color.white;
            label.fontSize = isCritical ? 0.18f : 0.13f;
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            float duration = 1.4f;
            Vector3 start = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.position = start + Vector3.up * (t * 0.8f);

                // 카메라를 향하도록
                if (Camera.main != null)
                    transform.LookAt(Camera.main.transform.position);

                // 후반부 페이드
                float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                label.color = new Color(label.color.r, label.color.g, label.color.b, alpha);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

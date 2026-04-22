using System.Collections;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// 공격 이펙트 — 공격자에서 방어자로 날아가는 빔 + 충격 파티클
    /// </summary>
    public class ARAttackEffect : MonoBehaviour
    {
        [SerializeField] private LineRenderer beamLine;
        [SerializeField] private ParticleSystem impactParticle;
        [SerializeField] private ParticleSystem chargeParticle;  // 공격 준비 파티클
        [SerializeField] private ParticleSystem ultParticle;     // 궁극기 전용

        public static ARAttackEffect Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (beamLine != null) beamLine.enabled = false;
        }

        /// 일반 공격 연출
        public IEnumerator PlayAttack(Transform attacker, Transform defender, bool isCritical)
        {
            // 1. 공격자 앞에서 차지 파티클
            if (chargeParticle != null)
            {
                chargeParticle.transform.position = attacker.position;
                chargeParticle.Play();
            }
            yield return new WaitForSeconds(0.3f);

            // 2. 공격자가 방어자 쪽으로 돌진
            Vector3 originPos = attacker.position;
            Vector3 targetPos = Vector3.Lerp(attacker.position, defender.position, 0.7f);
            float dashDur = 0.25f;
            float elapsed = 0f;
            while (elapsed < dashDur)
            {
                elapsed += Time.deltaTime;
                attacker.position = Vector3.Lerp(originPos, targetPos, elapsed / dashDur);
                yield return null;
            }

            // 3. 빔 라인
            if (beamLine != null)
            {
                beamLine.enabled = true;
                beamLine.SetPosition(0, attacker.position);
                beamLine.SetPosition(1, defender.position);
                Color beamColor = isCritical ? Color.yellow : Color.cyan;
                beamLine.startColor = beamColor;
                beamLine.endColor = beamColor;
            }

            // 4. 충격 파티클
            if (impactParticle != null)
            {
                impactParticle.transform.position = defender.position;
                impactParticle.Play();
            }

            // 5. 방어자 흔들림 (화면 쉐이크 대신 오브젝트 쉐이크)
            StartCoroutine(ShakeObject(defender, 0.3f, isCritical ? 0.12f : 0.06f));

            yield return new WaitForSeconds(0.2f);

            // 6. 빔 끄고 원위치 복귀
            if (beamLine != null) beamLine.enabled = false;

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                attacker.position = Vector3.Lerp(targetPos, originPos, elapsed / 0.2f);
                yield return null;
            }
            attacker.position = originPos;
        }

        /// 궁극기 연출
        public IEnumerator PlayUltimate(Transform caster, string guardianType)
        {
            if (ultParticle != null)
            {
                ultParticle.transform.position = caster.position;
                Color c = guardianType switch
                {
                    "animal" => Color.green,
                    "robot" => Color.blue,
                    "aircraft" => Color.yellow,
                    _ => Color.white
                };
                var main = ultParticle.main;
                main.startColor = c;
                ultParticle.Play();
            }

            // 궁극기는 스케일 업 후 원복
            Vector3 originalScale = caster.localScale;
            float dur = 0.6f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float s = 1f + Mathf.Sin(elapsed / dur * Mathf.PI) * 0.5f;
                caster.localScale = originalScale * s;
                yield return null;
            }
            caster.localScale = originalScale;
        }

        private IEnumerator ShakeObject(Transform target, float duration, float magnitude)
        {
            Vector3 origin = target.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                target.localPosition = origin + new Vector3(x, y, 0f);
                yield return null;
            }
            target.localPosition = origin;
        }
    }
}

using UnityEngine;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// AR 캐릭터 — Animator 제어 + 카메라 방향 회전 + 이름 빌보드
    /// Mixamo FBX 임포트 후 이 컴포넌트를 붙이면 됨
    /// </summary>
    public class ARCharacterController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform labelRoot;   // 이름 텍스트 부모 (카메라 향함)
        [SerializeField] private TextMeshPro nameLabel;
        [SerializeField] private TextMeshPro typeLabel;

        [Header("Settings")]
        [SerializeField] private float faceSpeed = 5f;  // 카메라 방향으로 회전하는 속도

        // Animator 파라미터 이름 (Mixamo 기본 컨트롤러와 일치)
        private static readonly int HashIdle    = Animator.StringToHash("Idle");
        private static readonly int HashBattle  = Animator.StringToHash("Battle");
        private static readonly int HashVictory = Animator.StringToHash("Victory");
        private static readonly int HashDefeat  = Animator.StringToHash("Defeat");

        private bool isBattling = false;

        void Update()
        {
            FaceCamera();
            BillboardLabel();
        }

        // ─── 카메라 방향으로 부드럽게 회전 (Y축만) ────────────────────
        private void FaceCamera()
        {
            if (Camera.main == null) return;
            Vector3 dir = Camera.main.transform.position - transform.position;
            dir.y = 0;
            if (dir == Vector3.zero) return;

            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * faceSpeed);
        }

        // ─── 이름 레이블은 항상 카메라 정면 ──────────────────────────
        private void BillboardLabel()
        {
            if (labelRoot == null || Camera.main == null) return;
            labelRoot.rotation = Camera.main.transform.rotation;
        }

        // ─── 외부에서 호출 ────────────────────────────────────────────
        public void SetName(string playerName)
        {
            if (nameLabel != null) nameLabel.text = playerName;
        }

        public void SetType(string type)
        {
            if (typeLabel != null)
                typeLabel.text = type switch
                {
                    "animal"   => "ANIMAL",
                    "robot"    => "ROBOT",
                    "aircraft" => "AIR",
                    _          => "?"
                };
        }

        public void SetColor(Color color)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r.material.HasProperty("_Color"))
                    r.material.color = color;
                if (r.material.HasProperty("_EmissionColor"))
                    r.material.SetColor("_EmissionColor", color * 1.5f);
            }
        }

        public void PlayIdle()
        {
            isBattling = false;
            TrySetTrigger(HashIdle);
        }

        public void PlayBattle()
        {
            isBattling = true;
            TrySetTrigger(HashBattle);
        }

        public void PlayVictory() => TrySetTrigger(HashVictory);
        public void PlayDefeat()  => TrySetTrigger(HashDefeat);

        private void TrySetTrigger(int hash)
        {
            if (animator != null) animator.SetTrigger(hash);
        }
    }
}

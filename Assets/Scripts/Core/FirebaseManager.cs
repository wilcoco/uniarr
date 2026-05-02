using System;
using UnityEngine;
using Firebase;
using Firebase.Messaging;

namespace GuardianAR
{
    /// <summary>
    /// Firebase 초기화 + FCM 토큰 등록 + 푸시 알림 수신
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var status = task.Result;
                if (status == DependencyStatus.Available)
                {
                    InitFirebase();
                }
                else
                {
                    Debug.LogError($"[Firebase] 초기화 실패: {status}");
                }
            });
        }

        private void InitFirebase()
        {
            try
            {
                FirebaseMessaging.MessageReceived += OnMessageReceived;
                FirebaseMessaging.TokenReceived   += OnTokenReceived;

                FirebaseMessaging.GetTokenAsync().ContinueWith(task =>
                {
                    if (!task.IsFaulted && !task.IsCanceled)
                        RegisterToken(task.Result);
                });

                Debug.Log("[Firebase] 초기화 완료");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] 초기화 실패 (에디터에서는 정상): {e.Message}");
            }
        }

        private void OnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            RegisterToken(e.Token);
        }

        private void RegisterToken(string token)
        {
            Debug.Log($"[Firebase] FCM Token: {token}");

            // 로그인된 상태면 서버에 토큰 등록
            var userId = GameManager.Instance?.UserId;
            if (!string.IsNullOrEmpty(userId))
                ApiManager.Instance.RegisterFcmToken(userId, token);
            else
                // 로그인 후 등록을 위해 저장
                PlayerPrefs.SetString("pending_fcm_token", token);
        }

        // ─── 푸시 알림 수신 ───────────────────────────────────────────
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var msg  = e.Message;
            var type = msg.Data.ContainsKey("type") ? msg.Data["type"] : "";

            Debug.Log($"[Firebase] 푸시 수신: {type}");

            // 메인 스레드에서 UI 처리
            MainThreadDispatcher.Enqueue(() => HandlePush(type, msg.Data));
        }

        private void HandlePush(string type, System.Collections.Generic.IDictionary<string, string> data)
        {
            switch (type)
            {
                case "ATTACK_RESULT":
                    HUD.Instance?.ShowNotification("You were attacked! Check battle result.");
                    break;

                case "ALLIANCE_REQUEST":
                    var requestId   = data.ContainsKey("requestId")   ? data["requestId"]   : "";
                    var requesterId = data.ContainsKey("requesterId")  ? data["requesterId"] : "";
                    HUD.Instance?.ShowAllianceRequest(requestId, requesterId);
                    break;

                case "ALLIANCE_ACCEPTED":
                    HUD.Instance?.ShowNotification("Alliance formed!");
                    break;

                case "ALLIANCE_DECLINED":
                    HUD.Instance?.ShowNotification("Alliance request declined.");
                    break;
            }
        }

        void OnDestroy()
        {
            try
            {
                FirebaseMessaging.MessageReceived -= OnMessageReceived;
                FirebaseMessaging.TokenReceived   -= OnTokenReceived;
            }
            catch { /* Firebase 미초기화 시 무시 */ }
        }
    }
}

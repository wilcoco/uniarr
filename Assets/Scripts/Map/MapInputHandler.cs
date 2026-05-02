using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GuardianAR
{
    /// <summary>
    /// 맵 터치 입력 — 1손가락 드래그(패닝), 2손가락 핀치(줌)
    /// New Input System 사용
    /// </summary>
    public class MapInputHandler : MonoBehaviour
    {
        [SerializeField] private MapTileManager tileManager;
        [SerializeField] private RectTransform tileContainer;
        [SerializeField] private RectTransform markerContainer;

        private bool isDragging;
        private Vector2 dragStartTouchPos;
        private Vector2 containerBasePos;

        private bool isPinching;
        private float lastPinchDistance;
        private int zoomCooldown;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
#if UNITY_EDITOR
            HandleMouse();
#else
            HandleTouch();
#endif
        }

        // ─── 에디터 마우스 (New Input System) ────────────────────────
        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
                BeginDrag(mousePos);
            else if (mouse.leftButton.isPressed && isDragging)
                UpdateDrag(mousePos);
            else if (mouse.leftButton.wasReleasedThisFrame && isDragging)
                EndDrag();

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f) tileManager.ChangeZoom(scroll > 0 ? 1 : -1);
        }

        // ─── 모바일 터치 (Enhanced Touch) ────────────────────────────
        private void HandleTouch()
        {
            var touches = Touch.activeTouches;

            if (touches.Count == 1)
            {
                isPinching = false;
                var t = touches[0];
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    BeginDrag(t.screenPosition);
                else if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    UpdateDrag(t.screenPosition);
                else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                         t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    if (isDragging) EndDrag();
            }
            else if (touches.Count == 2)
            {
                isDragging = false;
                HandlePinch(touches[0].screenPosition, touches[1].screenPosition);
            }
            else
            {
                isPinching = false;
            }
        }

        // ─── 드래그 ───────────────────────────────────────────────────
        private void BeginDrag(Vector2 pos)
        {
            isDragging = true;
            dragStartTouchPos = pos;
            containerBasePos  = tileContainer.anchoredPosition;
        }

        private void UpdateDrag(Vector2 pos)
        {
            Vector2 delta = pos - dragStartTouchPos;
            tileContainer.anchoredPosition   = containerBasePos + delta;
            markerContainer.anchoredPosition = containerBasePos + delta;
        }

        private void EndDrag()
        {
            isDragging = false;
            Vector2 offset = tileContainer.anchoredPosition;
            if (offset.magnitude < 5f) { ResetContainers(); return; }

            var center = tileManager.MapCenter;
            if (center == null) { ResetContainers(); return; }

            float mpp = tileManager.MetersPerPixel;
            double dLat =  offset.y * mpp / 111320.0;
            double dLng = -offset.x * mpp / (111320.0 * Math.Cos(center.lat * Math.PI / 180.0));

            ResetContainers();
            tileManager.CenterOn(new LatLng(center.lat + dLat, center.lng + dLng));
        }

        // ─── 핀치 줌 ─────────────────────────────────────────────────
        private void HandlePinch(Vector2 pos0, Vector2 pos1)
        {
            float dist = Vector2.Distance(pos0, pos1);

            if (!isPinching) { lastPinchDistance = dist; isPinching = true; return; }
            if (zoomCooldown > 0) { zoomCooldown--; return; }

            float delta = dist - lastPinchDistance;
            lastPinchDistance = dist;

            if (Mathf.Abs(delta) > 30f)
            {
                tileManager.ChangeZoom(delta > 0 ? 1 : -1);
                zoomCooldown = 20;
            }
        }

        private void ResetContainers()
        {
            tileContainer.anchoredPosition   = Vector2.zero;
            markerContainer.anchoredPosition = Vector2.zero;
        }
    }
}

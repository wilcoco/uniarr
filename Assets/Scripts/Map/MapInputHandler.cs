using System;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// 맵 터치 입력 — 1손가락 드래그(패닝), 2손가락 핀치(줌)
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

        void Update()
        {
#if UNITY_EDITOR
            HandleMouse();
#else
            HandleTouch();
#endif
        }

        // ─── 에디터 마우스 ────────────────────────────────────────────
        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))   BeginDrag(Input.mousePosition);
            else if (Input.GetMouseButton(0) && isDragging) UpdateDrag(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0) && isDragging) EndDrag();

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f) tileManager.ChangeZoom(scroll > 0 ? 1 : -1);
        }

        // ─── 모바일 터치 ─────────────────────────────────────────────
        private void HandleTouch()
        {
            if (Input.touchCount == 1)
            {
                isPinching = false;
                var t = Input.GetTouch(0);
                switch (t.phase)
                {
                    case TouchPhase.Began:    BeginDrag(t.position);  break;
                    case TouchPhase.Moved:    UpdateDrag(t.position); break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled: if (isDragging) EndDrag(); break;
                }
            }
            else if (Input.touchCount == 2)
            {
                isDragging = false;
                HandlePinch();
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

            // 픽셀 → GPS 오프셋
            var center = tileManager.MapCenter;
            if (center == null) { ResetContainers(); return; }

            float mpp = tileManager.MetersPerPixel;
            double dLat =  offset.y * mpp / 111320.0;
            double dLng = -offset.x * mpp / (111320.0 * Math.Cos(center.lat * Math.PI / 180.0));

            ResetContainers();
            tileManager.CenterOn(new LatLng(center.lat + dLat, center.lng + dLng));
        }

        // ─── 핀치 줌 ─────────────────────────────────────────────────
        private void HandlePinch()
        {
            float dist = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);

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

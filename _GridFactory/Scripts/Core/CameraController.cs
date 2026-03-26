using System;
using System.Collections.Generic;


using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using GridFactory.Utils;

namespace GridFactory.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Camera")]
        [SerializeField] private Camera cam;

        [Header("Zoom Settings")]
        [SerializeField] private float minSize = 4.5f;
        [SerializeField] private float maxSize = 7f;

        [Tooltip("Zoom Faktor")]
        [SerializeField] private float mouseZoomSpeed = 0.2f;
        [SerializeField] private float touchZoomSpeed = 0.02f;
        [SerializeField] private float keyboardZoomSpeed = 2f;

        [Header("Pan Settings")]
        [SerializeField] private int mousePanButton = 2;
        [SerializeField] private float panSpeed = 1f;
        [SerializeField] private float keyboardPanSpeed = 10f;

        [Header("Bounds")]
        [SerializeField] private Collider2D movementBounds;

        private Vector3 _startPosition;
        private Vector3 _lastPanWorld;
        private Vector2 _panStartScreenPos;
        private float _startOrthoSize;
        private float _previousPinchDistance = 0f;
        private float panPlaneZ = 0f;
        private bool _isPanning;
        private int _panFingerId = -1;

        public Action _onZoom; // f
        public Action<bool> _onPan;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (cam == null)
                cam = GetComponent<Camera>();

            _startPosition = cam.transform.position;
            _startOrthoSize = cam.orthographicSize;
        }

        private void Update()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            HandleMousePan();
            HandleMouseZoom();

#else
            HandleTouchPan();
            HandleTouchZoom();
#endif
            HandleKeyboardZoom();
            HandleKeyboardPan();
        }

        public void ResetCamera()
        {
            if (cam == null)
                return;

            cam.orthographicSize = _startOrthoSize;
            cam.transform.position = _startPosition;
            _isPanning = false;
            _panFingerId = -1;
            _previousPinchDistance = 0f;

            ClampCameraToBounds();
        }

        // -------------------
        // DESKTOP / MOUSE PAN
        // -------------------
        private void HandleMousePan()
        {
            if (Input.GetMouseButtonDown(mousePanButton))
            {
                if (IsPointerOverUI_Standalone())
                    return;
                _isPanning = true;
                _panStartScreenPos = Input.mousePosition;
                _lastPanWorld = ScreenToWorldOnPlane(Input.mousePosition);
                return;
            }

            if (_isPanning && Input.GetMouseButton(mousePanButton))
            {
                Vector3 currentWorld = ScreenToWorldOnPlane(Input.mousePosition);
                Vector3 delta = _lastPanWorld - currentWorld;

                cam.transform.position += delta * panSpeed;

                ClampCameraToBounds();

                _lastPanWorld = ScreenToWorldOnPlane(Input.mousePosition);
                _onPan?.Invoke(false);
                return;
            }

            if (Input.GetMouseButtonUp(mousePanButton))
            {
                _isPanning = false;
                float dist = Vector2.Distance(_panStartScreenPos, Input.mousePosition);
                if (dist >= TutorialGridFactoryController.Instance.minDistancePanPixel)
                    _onPan?.Invoke(true);
            }
        }

        private void HandleKeyboardPan()
        {
            float h = 0f;
            float v = 0f;

            if (Input.GetKey(KeyCode.LeftArrow))
                h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow))
                h += 1f;
            if (Input.GetKey(KeyCode.DownArrow))
                v -= 1f;
            if (Input.GetKey(KeyCode.UpArrow))
                v += 1f;

            Vector3 move = new Vector3(h, v, 0f);

            if (move.sqrMagnitude <= 0f)
                return;

            move = move.normalized;
            _panStartScreenPos = cam.transform.position;
            cam.transform.position += move * keyboardPanSpeed * Time.deltaTime;

            ClampCameraToBounds();

            float dist = Vector2.Distance(_panStartScreenPos, Input.mousePosition);
            if (dist >= TutorialGridFactoryController.Instance.minDistancePanPixel)
                _onPan?.Invoke(true);
        }

        private void HandleMouseZoom()
        {
            if (IsPointerOverUI_Standalone())
                return;

            float scroll = 0f;
            scroll = Input.mouseScrollDelta.y;

#if ENABLE_INPUT_SYSTEM
            // Neues Input System fallback (falls Legacy 0 liefert)
            if (Mathf.Approximately(scroll, 0f) && Mouse.current != null)
                scroll = Mouse.current.scroll.ReadValue().y;
#endif
            if (Mathf.Approximately(scroll, 0f))
                return;

            float zoomAmount = scroll * mouseZoomSpeed;

            ApplyZoom(zoomAmount);
            ClampCameraToBounds();
        }

        private void HandleTouchPan()
        {
            if (Input.touchCount >= 2) // Pinch -> nicht pannen
                return;

            if (Input.touchCount == 1)
            {
                Touch t = Input.GetTouch(0);

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                    return;
                if (UIUtils.ClickedOnUi())
                    return;

                if (t.phase == UnityEngine.TouchPhase.Began)
                {
                    _isPanning = true;
                    _panFingerId = t.fingerId;
                    _panStartScreenPos = t.position;
                    _lastPanWorld = ScreenToWorldOnPlane(t.position);
                    return;
                }

                if (_isPanning && t.fingerId == _panFingerId &&
                    (t.phase == UnityEngine.TouchPhase.Moved || t.phase == UnityEngine.TouchPhase.Stationary))
                {
                    Vector3 currentWorld = ScreenToWorldOnPlane(t.position);
                    Vector3 delta = _lastPanWorld - currentWorld;

                    cam.transform.position += delta * panSpeed;

                    ClampCameraToBounds();

                    _lastPanWorld = ScreenToWorldOnPlane(t.position);
                    _onPan?.Invoke(false);
                    return;
                }

                if (t.phase == UnityEngine.TouchPhase.Ended || t.phase == UnityEngine.TouchPhase.Canceled)
                {
                    _isPanning = false;
                    _panFingerId = -1;

                    float dist = Vector2.Distance(_panStartScreenPos, t.position);
                    if (dist >= TutorialGridFactoryController.Instance.minDistancePanPixel)
                        _onPan?.Invoke(true);
                }
                return;
            }
            _isPanning = false;
            _panFingerId = -1;
        }

        private void HandleKeyboardZoom()
        {
            float zoom = 0f;

            if (Input.GetKey(KeyCode.Comma))
                zoom -= keyboardZoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.Period))
                zoom += keyboardZoomSpeed * Time.deltaTime;

            if (Mathf.Approximately(zoom, 0f))
                return;

            ApplyZoom(zoom);
            ClampCameraToBounds();
        }

        private void HandleTouchZoom()
        {
            if (Input.touchCount >= 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                Vector2 p0 = t0.position;
                Vector2 p1 = t1.position;

                float distance = Vector2.Distance(p0, p1);

                if (_previousPinchDistance <= 0f)
                {
                    _previousPinchDistance = distance;
                    return;
                }

                float delta = distance - _previousPinchDistance;
                _previousPinchDistance = distance;

                if (Mathf.Approximately(delta, 0f))
                    return;

                float zoomAmount = delta * touchZoomSpeed;

                ApplyZoom(zoomAmount);
                ClampCameraToBounds();

                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var ts = Touchscreen.current;

                if (ts.touches.Count < 2)
                {
                    _previousPinchDistance = 0f;
                    return;
                }

                var t0 = ts.touches[0];
                var t1 = ts.touches[1];

                if (!t0.isInProgress || !t1.isInProgress)
                {
                    _previousPinchDistance = 0f;
                    return;
                }

                Vector2 p0 = t0.position.ReadValue();
                Vector2 p1 = t1.position.ReadValue();

                float distance = Vector2.Distance(p0, p1);

                if (_previousPinchDistance <= 0f)
                {
                    _previousPinchDistance = distance;
                    return;
                }

                float delta = distance - _previousPinchDistance;
                _previousPinchDistance = distance;

                if (Mathf.Approximately(delta, 0f))
                    return;

                float zoomAmount = delta * touchZoomSpeed;

                ApplyZoom(zoomAmount);
                ClampCameraToBounds();

                return;
            }
#endif
            _previousPinchDistance = 0f;
        }

        private void ApplyZoom(float zoomAmount)
        {
            if (cam == null)
                return;

            Vector2 screenPoint;

            screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            _onZoom?.Invoke();
            ApplyZoomAroundScreenPoint(zoomAmount, screenPoint);
        }

        private void ApplyZoomAroundScreenPoint(float zoomAmount, Vector2 screenPoint)
        {
            float oldSize = cam.orthographicSize;

            float dynamicMax = Mathf.Min(maxSize, GetMaxOrthoSizeThatFitsBounds());
            float targetSize = Mathf.Clamp(oldSize - zoomAmount, minSize, dynamicMax);

            if (Mathf.Approximately(oldSize, targetSize))
                return;

            float depth = Mathf.Abs(cam.transform.position.z);

            Vector3 before = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));

            cam.orthographicSize = targetSize;

            Vector3 after = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));

            cam.transform.position += before - after;
        }

        private bool IsPointerOverUI_Standalone()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {

                    GameObject topGO = results[0].gameObject;
                    if (topGO.name.Contains("tutorialClickBlocker"))
                        return false;

                }
                return true;
            }
            if (UIUtils.ClickedOnUi())
                return true;

            return false;
        }

        private Vector3 ScreenToWorldOnPlane(Vector2 screenPos)
        {
            Ray r = cam.ScreenPointToRay(screenPos);
            Plane p = new Plane(Vector3.forward, new Vector3(0f, 0f, panPlaneZ)); // XY-Ebene bei Z=panPlaneZ

            if (p.Raycast(r, out float enter))
                return r.GetPoint(enter);

            // Fallback (sollte praktisch nie passieren)
            float depth = Mathf.Abs(cam.transform.position.z - panPlaneZ);
            return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        }

        private void ClampCameraToBounds()
        {
            if (movementBounds == null || cam == null)
                return;

            Bounds b = movementBounds.bounds;

            float vertExtent = cam.orthographicSize;
            float horzExtent = vertExtent * cam.aspect;

            Vector3 pos = cam.transform.position;

            float minX = b.min.x + horzExtent;
            float maxX = b.max.x - horzExtent;
            float minY = b.min.y + vertExtent;
            float maxY = b.max.y - vertExtent;

            pos.x = (minX > maxX) ? b.center.x : Mathf.Clamp(pos.x, minX, maxX);
            pos.y = (minY > maxY) ? b.center.y : Mathf.Clamp(pos.y, minY, maxY);

            cam.transform.position = pos;
        }

        private float GetMaxOrthoSizeThatFitsBounds()
        {
            if (movementBounds == null || cam == null)
                return maxSize;

            Bounds b = movementBounds.bounds;

            float halfBoundsH = b.size.y * 0.5f;
            float halfBoundsW = b.size.x * 0.5f;
            float maxByHeight = halfBoundsH;
            float maxByWidth = halfBoundsW / cam.aspect;

            return Mathf.Min(maxByHeight, maxByWidth);
        }

    }
}

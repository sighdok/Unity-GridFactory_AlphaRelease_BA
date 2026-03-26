using UnityEngine;

public class WorldAnchoredPanel : MonoBehaviour
{
    [Header("References")]
    public Canvas canvas;              // dein Haupt-Canvas (Screen Space Overlay)
    public Camera worldCamera;         // die Kamera, die auf dein Grid schaut
    public RectTransform panelRect;    // RectTransform dieses Panels

    [Header("Settings")]
    public float margin = 10f;         // Abstand zum Rand
    public float offset = 10f;         // Abstand zum Objekt (zusätzlich)

    private Transform _target;         // z.B. PortMarker-Transform

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();
    }

    public void SetTarget(Transform target)
    {
        _target = target;

    }

    private void LateUpdate()
    {
        if (_target == null || canvas == null || panelRect == null)
            return;

        // 1) Weltposition -> Screenposition
        Vector3 worldPos = _target.position;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // Wenn Objekt hinter der Kamera ist -> UI ausblenden
        if (screenPos.z < 0f)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!panelRect.gameObject.activeSelf)
                panelRect.gameObject.SetActive(true);
        }

        // 2) Panel-Größe berücksichtigen
        //    (optional: falls Layouts im Spiel sind, kann man ein Rebuild erzwingen)
        float panelHeight = panelRect.rect.height;

        // Wieviel Platz ist nach oben/unten?
        float spaceAbove = Screen.height - screenPos.y;
        float spaceBelow = screenPos.y;

        // Default: oberhalb
        bool placeAbove = true;

        // Wenn nach oben nicht genug Platz für das Panel ist,
        // aber nach unten mehr Platz ist, dann nach unten verschieben.
        if (spaceAbove < panelHeight + margin && spaceBelow > spaceAbove)
        {
            placeAbove = false;
        }

        // 3) Screenposition leicht nach oben/unten verschieben
        if (placeAbove)
        {
            screenPos.y += (panelHeight * 0.5f) + offset;
        }
        else
        {
            screenPos.y -= (panelHeight * 0.5f) + offset;
        }

        // 4) Screenposition -> Canvas-Lokale Position
        RectTransform canvasRect = canvas.transform as RectTransform;

        Vector2 localPos;
        // Bei ScreenSpace-Overlay ist camera-Parameter = null
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out localPos
        );

        // 5) Panel-Position setzen
        panelRect.anchoredPosition = localPos;
    }
}

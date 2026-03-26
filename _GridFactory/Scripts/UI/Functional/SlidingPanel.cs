using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class SlidingPanel : MonoBehaviour
{
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    [Header("Setup")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private SlideDirection direction = SlideDirection.Left;
    [SerializeField] private float animationTime = 0.25f;
    [SerializeField] private bool startShown = true;

    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private bool isShown;
    private Coroutine animCoroutine;
    private bool positionsCached = false;

    // Globale Liste aller Panels dieses Typs
    private static readonly List<SlidingPanel> allPanels = new List<SlidingPanel>();

    private void Awake()
    {
        if (panel == null)
            panel = GetComponent<RectTransform>();

        CachePositions();
    }

    private void OnEnable()
    {
        if (!allPanels.Contains(this))
            allPanels.Add(this);

        // Startzustand setzen
        if (startShown)
        {
            isShown = true;
            panel.anchoredPosition = shownPos;
        }
        else
        {
            isShown = false;
            panel.anchoredPosition = hiddenPos;
        }
    }

    private void OnDisable()
    {
        allPanels.Remove(this);
    }

    private void CachePositions()
    {
        if (positionsCached || panel == null)
            return;

        // Sichtbare Position = aktuelle Position im Editor
        shownPos = panel.anchoredPosition;

        Vector2 offset = Vector2.zero;
        Vector2 size = panel.rect.size;

        switch (direction)
        {
            case SlideDirection.Left:
                offset = new Vector2(-size.x, 0f);
                break;
            case SlideDirection.Right:
                offset = new Vector2(size.x, 0f);
                break;
            case SlideDirection.Up:
                offset = new Vector2(0f, size.y);
                break;
            case SlideDirection.Down:
                offset = new Vector2(0f, -size.y);
                break;
        }

        hiddenPos = shownPos + offset;
        positionsCached = true;
    }

    // ---------- API für Button / Events ----------

    public void TogglePanel()
    {
        if (!isShown)
        {
            //CloseAllExcept(this);
        }

        isShown = !isShown;
        Vector2 target = isShown ? shownPos : hiddenPos;
        StartMove(target);
    }

    public void Open()
    {
        if (isShown) return;

        //CloseAllExcept(this);
        isShown = true;
        StartMove(shownPos);
    }

    public void Close()
    {
        if (!isShown) return;

        isShown = false;
        StartMove(hiddenPos);
    }

    public void QuickClose()
    {
        if (!isShown) return;

        isShown = false;
        StartCoroutine(AnimatePanel(panel.anchoredPosition, hiddenPos, 0.01f));
    }

    private static void CloseAllExcept(SlidingPanel exception)
    {
        foreach (var p in allPanels)
        {
            if (p != null && p != exception)
            {
                p.Close();
            }
        }

    }

    private void StartMove(Vector2 target)
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(AnimatePanel(panel.anchoredPosition, target, animationTime));
    }

    private IEnumerator AnimatePanel(Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            panel.anchoredPosition = Vector2.Lerp(from, to, lerp);
            yield return null;
        }

        panel.anchoredPosition = to;
        animCoroutine = null;
    }
}

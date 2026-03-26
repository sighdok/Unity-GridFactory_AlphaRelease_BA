
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

using GridFactory.Core;

namespace GridFactory.Utils
{
    public static class UIUtils
    {
        public static bool ClickedOnUi()
        {
            var currentControlScheme = GameManager.Instance.CurrentControlScheme;
            if (currentControlScheme == "desktop")
                return false;

            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = Input.mousePosition;

            if (Input.touchCount > 0)
                eventDataCurrentPosition.position = Input.GetTouch(0).position;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            foreach (var item in results)
            {
                if (item.gameObject.CompareTag("UI"))
                    return true;
            }

            return false;
        }
    }
}
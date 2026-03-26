using UnityEngine;

namespace GridFactory.UI
{

    // TODO: DELETE ME
    public class RequireAssignUI : MonoBehaviour
    {
        [SerializeField] private Object[] required;

        private void Awake()
        {
            if (required == null) return;

            for (int i = 0; i < required.Length; i++)
            {
                if (required[i] == null)
                    Debug.LogWarning($"[UIRequireAssign] Missing reference on {name} at index {i}", this);
            }
        }
    }
}

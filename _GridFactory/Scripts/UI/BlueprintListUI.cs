using UnityEngine;

using GridFactory.Blueprints;

namespace GridFactory.UI
{
    public class BlueprintListUI : MonoBehaviour
    {
        public static BlueprintListUI Instance { get; private set; }
        [Header("References")]
        [SerializeField] private RectTransform contentArea; // ScrollView/Content
        [SerializeField] private GameObject blueprintButtonPrefab;

        [Header("Placement")]
        [Tooltip("Standard-Position, an der der Blueprint im Grid platziert wird.")]
        public Vector2Int defaultAnchor = new Vector2Int(0, 0);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }


        public void RefreshList()
        {
            foreach (Transform child in contentArea)
                Destroy(child.gameObject);

            foreach (var bp in BlueprintManager.Instance.runtimeBlueprints)
                CreateButton(bp);
        }

        private void CreateButton(BlueprintDefinition bp)
        {
            GameObject btnObj = Instantiate(blueprintButtonPrefab, contentArea);
            btnObj.GetComponent<BlueprintButtonController>().Init(bp);
        }
    }
}

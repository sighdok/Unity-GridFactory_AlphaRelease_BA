using UnityEngine;

using GridFactory.Blueprints;

namespace GridFactory.UI
{
    public class BlueprintListUI : MonoBehaviour
    {
        public static BlueprintListUI Instance { get; private set; }

        private static BlueprintManager BPM => BlueprintManager.Instance;

        [SerializeField] private RectTransform contentArea;
        [SerializeField] private GameObject blueprintButtonPrefab;

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

            foreach (var bp in BPM.runtimeBlueprints)
                CreateButton(bp);
        }

        private void CreateButton(BlueprintDefinition bp)
        {
            GameObject btnObj = Instantiate(blueprintButtonPrefab, contentArea);
            btnObj.GetComponent<BlueprintButtonController>().Init(bp);
        }
    }
}

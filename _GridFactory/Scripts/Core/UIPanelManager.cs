using Esper.SkillWeb.UI.UGUI;
using GridFactory.Blueprints;
using GridFactory.Core;
using GridFactory.Grid;
using GridFactory.Meta;
using GridFactory.Tech;
using GridFactory.UI;
using Unity.VisualScripting;
using UnityEngine;

public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager Instance { get; private set; }

    private static UIConfigurationManager UICONFIGM => UIConfigurationManager.Instance;

    [SerializeField] private BlueprintUI blueprintUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private TechTreeUI techtreeUI;
    //[SerializeField] private BlueprintImportExportWindow importExportUI;
    [SerializeField] private SettingsUI settingsUI;
    [SerializeField] private SlidingPanel topSlidingPanel;
    [SerializeField] private BlueprintInfoPanel blueprintInfoPanel;
    [SerializeField] private UIConfirmationManager confirmationManager;
    [SerializeField] private UIConfigurationManager configurationManager;
    [SerializeField] private SkillHovercardUGUI techTreeHovercard;
    [SerializeField] private MetaBuildMenuUI metaBuildUI;
    [SerializeField] private BuildMenuUI buildUI;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ToggleBlueprintUI()
    {
        if (blueprintUI.IsOpen)
            blueprintUI.Close();
        else
        {
            blueprintUI.Open();
            shopUI.Close();
            techtreeUI.Close();
            settingsUI.Close();
            //importExportUI.Close();
            confirmationManager.Close();
            configurationManager.Close();

            CloseAllBuildTabs();
            topSlidingPanel.QuickClose();
        }
    }

    public void ToggleShopUI()
    {
        if (shopUI.IsOpen)
            shopUI.Close();
        else
        {
            shopUI.Open();
            blueprintUI.Close();
            techtreeUI.Close();
            settingsUI.Close();
            //importExportUI.Close();
            confirmationManager.Close();
            configurationManager.Close();

            CloseAllBuildTabs();
            topSlidingPanel.QuickClose();
        }
    }

    /*
        public void ToggleImportExportWindow()
        {
            if (importExportUI.IsOpen)
                importExportUI.Close();
            else
            {
                importExportUI.Open(BlueprintManager.Instance.CreateBlueprintFromCurrentGrid("BP_{System.DateTime.Now:HHmmss}", null, false));
                shopUI.Close();
                blueprintUI.Close();
                techtreeUI.Close();
                settingsUI.Close();
                confirmationManager.Close();

                CloseAllBuildTabs();
                topSlidingPanel.QuickClose();
            }
        }
    */

    public void ToggleSettingsWindow()
    {
        if (settingsUI.IsOpen)
            settingsUI.Close();
        else
        {
            settingsUI.Open();
            shopUI.Close();
            blueprintUI.Close();
            techtreeUI.Close();
            //importExportUI.Close();
            confirmationManager.Close();
            configurationManager.Close();

            CloseAllBuildTabs();
            topSlidingPanel.QuickClose();
        }
    }

    public void ToggleTechTreeUI(MetaResearchCenter selectedCenter = null, bool forceClose = false)
    {
        if (techtreeUI.IsOpen || forceClose)
        {
            techTreeHovercard.ForceClose();
            techtreeUI.Close();
        }
        else
        {
            if (selectedCenter != null)
                TechTreeManager.Instance.SelectedCenter = selectedCenter;

            techtreeUI.Open();
            blueprintUI.Close();
            shopUI.Close();
            settingsUI.Close();
            //importExportUI.Close();
            confirmationManager.Close();
            configurationManager.Close();

            CloseAllBuildTabs();
            topSlidingPanel.QuickClose();
        }
    }

    public void OpenTechTreeUI(MetaResearchCenter selectedCenter)
    {
        blueprintUI.Close();
        shopUI.Close();
        settingsUI.Close();
        //importExportUI.Close();
        confirmationManager.Close();
        configurationManager.Close();

        CloseAllBuildTabs();
        topSlidingPanel.QuickClose();

        TechTreeManager.Instance.SelectedCenter = selectedCenter;
        techtreeUI.Open();
    }


    public void CloseAllConfigPanels()
    {
        UICONFIGM.Close();
    }

    public void CloseAll()
    {
        blueprintUI.Close();
        shopUI.Close();
        settingsUI.Close();
        //importExportUI.Close();
        confirmationManager.Close();
        techtreeUI.Close();
    }

    public void CloseMetaBuildTabs()
    {
        metaBuildUI.CloseAllSubs();
    }

    public void CloseMicroBuildTabs()
    {
        buildUI.CloseAllSubs();
    }

    public void CloseAllBuildTabs()
    {
        CloseMetaBuildTabs();
        CloseMicroBuildTabs();
    }
}

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace MC_SVManageBP
{
    internal class ManageBPUI
    {
        private enum ConfirmAction { save, delete }

        private const string confirmSave = "Overwrite existing blueprint BPNAME?";
        private const string confirmDelete = "Delete blueprint BPNAME?";
        private const int listItemSpacing = 20;
        private const int noneSelectedIndex = -2;
        private const int newBPIndex = -1;

        private GameObject mainPanelTemplate;
        private GameObject confirmDialogTemplate;
        private GameObject listItemTemplate;
        private GameObject manageBPButtonTemplate;

        private GameObject mainPanel;
        private Text selectedBPName;
        private Transform savedBPList;
        private int selectedIndex = -2;
        private Transform selectedBPContent;
        private GameObject currentHighlight = null;
        private int coreFilter = noneSelectedIndex;

        private GameObject confirmDialog;
        private Text confirmDialogText;
        private Button confirmDialogConfirmBtn;
        private ButtonClickedEvent confirmDialogSaveEvent;
        private ButtonClickedEvent confirmDialogDeleteEvent;
        
        private GameObject manageBPButton;
        private WeaponCrafting weaponCrafting;        

        internal ManageBPUI(GameObject mainPanel, GameObject confirmDialog, GameObject listItem, GameObject manageBPButton)
        {
            mainPanelTemplate = mainPanel;
            confirmDialogTemplate = confirmDialog;
            listItemTemplate = listItem;
            this.manageBPButtonTemplate = manageBPButton;
        }

        internal void Initialise(GameObject parentUI)
        {
            // Manage bp button
            Transform resultTransform = parentUI.transform.Find("Result");
            Transform buildButton = resultTransform.Find("BtnBuild");
            manageBPButton = GameObject.Instantiate(manageBPButtonTemplate);
            manageBPButton.transform.SetParent(resultTransform);
            RectTransform manageBPButtonRect = manageBPButton.GetComponent<RectTransform>();
            manageBPButton.transform.localPosition = new Vector3(
                buildButton.localPosition.x + (manageBPButtonRect.rect.width * 2),
                buildButton.localPosition.y,
                buildButton.localPosition.z);
            manageBPButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buildButton.gameObject.GetComponent<RectTransform>().rect.height);
            manageBPButton.transform.localScale = Vector3.one;
            ButtonClickedEvent manageBPBCE = new ButtonClickedEvent();
            manageBPBCE.AddListener(ManageButton_Click);
            manageBPButton.GetComponent<Button>().onClick = manageBPBCE;

            // Main panel
            mainPanel = GameObject.Instantiate(mainPanelTemplate);
            mainPanel.transform.SetParent(parentUI.transform, false);
            Transform actualMainPanel = mainPanel.transform.Find("mc_savebpPanel");
            mainPanel.SetActive(false);

            // References            
            savedBPList = actualMainPanel.Find("Left").Find("mc_savebpBPList").GetChild(0).GetChild(0);
            selectedBPName = actualMainPanel.Find("Right").Find("mc_savebpSelectedBPName").gameObject.GetComponent<Text>();
            selectedBPContent = actualMainPanel.Find("Right").Find("mc_savebpSelectedBPContent").GetChild(0).GetChild(0);

            // Core filter            
            Dropdown coreFilterList = actualMainPanel.Find("Left").Find("mc_savebpCoreFilter").gameObject.GetComponent<Dropdown>();
            foreach (WeaponComponent wepComponent in AccessTools.StaticFieldRefAccess<List<WeaponComponent>>(typeof(Crafting), "weaponComponents"))
            {
                if (wepComponent.componentName.ToLower().Contains("core"))
                {
                    PersistentData.Blueprint.coreIds.Add(wepComponent.id);
                    coreFilterList.options.Add(new Dropdown.OptionData(wepComponent.componentName));
                }
            }

            Dropdown.DropdownEvent filterChangeEvent = new Dropdown.DropdownEvent();
            filterChangeEvent.AddListener(new UnityAction<int>(delegate (int i)
            {
                if (i == 0)
                    coreFilter = noneSelectedIndex;
                else
                    coreFilter = PersistentData.Blueprint.coreIds[i - 1];

                RefreshSavedBPList();
                RefreshSelectedBPContent();
            }));
            coreFilterList.onValueChanged = filterChangeEvent;

            // Delete button
            ButtonClickedEvent deleteBCE = new ButtonClickedEvent();
            deleteBCE.AddListener(DeleteButton_Click);
            actualMainPanel.Find("mc_savebpDelete").gameObject.GetComponent<Button>().onClick = deleteBCE;

            // Load button
            ButtonClickedEvent loadBCE = new ButtonClickedEvent();
            loadBCE.AddListener(LoadButton_Click);
            actualMainPanel.Find("mc_savebpLoad").gameObject.GetComponent<Button>().onClick = loadBCE;

            // Save button
            ButtonClickedEvent saveBCE = new ButtonClickedEvent();
            saveBCE.AddListener(SaveButton_Click);
            actualMainPanel.Find("mc_savebpSave").gameObject.GetComponent<Button>().onClick = saveBCE;

            // Cancel button
            ButtonClickedEvent cancelBCE = new ButtonClickedEvent();
            cancelBCE.AddListener(CancelButton_Click);
            actualMainPanel.Find("mc_savebpCancel").gameObject.GetComponent<Button>().onClick = cancelBCE;

            // Confirmation panel
            confirmDialog = GameObject.Instantiate(confirmDialogTemplate);
            confirmDialog.transform.SetParent(parentUI.transform, false);
            confirmDialogText = confirmDialog.transform.Find("mc_savebpPanel").Find("mc_savebpMessage").gameObject.GetComponent<Text>();
            confirmDialog.SetActive(false);

            // Cancel button
            ButtonClickedEvent confirmDlgCancelBCE = new ButtonClickedEvent();
            confirmDlgCancelBCE.AddListener(ConfirmDialogCancelButton_Click);
            confirmDialog.transform.Find("mc_savebpPanel").Find("mc_savebpCancel").gameObject.GetComponent<Button>().onClick = confirmDlgCancelBCE;

            // Confirm button
            confirmDialogConfirmBtn = confirmDialog.transform.Find("mc_savebpPanel").Find("mc_savebpConfirm").gameObject.GetComponent<Button>();
            confirmDialogDeleteEvent = new ButtonClickedEvent();
            confirmDialogDeleteEvent.AddListener(ConfirmDialogDeleteButton_Click);
            confirmDialogSaveEvent = new ButtonClickedEvent();
            confirmDialogSaveEvent.AddListener(ConfirmDialogSaveButton_Click);
        }

        internal void CloseAll()
        {
            if(mainPanel != null)
                mainPanel.SetActive(false);
            
            if(confirmDialog != null)
                confirmDialog.SetActive(false);

            SetActiveManageBPBtn(null, false);
        }

        internal void SetActiveManageBPBtn(WeaponCrafting weaponCrafting, bool state)
        {
            if (manageBPButton == null)
            {
                if (state)
                {
                    this.weaponCrafting = weaponCrafting;
                    Initialise(AccessTools.FieldRefAccess<GameObject>(typeof(WeaponCrafting), "MainPanel")(weaponCrafting));
                }
                else
                    return;
            }

            manageBPButton.SetActive(state);
        }

        private void OpenMainPanel()
        {
            if (mainPanel == null)
                return;

            mainPanel.SetActive(true);
            RefreshSavedBPList();
            RefreshSelectedBPContent();
        }

        private void RefreshSavedBPList()
        {
            for (int i = 0; i < savedBPList.childCount; i++)
                GameObject.Destroy(savedBPList.GetChild(i).gameObject);

            selectedIndex = noneSelectedIndex;

            // Set content game object size
            savedBPList.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listItemSpacing * (Main.data.blueprints.Count + 1));

            // "New..." item
            GameObject newListItem = GameObject.Instantiate(listItemTemplate);
            newListItem.transform.SetParent(savedBPList, false);
            newListItem.transform.Find("mc_savebpItemImage").gameObject.SetActive(false);
            newListItem.transform.Find("mc_savebpItemText").gameObject.GetComponent<Text>().text = "New...";
            newListItem.layer = savedBPList.gameObject.layer;
            ListItemData listItemData = newListItem.AddComponent<ListItemData>();
            listItemData.index = newBPIndex;
            EventTrigger.Entry newItemTrig = new EventTrigger.Entry();
            newItemTrig.eventID = EventTriggerType.PointerDown;
            newItemTrig.callback.AddListener((data) => { ListItem_Click((PointerEventData)data); });
            newListItem.GetComponent<EventTrigger>().triggers.Add(newItemTrig);

            // List saved BPs
            int skipped = 0;
            for (int i = 0; i < Main.data.blueprints.Count; i++)
            {
                if (coreFilter == noneSelectedIndex || coreFilter == Main.data.blueprints[i].core)
                {
                    // Create
                    GameObject bpListItem = GameObject.Instantiate(listItemTemplate);
                    bpListItem.transform.SetParent(savedBPList, false);
                    bpListItem.transform.localPosition = new Vector3(
                        bpListItem.transform.localPosition.x,
                        bpListItem.transform.localPosition.y - (listItemSpacing * (i + 1 - skipped)),
                        bpListItem.transform.localPosition.z);
                    bpListItem.layer = savedBPList.gameObject.layer;

                    // Image
                    bpListItem.transform.Find("mc_savebpItemImage").gameObject.GetComponent<Image>().sprite = Crafting.GetWeaponComponent(Main.data.blueprints[i].core).sprite;

                    // Text
                    bpListItem.transform.Find("mc_savebpItemText").gameObject.GetComponent<Text>().text = Main.data.blueprints[i].name;

                    // List item data
                    ListItemData bpListItemData = bpListItem.AddComponent<ListItemData>();
                    bpListItemData.index = i;

                    // Click event                
                    EventTrigger.Entry newTrig = new EventTrigger.Entry();
                    newTrig.eventID = EventTriggerType.PointerDown;
                    newTrig.callback.AddListener((data) => { ListItem_Click((PointerEventData)data); });
                    bpListItem.GetComponent<EventTrigger>().triggers.Add(newTrig);
                }
                else
                    skipped++;
            }
        }

        private void RefreshSelectedBPContent()
        {
            for (int i = 0; i < selectedBPContent.childCount; i++)
                GameObject.Destroy(selectedBPContent.GetChild(i).gameObject);

            if (selectedIndex < 0)
            {
                selectedBPName.text = "";
                return;
            }

            PersistentData.Blueprint bp = Main.data.blueprints[selectedIndex];

            selectedBPName.text = bp.name;

            // Set content game object size
            selectedBPContent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listItemSpacing * (bp.components.Count + bp.modifiers.Count));

            for (int i = 0; i < bp.components.Count; i++)
            {
                // Create
                GameObject componentListItem = GameObject.Instantiate(listItemTemplate);
                componentListItem.transform.SetParent(selectedBPContent, false);
                componentListItem.transform.localPosition = new Vector3(
                    componentListItem.transform.localPosition.x,
                    componentListItem.transform.localPosition.y - (listItemSpacing * i),
                    componentListItem.transform.localPosition.z);
                componentListItem.layer = selectedBPContent.gameObject.layer;

                // Image
                componentListItem.transform.Find("mc_savebpItemImage").gameObject.GetComponent<Image>().sprite = Crafting.GetWeaponComponent(bp.components[i].id).sprite;

                // Text
                componentListItem.transform.Find("mc_savebpItemText").gameObject.GetComponent<Text>().text = "(" + bp.components[i].qnt.ToString() + ") " + Crafting.GetWeaponComponent(bp.components[i].id).componentName;
            }

            for (int i = bp.components.Count; i < bp.components.Count + bp.modifiers.Count; i++)
            {
                int modI = i - bp.components.Count;

                // Create
                GameObject componentListItem = GameObject.Instantiate(listItemTemplate);
                componentListItem.transform.SetParent(selectedBPContent, false);
                componentListItem.transform.localPosition = new Vector3(
                    componentListItem.transform.localPosition.x,
                    componentListItem.transform.localPosition.y - (listItemSpacing * i),
                    componentListItem.transform.localPosition.z);
                componentListItem.layer = selectedBPContent.gameObject.layer;

                // Image
                componentListItem.transform.Find("mc_savebpItemImage").gameObject.GetComponent<Image>().sprite = Crafting.GetWeaponModifier(bp.modifiers[modI].id).sprite;

                // Text
                componentListItem.transform.Find("mc_savebpItemText").gameObject.GetComponent<Text>().text = "(" + bp.modifiers[modI].qnt.ToString() + ") " + Crafting.GetWeaponModifier(bp.modifiers[modI].id).modifierName;
            }
        }

        private void OpenConfirmPanel(ConfirmAction action)
        {
            string message = "";
            string blueprintName = Main.data.blueprints[selectedIndex].name;

            switch(action)
            {
                case ConfirmAction.delete:
                    message = confirmDelete.Replace("BPNAME", blueprintName);
                    confirmDialogConfirmBtn.onClick = confirmDialogDeleteEvent;
                    break;
                case ConfirmAction.save:
                    message = confirmSave.Replace("BPNAME", blueprintName);
                    confirmDialogConfirmBtn.onClick = confirmDialogSaveEvent;
                    break;
                default:
                    break;
            }

            confirmDialogText.text = message;

            confirmDialog.SetActive(true);
        }

        private void ManageButton_Click()
        {
            OpenMainPanel();
        }

        private void ListItem_Click(PointerEventData pointerEventData)
        {
            GameObject listItem = pointerEventData.pointerCurrentRaycast.gameObject.transform.parent.gameObject;
            
            if (currentHighlight != null)
                currentHighlight.SetActive(false);

            selectedIndex = listItem.GetComponent<ListItemData>().index;
            currentHighlight = listItem.transform.Find("mc_savebpHighlight").gameObject;
            currentHighlight.SetActive(true);
            RefreshSelectedBPContent();
        }

        private void DeleteButton_Click()
        {
            if (selectedIndex >= 0)
                OpenConfirmPanel(ConfirmAction.delete);
        }

        private void LoadButton_Click()
        {
            if (weaponCrafting == null)
            {
                if (selectedIndex < 0)
                    InfoPanelControl.inst.ShowWarning("No blueprint selected.", 1, false);
                return;
            }

            AccessTools.FieldRefAccess<WeaponCrafting, List<SelectedItems>>("currComponents")(weaponCrafting) = Main.data.blueprints[selectedIndex].components;
            AccessTools.FieldRefAccess<WeaponCrafting, List<SelectedItems>>("currModifiers")(weaponCrafting) = Main.data.blueprints[selectedIndex].modifiers;                      
            AccessTools.FieldRefAccess<InputField>(typeof(WeaponCrafting), "resultWeaponName")(weaponCrafting).text = Main.data.blueprints[selectedIndex].name;
            AccessTools.Method(typeof(WeaponCrafting), "CalculateAll", new System.Type[] { typeof(bool) }).Invoke(weaponCrafting, new object[] { true });

            Main.loadedBPIndex = selectedIndex;
            mainPanel.SetActive(false);
        }

        private void SaveButton_Click()
        {
            if (weaponCrafting == null)
                return;

            if (selectedIndex >= 0)
                OpenConfirmPanel(ConfirmAction.save);
            else if (selectedIndex == -1)
                ConfirmDialogSaveButton_Click();
            else
                InfoPanelControl.inst.ShowWarning("Select \"New...\" or a blueprint to overwrite.", 1, false);
        }

        private void CancelButton_Click()
        {
            mainPanel.SetActive(false);
        }

        private void ConfirmDialogCancelButton_Click()
        {
            confirmDialog.SetActive(false);
        }

        private void ConfirmDialogSaveButton_Click()
        {
            PersistentData.Blueprint bp;
            bp = new PersistentData.Blueprint()
            {
                components = new List<SelectedItems>(AccessTools.FieldRefAccess<WeaponCrafting, List<SelectedItems>>("currComponents")(weaponCrafting)),
                modifiers = new List<SelectedItems>(AccessTools.FieldRefAccess<WeaponCrafting, List<SelectedItems>>("currModifiers")(weaponCrafting)),
                name = AccessTools.FieldRefAccess<InputField>(typeof(WeaponCrafting), "resultWeaponName")(weaponCrafting).text
            };
            foreach (SelectedItems component in bp.components)
                if (PersistentData.Blueprint.coreIds.Contains(component.id))
                    bp.core = component.id;

            if (bp.components.Count < 1)
            {
                InfoPanelControl.inst.ShowWarning("No components in blueprint.", 1, false);
                return;
            }

            if (selectedIndex == newBPIndex)
            {                
                Main.data.blueprints.Add(bp);
            }
            else
            {
                Main.data.blueprints[selectedIndex].components = new List<SelectedItems>(bp.components);
                Main.data.blueprints[selectedIndex].modifiers = new List<SelectedItems>(bp.modifiers);
                Main.data.blueprints[selectedIndex].name = bp.name;
                Main.data.blueprints[selectedIndex].core = bp.core;
            }

            Main.loadedBPIndex = Main.data.blueprints.IndexOf(bp);

            if(confirmDialog.activeSelf)
                confirmDialog.SetActive(false);

            PersistentData.SaveData(Main.data);
            RefreshSavedBPList();
            RefreshSelectedBPContent();
        }

        private void ConfirmDialogDeleteButton_Click()
        {
            Main.data.blueprints.RemoveAt(selectedIndex);            
            RefreshSavedBPList();
            RefreshSelectedBPContent();
            confirmDialog.SetActive(false);
        }

        private class ListItemData:MonoBehaviour
        {
            internal int index;
        }
    }
}

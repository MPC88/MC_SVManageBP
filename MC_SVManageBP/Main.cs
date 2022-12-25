
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MC_SVManageBP
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        // BepInEx
        public const string pluginGuid = "mc.starvalor.managebp";
        public const string pluginName = "SV Manage BP";
        public const string pluginVersion = "1.0.6";

        // Star Valor
        private const int craftingPanelCode = 4;

        // Mod
        public const int noBPLoaded = -1;
        internal static ManageBPUI ui;
        public static PersistentData data;
        public static int loadedBPIndex = noBPLoaded;

        public void Awake()
        {
            ui = LoadAssets();
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        private ManageBPUI LoadAssets()
        {
            string pluginfolder = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
            string bundleName = "mc_svmanagebp";
            AssetBundle assets = AssetBundle.LoadFromFile($"{pluginfolder}\\{bundleName}");
            GameObject pack = assets.LoadAsset<GameObject>("Assets/mc_managebp.prefab");

            ManageBPUI ui = new ManageBPUI(
                pack.transform.Find("mc_savebpMainPanel").gameObject,
                pack.transform.Find("mc_savebpConfirmDlg").gameObject,
                pack.transform.Find("mc_savebpListItem").gameObject,
                pack.transform.Find("mc_savebpManageBPBtn").gameObject);

            return ui;
        }

        [HarmonyPatch(typeof(MenuControl), nameof(MenuControl.LoadGame))]
        [HarmonyPostfix]
        private static void MenuControlLoadGame_Post()
        {
            data = PersistentData.LoadData(GameData.gameFileIndex.ToString("00"));
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.SaveGame))]
        [HarmonyPrefix]
        private static void GameDataSaveGame_Pre()
        {
            PersistentData.SaveData(data);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.OpenPanel))]
        [HarmonyPostfix]
        private static void DockingUIOpenPanel_Post(WeaponCrafting ___weaponCrafting, int code)
        {
            if (code != craftingPanelCode || !___weaponCrafting.isActive)
            {
                ui.SetActiveManageBPBtn(null, false);
                return;
            }

            if (data == null)
                data = new PersistentData();
            ui.SetActiveManageBPBtn(___weaponCrafting, true);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.StartDockingStation))]
        [HarmonyPostfix]
        private static void DockingUIStartDocking_Post(DockingUI __instance, GameObject ___craftingPanel, WeaponCrafting ___weaponCrafting)
        {
            if (AccessTools.FieldRefAccess<DockingUI, GameObject>("craftingPanel")(__instance).activeSelf)
            {
                if (data == null)
                    data = new PersistentData();
                ui.SetActiveManageBPBtn(___weaponCrafting, true);
            }
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.CloseDockingStation))]
        [HarmonyPrefix]
        private static void DockingUICloseDockingStation_Pre()
        {
            ui.CloseAll();
        }

        [HarmonyPatch(typeof(WeaponCrafting), nameof(WeaponCrafting.Open))]
        [HarmonyPostfix]
        private static void WeaponCraftingOpen_Post(WeaponCrafting __instance)
        {
            if (data == null)
                data = new PersistentData();
            ui.SetActiveManageBPBtn(__instance, true);
        }

        [HarmonyPatch(typeof(WeaponCrafting), nameof(WeaponCrafting.BuildWeapon))]
        [HarmonyPostfix]
        private static void WeaponCraftingBuild_Post(WeaponCrafting __instance)
        {
            if (loadedBPIndex == noBPLoaded)
                return;

            data.blueprints[loadedBPIndex].weaponIDs.Add(GameData.data.weaponList[GameData.data.weaponList.Count - 1].index);            
        }
    }
}

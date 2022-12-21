using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;

namespace MC_SVManageBP
{
    [Serializable]
    public class PersistentData
    {
        [NonSerialized]
        private const string modSaveFolder = "/MCSVSaveData/";  // /SaveData/ sub folder
        [NonSerialized]
        private const string modSaveFilePrefix = "ManageBP_"; // modSaveFilePrefixNN.dat

        public List<Blueprint> blueprints;

        internal PersistentData()
        {
            blueprints = new List<Blueprint>();
        }

        internal static void SaveData(PersistentData data)
        {
            if (data == null)
                return;

            string tempPath = Application.dataPath + GameData.saveFolderName + modSaveFolder + "MBPTemp.dat";

            if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = File.Create(tempPath);
            binaryFormatter.Serialize(fileStream, data);
            fileStream.Close();

            File.Copy(tempPath, Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + GameData.gameFileIndex.ToString("00") + ".dat", true);
            File.Delete(tempPath);
        }

        internal static PersistentData LoadData(string saveIndex)
        {
            string modData = Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + saveIndex + ".dat";

            FileStream fileStream = null;
            try
            {
                if (!saveIndex.IsNullOrWhiteSpace() && File.Exists(modData))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    fileStream = File.Open(modData, FileMode.Open);
                    PersistentData loadData = (PersistentData)binaryFormatter.Deserialize(fileStream);
                    fileStream.Close();

                    if (loadData != null)
                        return loadData;
                }

                return new PersistentData();
            }
            catch
            {
                SideInfo.AddMsg("<color=red>CrewRoll mod load failed.</color>");
                if (fileStream != null)
                    fileStream.Close();
                return null;
            }
            finally
            {
                if(fileStream != null)
                    fileStream.Close();
            }
        }

        [Serializable]
        public class Blueprint
        {
            [NonSerialized]
            internal static List<int> coreIds = new List<int>() { 0,1,2,3,4,5,6,7,8,9,10,11,16,17,18,21 };

            internal string name;
            internal int core;
            public List<SelectedItems> components;
            public List<SelectedItems> modifiers;
            public List<int> weaponIDs;

            internal Blueprint()
            {
                name = "";
                core = 1;
                components = new List<SelectedItems>();
                modifiers = new List<SelectedItems>();
                this.weaponIDs = new List<int>();
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WonderDanceProj
{
    public static class DataFileLoader
    {
        private static List<BeatMap> beatMaps = new List<BeatMap>();

        public static List<BeatMap> Beatmaps => beatMaps;

        public static void LoadData()
        {
            // Database path
            string externalDataPath = Path.GetFullPath(@Path.Combine(Application.dataPath, "External Data"));
            string resourcePath = Path.GetFullPath(@Path.Combine(Application.dataPath, "Resources"));

            // Check external data diretory not exists
            if (!Directory.Exists(externalDataPath))
            {
                DirectoryInfo info = Directory.CreateDirectory(externalDataPath);
            }

            // Load resource files
            DirectoryInfo resDir = new DirectoryInfo(resourcePath);
            FileInfo[] s = resDir.GetFiles("*.*", SearchOption.AllDirectories)
                .Where(file => file.FullName.ToLower().EndsWith(".mp3") ||
                    file.FullName.ToLower().EndsWith(".wav") ||
                    file.FullName.ToLower().EndsWith(".ogg"))
                .ToArray();

            // Copy all song file from resource to external data
            foreach (FileInfo f in s)
            {
                // Get path and file name from resource
                string resFileDir = f.FullName;
                string fileName = f.Name;

                // Check if the default song file not exists
                string targetCopyFileDir = Path.GetFullPath(@Path.Combine(externalDataPath, fileName));
                if (!File.Exists(targetCopyFileDir))
                {
                    File.Copy(resFileDir, targetCopyFileDir);
                }
            }

            // Refresh asset database in unity editor only
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }

        public static void SaveData()
        {

        }
    }

}

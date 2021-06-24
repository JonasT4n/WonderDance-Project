using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

namespace WonderDanceProj
{
    public static class DataFileLoader
    {
        public const string MAPFILE_EXTENSION = "wdmap";
        public const string EXTERNAL_DATAFOLDER = "External Data";

        private static Dictionary<string, BeatMap> beatMaps = new Dictionary<string, BeatMap>();

        public static Dictionary<string, BeatMap> Beatmaps => beatMaps;

        /// <summary>
        /// Load all existing data.
        /// </summary>
        public static async Task LoadData()
        {
            // Database path
            string externalDataPath = Path.GetFullPath(@Path.Combine(Application.dataPath, EXTERNAL_DATAFOLDER));
            string resourcePath = Application.streamingAssetsPath;

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

            // Load all songs file into beatmap data, including existing map files (.wdmap)
            DirectoryInfo exDir = new DirectoryInfo(externalDataPath);
            FileInfo[] songData = resDir.GetFiles("*.*", SearchOption.AllDirectories)
                .Where(file => file.FullName.ToLower().EndsWith(".mp3") ||
                    file.FullName.ToLower().EndsWith(".wav") ||
                    file.FullName.ToLower().EndsWith(".ogg"))
                .ToArray();

            // Collect all requests
            foreach (FileInfo info in songData)
            {
                string filePath = info.FullName;
                string[] fileNameSeparator = info.Name.Split('.');

                // Check beatmap already exists
                if (beatMaps.ContainsKey(fileNameSeparator[0]))
                    continue;


                // Create import request
                UnityWebRequest request;
                switch (fileNameSeparator[1])
                {
                    case "wav":
                        request = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.WAV);
                        break;

                    case "ogg":
                        request = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.OGGVORBIS);
                        break;

                    default: // MPEG
                        request = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.MPEG);
                        break;
                }

                // Run a request
                string mapTargetPath = $"{externalDataPath}/{fileNameSeparator[0]}.{MAPFILE_EXTENSION}";
                await DownloadRequestAsync(fileNameSeparator[0], request, mapTargetPath);
            }
        }

        /// <summary>
        /// Load data by selecting extensions.
        /// </summary>
        /// 
        /// <example>
        /// <code>await LoadDataSelection("png", "mpeg", "txt")</code>
        /// </example>
        public static async Task LoadDataSelection(string path, params string[] extensions)
        {
            await Task.Yield();
        }

        /// <summary>
        /// Save all existing data.
        /// </summary>
        public static void SaveData()
        {
            // Save all beatmap data using binary formatter
            BinaryFormatter formatter = new BinaryFormatter();
            foreach (KeyValuePair<string, BeatMap> beatMap in beatMaps)
            {
                BeatMap map = beatMap.Value;

                // Load stream file
                FileStream stream = new FileStream(map.FilePath, FileMode.Create);

                // Serialize into binary format
                formatter.Serialize(stream, new BeatMap.BeatMapInnerData()
                {
                    bpm = map.BPM,
                    dataPath = map.FilePath,
                });

                // Close stream
                stream.Close();
            }

            // Refresh asset database in unity editor only
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }

        /// <summary>
        /// Save individual beatmap data.
        /// </summary>
        public static void SaveData(BeatMap map)
        {
            // Save beatmap using binary formatter
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(map.FilePath, FileMode.OpenOrCreate);

            // Serialize into binary format
            formatter.Serialize(stream, new BeatMap.BeatMapInnerData()
            {
                bpm = map.BPM,
                dataPath = map.FilePath,
            });

            // Close stream
            stream.Close();

            // Refresh asset database in unity editor only
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }

        private static async Task DownloadRequestAsync(string mapName, UnityWebRequest request, string mapFilePath)
        {
            // Receiving download data
            while (!request.isDone)
            {
                request.SendWebRequest();
                await Task.Yield();

                // Check current result
                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        #if UNITY_EDITOR
                        Debug.LogWarning($"Connection Failed - Cannot request link: {request.url}");
                        #endif
                        return;

                    case UnityWebRequest.Result.DataProcessingError:
                        #if UNITY_EDITOR
                        Debug.LogWarning($"Process Failed - Cannot request link: {request.url}");
                        #endif
                        return;

                    case UnityWebRequest.Result.ProtocolError:
                        #if UNITY_EDITOR
                        Debug.LogWarning($"Protocol Request Error - Cannot request link: {request.url}");
                        #endif
                        return;

                    default: // In progress or succeeded
                        break;
                }
            }

            // Load stream map file
            if (File.Exists(mapFilePath))
            {
                try
                {
                    // Load stream file
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream stream = new FileStream(mapFilePath, FileMode.Open);

                    // Convert stream data into inner data
                    BeatMap.BeatMapInnerData innerData = formatter.Deserialize(stream) as BeatMap.BeatMapInnerData;
                    innerData.dataPath = mapFilePath;

                    // Create beatmap if only the request succeeded
                    beatMaps.Add(mapName, new BeatMap(DownloadHandlerAudioClip.GetContent(request), innerData));

                    // Close reader
                    stream.Close();
                    return;
                }
                catch (System.Exception exc)
                {
                    #if UNITY_EDITOR
                    Debug.LogError($"Loading Failed ({mapFilePath}): {exc.GetType()} -> {exc.Message}");
                    Debug.Log("Creating new file and replace it.");
                    #endif
                }
            }

            // Create beatmap if only the request succeeded
            BeatMap newBeatMap = new BeatMap(DownloadHandlerAudioClip.GetContent(request), mapFilePath);
            beatMaps.Add(mapName, newBeatMap);
            SaveData(newBeatMap);
        }

        // Clear static data
        public static void ClearLoadedData()
        {
            beatMaps.Clear();
        }
    }
}

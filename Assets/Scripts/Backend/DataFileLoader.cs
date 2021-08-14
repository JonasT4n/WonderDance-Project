using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WonderDanceProj
{
    public static class DataFileLoader
    {
        // Constants for path saving assets
        public const string                         MAPFILE_EXTENSION = "wdmap";
        public const string                         EXTERNAL_DATAFOLDER = "External Data";

        // Temporary variables
        private static Dictionary<string, Beatmap>  _beatMaps = new Dictionary<string, Beatmap>();
        internal static List<IEnumerator>           _needToBeLoadRoutines = new List<IEnumerator>();
        internal static int                         _needTobeLoad = 0;

        #region Properties
        public static Dictionary<string, Beatmap> Beatmaps => _beatMaps;
        public static bool IsFinishLoading => _needTobeLoad <= 0;
        #endregion

        /// <summary>
        /// Load all existing data.
        /// </summary>
        public static void LoadBeatmapData()
        {
            // Clear current loaded beatmap cache
            _beatMaps.Clear();

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
            AssetDatabase.Refresh();
            #endif

            // Load all songs file into beatmap data, including existing map files (.wdmap)
            DirectoryInfo exDir = new DirectoryInfo(externalDataPath);
            FileInfo[] songData = exDir.GetFiles("*.*", SearchOption.AllDirectories)
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
                if (_beatMaps.ContainsKey(fileNameSeparator[0]))
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
                _needToBeLoadRoutines.Add(DownloadRequestAsync(fileNameSeparator[0], request, mapTargetPath));
            }
        }

        /// <summary>
        /// Load data by selecting extensions.
        /// </summary>
        /// <example>
        /// <code>await LoadDataSelection("png", "mpeg", "txt")</code>
        /// </example>
        public static void LoadSelectionData(string path, params string[] extensions)
        {
            // TODO: load data by selection
        }

        /// <summary>
        /// Save all existing data.
        /// </summary>
        public static void SaveData()
        {
            // Save all beatmap data using binary formatter
            BinaryFormatter formatter = new BinaryFormatter();
            foreach (KeyValuePair<string, Beatmap> beatMap in _beatMaps)
            {
                Beatmap map = beatMap.Value;

                // Load stream file
                string externalDataPath = Path.GetFullPath(@Path.Combine(Application.dataPath, EXTERNAL_DATAFOLDER));
                string filePath = Path.GetFullPath(@Path.Combine(externalDataPath, $"{map.MapName}.{MAPFILE_EXTENSION}"));
                FileStream stream = new FileStream(filePath, FileMode.Create);

                // Serialize into binary format
                formatter.Serialize(stream, new Beatmap.BeatmapPrivateData()
                {
                    bpm = map.BPM,
                    mapName = map.MapName,
                    dropSpeed = map.DropSpeed,
                    startPointSeconds = map.StartPointSeconds,
                    FixedDivision = map.SequenceDivision,
                    tempSeqDiv = map.SequenceDivision.GetDivisionDivider(),
                    lines = map.LinesCount,
                    objectsData = (int[][][])map.ObjectsData,
                    objectsMetaData = map.MetaData,
                });

                // Close stream
                stream.Close();
            }

            // Refresh asset database in unity editor only
            #if UNITY_EDITOR
            AssetDatabase.Refresh();
            #endif
        }

        /// <summary>
        /// Save individual beatmap data.
        /// </summary>
        public static void SaveData(Beatmap map)
        {
            // Save beatmap using binary formatter
            BinaryFormatter formatter = new BinaryFormatter();
            string externalDataPath = Path.GetFullPath(@Path.Combine(Application.dataPath, EXTERNAL_DATAFOLDER));
            string filePath = Path.GetFullPath(@Path.Combine(externalDataPath, $"{map.MapName}.{MAPFILE_EXTENSION}"));
            FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate);

            // Serialize into binary format
            formatter.Serialize(stream, new Beatmap.BeatmapPrivateData()
            {
                bpm = map.BPM,
                mapName = map.MapName,
                dropSpeed = map.DropSpeed,
                FixedDivision = map.SequenceDivision,
                startPointSeconds = map.StartPointSeconds,
                tempSeqDiv = map.SequenceDivision.GetDivisionDivider(),
                lines = map.LinesCount,
                objectsData = (int[][][])map.ObjectsData,
                objectsMetaData = map.MetaData
            });

            // Close stream
            stream.Close();

            // Save cache
            _beatMaps[map.MapName] = map;

            // Refresh asset database in unity editor only
            #if UNITY_EDITOR
            AssetDatabase.Refresh();
            #endif
        }

        // Clear static data
        public static void ClearLoadedData()
        {
            _beatMaps.Clear();
        }

        #if UNITY_EDITOR
        internal static void CreateBeatmap(AudioClip song, Beatmap.BeatmapPrivateData data)
        {
            // Save beatmap using binary formatter
            BinaryFormatter formatter = new BinaryFormatter();
            string externalDataPath = Path.GetFullPath(@Path.Combine(Application.dataPath, EXTERNAL_DATAFOLDER));
            string filePath = Path.GetFullPath(@Path.Combine(externalDataPath, $"{data.mapName}.{MAPFILE_EXTENSION}"));
            FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate);

            // Serialize into binary format
            if (!File.Exists(filePath))
            {
                // Convert into binary
                formatter.Serialize(stream, data);
                Debug.Log($"Beatmap file {Path.GetFileName(filePath)} has been created.");
            }
            else Debug.LogWarning($"Beatmap file {Path.GetFileName(filePath)} already exists.");

            // Close stream
            stream.Close();

            // Copy song into external data folder
            string songAssetPath = AssetDatabase.GetAssetPath(song.GetInstanceID());

            // Check if the default song file not exists
            string targetCopyFileDir = Path.GetFullPath(@Path.Combine(externalDataPath, Path.GetFileName(songAssetPath)));
            Debug.Log($"Copying song {Path.GetFileName(songAssetPath)} into external data folder.");
            if (!File.Exists(targetCopyFileDir))
            {
                // Copy song file
                File.Copy(songAssetPath, targetCopyFileDir);
                Debug.Log($"Song file {Path.GetFileName(targetCopyFileDir)} copied.");
            }
            else Debug.LogWarning($"Song file {Path.GetFileName(targetCopyFileDir)} already exists.");

            // Refresh asset database in unity editor only
            AssetDatabase.Refresh();
        }
        #endif

        private static IEnumerator DownloadRequestAsync(string musicName, UnityWebRequest request, string mapFilePath)
        {
            // Add 1 need to be load progress
            _needTobeLoad++;

            // Receiving download data
            while (!request.isDone)
            {
                request.SendWebRequest();
                yield return null;

                // Check current result
                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        #if UNITY_EDITOR
                        Debug.LogWarning($"Connection Failed - Cannot request link: {request.url}");
                        #endif
                        break;

                    case UnityWebRequest.Result.DataProcessingError:
                        #if UNITY_EDITOR
                        Debug.LogWarning($"Process Failed - Cannot request link: {request.url}");
                        #endif
                        break;

                    case UnityWebRequest.Result.ProtocolError:
                        #if UNITY_EDITOR
                        Debug.LogWarning($"Protocol Request Error - Cannot request link: {request.url}");
                        #endif
                        break;

                    default: // In progress or succeeded
                        break;
                }
            }

            // Check loading failed
            if (request.result == UnityWebRequest.Result.Success)
            {
                #if UNITY_EDITOR
                if (GameManager.Singleton._debug)
                {
                    Debug.Log($"Successfully loading {Path.GetFileName(mapFilePath)}");
                    Debug.Log($"Is File Exists: {File.Exists(mapFilePath)}");
                }
                #endif
                // Load stream map file
                if (File.Exists(mapFilePath))
                {
                    // Load stream file
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream stream = new FileStream(mapFilePath, FileMode.Open);

                    try
                    {
                        // Convert stream data into inner data
                        Beatmap.BeatmapPrivateData innerData = formatter.Deserialize(stream) as Beatmap.BeatmapPrivateData;

                        // Create beatmap if only the request succeeded
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                        audioClip.name = musicName;
                        _beatMaps.Add(musicName, new Beatmap(audioClip, innerData));

                        // Close reader
                        stream.Close();
                    }
                    catch (System.Exception exc)
                    {
                        #if UNITY_EDITOR
                        Debug.LogError($"Loading Failed ({mapFilePath}): {exc.GetType()} -> {exc.Message}");
                        Debug.Log("Creating new file and replace it.");
                        #endif

                        // Delete old serialized file
                        stream.Flush();
                        stream.Close();
                        File.Delete(mapFilePath);

                        // Recreate beatmap file
                        Beatmap newBeatMap = new Beatmap(DownloadHandlerAudioClip.GetContent(request), musicName);
                        _beatMaps.Add(musicName, newBeatMap);
                        SaveData(newBeatMap);
                    }
                }
                else
                {
                    // Create new beatmap file if not exists
                    Beatmap newBeatMap = new Beatmap(DownloadHandlerAudioClip.GetContent(request), musicName);
                    _beatMaps.Add(musicName, newBeatMap);
                    SaveData(newBeatMap);
                }
            }

            // 1 loading progress finished
            _needTobeLoad--;
        }
    }
}

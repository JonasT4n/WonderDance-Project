using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WonderDanceProj
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(BeatmapDesign))]
    public class BeatmapDesignEditor : Editor
    {
        private Beatmap     _loadedBeatmapFile = null;
        private string      _mapFilePath;

        public override void OnInspectorGUI()
        {
            // Update current serialize field objects
            serializedObject.Update();

            // Convert target to components
            BeatmapDesign design = (BeatmapDesign)target;

            // Beatmap file section
            EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
            EditorGUILayout.LabelField("Beatmap Loaders", EditorGUIStyles.GetLabelHeaderStyle(TextAnchor.MiddleLeft, 12));
            EditorGUILayout.HelpBox("Type file path into this field to load the beatmap into the editor.", MessageType.Info);
            _mapFilePath = GUILayout.TextField(_mapFilePath);
            // Load the file path and it's song
            string filePathWithExtension = $"{_mapFilePath}.{DataFileLoader.MAPFILE_EXTENSION}";
            if (File.Exists(filePathWithExtension) && _loadedBeatmapFile == null)
            {
                // TODO: Load beatmap file
            }
            else _loadedBeatmapFile = null;
            EditorGUILayout.EndVertical();
            
            // Check beatmap is not empty
            if (_loadedBeatmapFile == null)
            {
                // Create serialize fields
                EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
                EditorGUILayout.LabelField("Design Settlement", EditorGUIStyles.GetLabelHeaderStyle());
                var song = serializedObject.FindProperty("_song");
                EditorGUILayout.PropertyField(song, new GUIContent("Song"));

                // Beatmap data section
                EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.white));
                string mapName = design._song != null ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(design._song.GetInstanceID())) : string.Empty;
                GUI.enabled = false;
                design._beatmap.mapName = GUILayout.TextField(mapName);
                GUI.enabled = true;
                design._beatmap.bpm = EditorGUILayout.IntField("BPM", design._beatmap.bpm);
                design._beatmap.InitialDivision = (Division)EditorGUILayout.EnumPopup("Divider", design._beatmap.FixedDivision);
                GUILayout.Label($"Beat Sequence divided by 1/{design._beatmap.FixedDivision.GetDivisionDivider() / 4}", EditorGUIStyles.GetLabelHeaderStyle(TextAnchor.MiddleLeft, 12));
                design._beatmap.dropSpeed = EditorGUILayout.FloatField("Drop Speed", design._beatmap.dropSpeed);
                GUI.enabled = false;
                design._beatmap.lines = EditorGUILayout.IntField("Column Count", design._beatmap.lines);
                GUI.enabled = true;
                design._beatmap.startPointSeconds = EditorGUILayout.FloatField("Start Point", design._beatmap.startPointSeconds);
                EditorGUILayout.EndVertical();
                // Button to reset
                if (GUILayout.Button("Reset"))
                {
                    song.objectReferenceValue = null;
                    design.ResetData();
                }
                // Button to create
                if (GUILayout.Button("Create"))
                {
                    // Save new beatmap created via Unity Editor
                    DataFileLoader.CreateBeatmap(design._song, design._beatmap);

                    // Notify
                    Debug.Log($"New beatmap has been created\n" +
                        $"Song: {design._song.name}\n" +
                        $"Map Name: {design._beatmap.mapName}\n");
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                // Create serialize fields
                EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
                EditorGUILayout.LabelField("Design Settlement", EditorGUIStyles.GetLabelHeaderStyle());
                var song = serializedObject.FindProperty("_song");
                EditorGUILayout.PropertyField(song, new GUIContent("Song"));

                // Beatmap data section
                EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.white));
                string mapName = design._song != null ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(design._song.GetInstanceID())) : string.Empty;
                GUI.enabled = false;
                design._beatmap.mapName = GUILayout.TextField(mapName);
                GUI.enabled = true;
                design._beatmap.bpm = EditorGUILayout.IntField("BPM", design._beatmap.bpm);
                design._beatmap.InitialDivision = (Division)EditorGUILayout.EnumPopup("Divider", design._beatmap.FixedDivision);
                GUILayout.Label($"Beat Sequence divided by 1/{design._beatmap.FixedDivision.GetDivisionDivider() / 4}", EditorGUIStyles.GetLabelHeaderStyle(TextAnchor.MiddleLeft, 12));
                design._beatmap.dropSpeed = EditorGUILayout.FloatField("Drop Speed", design._beatmap.dropSpeed);
                GUI.enabled = false;
                design._beatmap.lines = EditorGUILayout.IntField("Column Count", design._beatmap.lines);
                GUI.enabled = true;
                design._beatmap.startPointSeconds = EditorGUILayout.FloatField("Start Point", design._beatmap.startPointSeconds);
                EditorGUILayout.EndVertical();
                // Button to reset
                if (GUILayout.Button("Reset"))
                {
                    song.objectReferenceValue = null;
                    design.ResetData();
                }
                // Button to save beatmap file
                if (GUILayout.Button("Save Changes"))
                {
                    // TODO: Save changes
                }
                EditorGUILayout.EndVertical();
            }

            // Check any changes
            if (GUI.changed)
            {
                // Update fields
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(design);
            }
        }
    }
    #endif

    [CreateAssetMenu(fileName = "Beatmap Creator", menuName = "Project Design/Beatmap Creator Object")]
    internal class BeatmapDesign : ScriptableObject
    {
        [Tooltip("Song to be set with the beatmap")]
        [SerializeField] internal AudioClip                      _song = null;
        [Tooltip("Data Set for Creating Beatmap")]
        [SerializeField] internal Beatmap.BeatmapPrivateData     _beatmap = null;

        /// <summary>
        /// Reset current data set in beatmap data.
        /// </summary>
        public void ResetData()
        {
            // Reset data
            _beatmap.bpm = 130;
            _beatmap.mapName = string.Empty;
            _beatmap.dropSpeed = 5f;
            _beatmap.sequenceDiv = 16;
            _beatmap.startPointSeconds = 0f;
            _beatmap.tempSeqDiv = 16;
            _beatmap.lines = InputMap.MAX_KEY;
            _beatmap.objectsMetaData = string.Empty;
        }
    }

}

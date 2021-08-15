using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NaughtyAttributes;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WonderDanceProj
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(LevelContainer))]
    public class LevelContainerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Update serialized field objects
            serializedObject.Update();

            // Convert target into component
            LevelContainer lvlc = (LevelContainer)target;

            //// Create missing serialize fields
            //var mapNameHintTxt = serializedObject.FindProperty("_mapNameHintTxt");
            //EditorGUILayout.PropertyField(mapNameHintTxt, new GUIContent("Hint Txt"));

            // Check GUI changes
            if (GUI.changed)
            {
                // Update changes
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(lvlc);
            }
        }
    }
    #endif

    public class LevelContainer : ScrollRect
    {
        [Header("Attributes")]
        [SerializeField]
        private float               _atPreviewTime = 12f;

        [Header("UI Helper")]
        [SerializeField]
        private TextMeshProUGUI     _mapNameHintTxt = null;

        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Beatmap             _selectedBeatmap = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _levelIndex = 0;

        #region Unity BuiltIn Methods
        protected override void Start()
        {
            // Run base function
            base.Start();

            // Init component and set values
            _mapNameHintTxt.text = string.Empty;
        }
        #endregion

        public void SetPlayLevel(string levelName)
        {
            // Get level from loaded data file
            Beatmap beatmap;
            if (DataFileLoader.Beatmaps.TryGetValue(levelName, out beatmap))
            {
                // Check selected beatmap and confirmation
                if (beatmap.Equals(_selectedBeatmap))
                {
                    // Confirmation play beatmap
                    UIMenuManager.Singleton.StartPlay();
                }
                else
                {
                    // Set beatmap info
                    _selectedBeatmap = beatmap;

                    // Update ui hint
                    _mapNameHintTxt.text = _selectedBeatmap.MapName;
                    GameManager._S._selectedMap = _selectedBeatmap;

                    // Preview song and beatmap
                    SoundMaster.Singleton.Play(_selectedBeatmap.Song, _atPreviewTime);
                }
            }
        }
    }

}

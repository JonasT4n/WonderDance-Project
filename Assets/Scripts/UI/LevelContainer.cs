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
        private float               _atPreviewTime = 15f;
        [SerializeField]
        private Button              _levelButtonPrefab = null;

        [Header("UI Helper")]
        [SerializeField]
        private TextMeshProUGUI     _mapNameHintTxt = null;

        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Beatmap             _selectedBeatmap = null;

        #region Unity BuiltIn Methods
        protected override void Awake()
        {
            // Run base function
            base.Awake();

            // Clear and load all existing levels
            ClearLevelList();
            LoadLevelList();

            // Subscribe events
        }

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
                    GameManager.Singleton._selectedMap = _selectedBeatmap;

                    // Preview song and beatmap
                    SoundMaster.Singleton.Play(_selectedBeatmap.Song, _atPreviewTime);
                }
            }
        }

        private void LoadLevelList()
        {
            // Load all levels
            foreach (KeyValuePair<string, Beatmap> beatmapKeyPair in DataFileLoader.Beatmaps)
            {
                // Create new button
                Button lvlBtn = Instantiate(_levelButtonPrefab, content);

                // Set text
                TextMeshProUGUI txtMesh = lvlBtn.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                txtMesh.text = beatmapKeyPair.Key;

                // Subscribe event
                lvlBtn.onClick.AddListener(() =>
                {
                    // Select the level
                    SetPlayLevel(beatmapKeyPair.Key);
                });
            }

            // Calculate content height by how many childs
            VerticalLayoutGroup layoutContent = content.GetComponent<VerticalLayoutGroup>();
            if (layoutContent != null)
            {
                // Get paddings height
                float height = layoutContent.padding.top + layoutContent.padding.bottom;

                // Get spacings
                height += layoutContent.spacing * (content.childCount - 1);

                // Set content height
                content.sizeDelta = new Vector2(content.sizeDelta.x, height);
            }
        }

        private void ClearLevelList()
        {
            // Clear level by removing buttons
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                // Unsubcribe button if exists
                Transform lvlBtn = content.GetChild(i);
                Button btn = content.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveAllListeners();

                // Destroy object
                Destroy(lvlBtn.gameObject);
            }
        }
    }

}

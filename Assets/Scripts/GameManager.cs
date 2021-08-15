using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WonderDanceProj
{
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Event called when the game app has completely loaded.
        /// </summary>
        public static event System.Action   OnAppLoaded;

        // Singleton Behaviour
        public static GameManager           _S;

        [Header("Requirements")]
        [SerializeField]
        private SpriteAssetObj              _assets = null;

        // Temporary variables
        #if UNITY_EDITOR
        [BoxGroup("DEBUG"), SerializeField]
        internal bool                       _debug = true;
        #endif
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        internal Beatmap                    _selectedMap = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        internal string                     _characterKey = string.Empty;

        #region Properties
        public static GameManager Singleton => _S;
        public SpriteAssetObj Asset => _assets;
        internal static GameState State { set; get; } = GameState.MainMenu;
        #endregion

        #region Unity BuiltIn Methods
        private void Awake()
        {
            // Check singleton exists
            if (_S != null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"Deleted extra object of singleton behaviour, you can ignore this warning.");
                #endif
                Destroy(this);
                return;
            }

            // Set singleton if not exists yet
            _S = this;
            DontDestroyOnLoad(this.gameObject);

            // Load level data
            DataFileLoader.LoadBeatmapData();

            // TEMPORARY: Immediately play a level
            StartCoroutine(LevelLoadingRoutine());

            // Subscribe events
            EventHandler.OnBeatmapSaveChangesEvent += HandleSaveChanges;
        }

        private void OnDestroy()
        {
            // Safe release static data
            if (this.Equals(_S))
            {
                _S = null;

                // Save data into database
                DataFileLoader.SaveData();
                DataFileLoader.ClearLoadedData();

                // Unsubscribe events
                EventHandler.OnBeatmapSaveChangesEvent += HandleSaveChanges;
            }
        }
        #endregion

        #region Event Methods
        private void HandleSaveChanges(SaveEditBeatmapEventArgs args)
        {
            // Set current beatmap data in game manager
            _selectedMap = args.EditedMap;
        }
        #endregion

        private IEnumerator LevelLoadingRoutine()
        {
            // Initialize level loading
            for (int i = 0; i < DataFileLoader._needToBeLoadRoutines.Count; i++)
            {
                StartCoroutine(DataFileLoader._needToBeLoadRoutines[i]);
            }

            // Add loading progression
            yield return null;

            // Check levels are all loaded
            while (!DataFileLoader.IsFinishLoading) yield return null;

            // Call Event
            OnAppLoaded?.Invoke();
        }

    }

}

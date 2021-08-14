using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NaughtyAttributes;
using TMPro;

namespace WonderDanceProj
{
    /// <summary>
    /// Input mapping for player control key.
    /// </summary>
    [System.Serializable]
    public struct InputMap
    {
        public const int    MAX_KEY = 4;
        public KeyCode[]    keys;
    }

    /// <summary>
    /// Manage every GUI elements in game.
    /// </summary>
    public class UIGameManager : MonoBehaviour
    {
        /// <summary>
        /// Event callen when quiting the current game.
        /// This event called from Pause State to Level Menu State.
        /// </summary>
        public static event System.Action OnQuitGame;

        /// <summary>
        /// Event called whenever the game is pause or unpause.
        /// Event also can be called from Pause State to Play State.
        /// </summary>
        public static event System.Action<bool>     OnPauseGameActive;

        /// <summary>
        /// Event called when restarting level.
        /// </summary>
        public static event System.Action           OnRestartGame;

        // Singleton behaviour
        public static UIGameManager     _S;

        [Header("UI Pages")]
        [SerializeField] 
        private BeatmapEditor       _editor = null;
        [SerializeField]
        private RectTransform       _pausePanel = null;
        [SerializeField]
        private RectTransform       _endPage = null;

        [Header("UI Elements")]
        [SerializeField]
        private Button              _pauseButton = null;

        [Header("Attributes")]
        [SerializeField, Scene]
        private string              _levelMenuScene = string.Empty;
        [SerializeField]
        private float               _countdownSeconds = 3f;
        [SerializeField]
        private bool                _isCountdownResume = false;

        // Temporary variables
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private bool                _isEditorModeActive = false;
        private IEnumerator         _countdownRoutine;

        #region Properties
        public static UIGameManager Singleton => _S;
        public static bool IsEditorModeActive => _S._isEditorModeActive;
        public BeatmapEditor EditorUI => _editor;
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

            // Set editor inactive
            _editor.gameObject.SetActive(false);

            // Subscribe events
            BeatmapPlayer.OnBeginPlay += HandleBeginGameplay;
            BeatmapPlayer.OnPreEndPlay += HandlePreEndGameplay;
            BeatmapPlayer.OnEndPlay += HandleEndGameplay;
        }

        private void Update()
        {
            // Pause shortcut
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool setPause = !_pausePanel.gameObject.activeSelf;
                PauseGame(setPause);
            }
        }

        private void OnDestroy()
        {
            // Release singleton
            if (this.Equals(_S))
            {
                _S = null;

                // Unsubscribe events
                BeatmapPlayer.OnBeginPlay -= HandleBeginGameplay;
                BeatmapPlayer.OnPreEndPlay -= HandlePreEndGameplay;
                BeatmapPlayer.OnEndPlay -= HandleEndGameplay;
            }
        }
        #endregion

        #region Event Methods
        private void HandleBeginGameplay()
        {
            // Enable certain UI
            _pauseButton.interactable = true;
        }

        private void HandlePreEndGameplay()
        {
            // Disable certain UI
            _pauseButton.interactable = false;
        }

        private void HandleEndGameplay()
        {
            // Activate (Animatable) end game page
            _endPage.gameObject.SetActive(true);
        }
        #endregion

        /// <summary>
        /// Quit current gameplay.
        /// Used when the game is on edit or play state.
        /// </summary>
        public void QuitGame()
        {
            // Call event
            OnQuitGame?.Invoke();

            // Load to level menu scene
            GameManager.State = GameState.LevelMenu;
            SceneManager.LoadScene(_levelMenuScene);
        }

        /// <summary>
        /// Restart the level.
        /// </summary>
        public void RestartGame() => OnRestartGame?.Invoke();

        /// <summary>
        /// Pause current gameplay.
        /// </summary>
        /// <param name="active">Set pause active</param>
        public void PauseGame(bool active)
        {
            // Check countdown resume
            if (_isCountdownResume && !active)
            {
                // Start routine countdown before resume
                _pausePanel.gameObject.SetActive(false);
                _countdownRoutine = ResumeCountdownRoutine(active);
                StartCoroutine(_countdownRoutine);
                return;
            }

            // Call event
            _pausePanel.gameObject.SetActive(active);
            OnPauseGameActive?.Invoke(active);
        }

        /// <summary>
        /// Enter or exit editor mode.
        /// </summary>
        /// <param name="active"></param>
        public void SetEditorMode(bool active)
        {
            // Check active changes
            if (_isEditorModeActive != active)
            {
                // Assign active editor state
                if (active) _isEditorModeActive = true;
                _editor.gameObject.SetActive(active);

                // Call event
                EventHandler.CallEvent(new EditorModeActiveEventArgs(active));

                // Assign disable editor state
                if (!active) _isEditorModeActive = false;
            }
        }

        private IEnumerator ResumeCountdownRoutine(bool active)
        {
            // Start resume countdown
            float countdown = _countdownSeconds;
            while (countdown > 0f)
            {
                countdown -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            // Call late event
            OnPauseGameActive?.Invoke(active);

            // Release cache
            _countdownRoutine = null;
        }
    }

}

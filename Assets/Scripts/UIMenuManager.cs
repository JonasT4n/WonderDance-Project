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
    public class UIMenuManager : MonoBehaviour
    {
        // Singleton behaviour
        private static UIMenuManager    _S;

        [Header("UI Pages")]
        [SerializeField]
        private Animator                _mainMenuPage = null;
        [SerializeField]
        private Animator                _levelMenuPage = null;
        [SerializeField]
        private Animator                _chooseCharacterDialog = null;

        [Header("UI Input Settings")]
        [SerializeField]
        private RectTransform           _interactionBlock = null;
        [SerializeField, ReorderableList]
        private Button[]                _changeInputButton = { null, null, null, null };
        [SerializeField]
        private Slider                  _bgmVolumeSlider = null;
        [SerializeField]
        private Slider                  _sfxVolumeSlider = null;
        [SerializeField]
        private Toggle                  _editModeToggle = null;

        [Header("Attributes")]
        [SerializeField, Scene]
        private string                  _gameplayScene = string.Empty;

        // Temporary variables
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                     _inputIndex = -1;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private bool                    _isSettingInput = false;
        [BoxGroup("DEBUG"), SerializeField]
        private float                   _automaticCancelSettingSeconds = 10f;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private float                   _tempSettingSeconds = 0f;
        private static InputMap         _fixedInputMap = new InputMap {
            keys = new KeyCode[InputMap.MAX_KEY] {
                KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.UpArrow, KeyCode.RightArrow,
            }
        };
        private IEnumerator             _transitionRoutine = null;

        #region Properties
        public static UIMenuManager Singleton => _S;
        public static InputMap InputControl => _fixedInputMap;
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

            // Subscribe events
            _bgmVolumeSlider.onValueChanged.AddListener(val =>
            {
                // Set volume at speaker
                SoundMaster.Singleton.SetBGMVolume(val);
            });
            _sfxVolumeSlider.onValueChanged.AddListener(val =>
            {
                // Set volume at speaker
                SoundMaster.Singleton.SetSFXVolume(val);
            });
            _editModeToggle.onValueChanged.AddListener(active =>
            {
                // Assign editor mode
                UIGameManager.IsEditorModeActive = active;
            });
            for (int i = 0; i < InputMap.MAX_KEY; i++)
            {
                // Get button view object by index
                Button button = _changeInputButton[i];

                // Make sure there's no event set
                button.onClick.RemoveAllListeners();

                // Add event into listeners
                button.onClick.AddListener(() =>
                {
                    // Set control key with routine
                    SetControlKey(button);

                    // Activate interaction blocker
                    _interactionBlock.gameObject.SetActive(true);
                });
            }
        }

        private void Start()
        {
            // Set settings initial values
            _bgmVolumeSlider.value = SoundMaster.Singleton._volumeBGM;
            _sfxVolumeSlider.value = SoundMaster.Singleton._volumeSFX;
            _editModeToggle.isOn = UIGameManager.IsEditorModeActive;

            // Check game state to activate which menu
            switch (GameManager.State)
            {
                case GameState.LevelMenu:
                    _mainMenuPage.gameObject.SetActive(false);
                    _levelMenuPage.gameObject.SetActive(true);
                    _levelMenuPage.SetBool("Active", true);
                    break;

                case GameState.MainMenu:
                    _levelMenuPage.gameObject.SetActive(false);
                    _mainMenuPage.gameObject.SetActive(true);
                    _mainMenuPage.SetBool("Active", true);
                    break;
            }
        }

        private void Update()
        {
            // Check duration settings
            if (_isSettingInput && _tempSettingSeconds > 0f)
            {
                // Check timeout
                _tempSettingSeconds -= Time.deltaTime;
                if (_tempSettingSeconds <= 0f) _isSettingInput = false;
            }

            // Check key binding input settings
            if (_isSettingInput && Input.anyKeyDown && _inputIndex >= 0)
            {
                // Get which key was pressed
                KeyCode key = GetKeyDown();

                // Change ui text information about input
                Transform buttonChild = _changeInputButton[_inputIndex].transform.GetChild(0); // Get first child
                if (buttonChild.GetComponent<Text>()) buttonChild.GetComponent<Text>().text = key.ToString();
                else if (buttonChild.GetComponent<TextMeshProUGUI>()) buttonChild.GetComponent<TextMeshProUGUI>().text = key.ToString();

                // Close ui that blocks the games
                _fixedInputMap.keys[_inputIndex] = key;
                _interactionBlock.gameObject.SetActive(false);
                _inputIndex = -1;
            }
        }

        private void OnDestroy()
        {
            // Release singleton
            if (this.Equals(_S))
            {
                _S = null;

                // Unsubscribe events
                _bgmVolumeSlider.onValueChanged.RemoveAllListeners();
                _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                _editModeToggle.onValueChanged.RemoveAllListeners();
                for (int i = 0; i < InputMap.MAX_KEY; i++)
                {
                    // Remove all events on each buttons
                    Button button = _changeInputButton[i];
                    button.onClick.RemoveAllListeners();
                }
            }
        }
        #endregion

        /// <summary>
        /// Quit application.
        /// </summary>
        public void QuitApplication() => Application.Quit();

        public void GotoMainMenu()
        {
            // Check current transition routine is running, then ignore
            if (_transitionRoutine != null) return;

            // Check which from state
            switch (GameManager.State)
            {
                case GameState.LevelMenu:
                    _transitionRoutine = DelayPageActivation(_levelMenuPage, _mainMenuPage);
                    StartCoroutine(_transitionRoutine);
                    break;
            }

            // Set new state
            GameManager.State = GameState.MainMenu;
        }

        public void GotoLevelMenu()
        {
            // Check current transition routine is running, then ignore
            if (_transitionRoutine != null) return;

            // Check which from state
            switch (GameManager.State)
            {
                case GameState.MainMenu:
                    _transitionRoutine = DelayPageActivation(_mainMenuPage, _levelMenuPage);
                    StartCoroutine(_transitionRoutine);
                    break;
            }

            // Set new state
            GameManager.State = GameState.LevelMenu;
        }

        public void OpenPage(Animator target)
        {
            // Check current transition routine is running, then ignore
            if (_transitionRoutine != null) return;

            // Run transition
            _transitionRoutine = OpenPageTransition(target);
            StartCoroutine(_transitionRoutine);
        }

        public void ClosePage(Animator target)
        {
            // Check current transition routine is running, then ignore
            if (_transitionRoutine != null) return;

            // Run transition
            _transitionRoutine = ClosePageTransition(target);
            StartCoroutine(_transitionRoutine);
        }

        public void PickCharacter(string characterKey)
        {
            // Pick the character
            GameManager.Singleton._characterKey = characterKey;

            // Confirm play beatmap, go to gameplay scene
            GameManager.State = GameState.Gameplay;
            SceneManager.LoadScene(_gameplayScene);
        }

        public void StartPlay()
        {
            // Check editor must active
            if (UIGameManager.IsEditorModeActive)
            {
                // Confirm play beatmap, go to gameplay scene
                GameManager.State = GameState.Gameplay;
                SceneManager.LoadScene(_gameplayScene);
                return;
            }

            // Call for choosing character dialog
            OpenPage(_chooseCharacterDialog);
        }

        private void SetControlKey(Button button)
        {
            // Check any current setting input
            for (int i = 0; i < _changeInputButton.Length; i++)
            {
                // Check setting exists
                Button btn = _changeInputButton[i];
                if (button.Equals(btn))
                {
                    // Start setting input control
                    _inputIndex = i;
                    _tempSettingSeconds = _automaticCancelSettingSeconds;
                    _isSettingInput = true;
                    break;
                }
            }
        }

        private KeyCode GetKeyDown()
        {
            // NAIVE SOLUTION: Check each keycode
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                // Ignore mouse button
                switch (key)
                {
                    case KeyCode.Mouse0: continue;
                    case KeyCode.Mouse1: continue;
                    case KeyCode.Mouse2: continue;
                    case KeyCode.Mouse3: continue;
                    case KeyCode.Mouse4: continue;
                    case KeyCode.Mouse5: continue;
                    case KeyCode.Mouse6: continue;
                }

                // Return accepted key
                if (Input.GetKeyDown(key)) return key;
            }
            return KeyCode.None;
        }

        private IEnumerator OpenPageTransition(Animator target)
        {
            // Enable object, then do transition
            target.gameObject.SetActive(true);

            // Get enter duration
            target.SetBool("Active", true);
            AnimatorStateInfo info = target.GetCurrentAnimatorStateInfo(0);
            float duration = info.length;

            // Wait for animation finish
            while (duration > 0f)
            {
                duration -= Time.deltaTime;
                yield return null;
            }

            // Release transition routine
            _transitionRoutine = null;
        }

        private IEnumerator ClosePageTransition(Animator target)
        {
            // Get exit duration
            target.SetBool("Active", false);
            AnimatorStateInfo info = target.GetCurrentAnimatorStateInfo(0);
            float duration = info.length;

            // Wait for animation finish
            while (duration > 0f)
            {
                duration -= Time.deltaTime;
                yield return null;
            }

            // Disable object after transition
            target.gameObject.SetActive(false);

            // Release transition routine
            _transitionRoutine = null;
        }

        private IEnumerator DelayPageActivation(Animator from, Animator to)
        {
            // Start transitioning
            from.SetBool("Active", false);

            // Get exit duration before entering new state
            AnimatorStateInfo fromInfo = from.GetCurrentAnimatorStateInfo(0);
            float duration = fromInfo.length;

            // Wait until animation finished
            while (duration > 0f)
            {
                duration -= Time.deltaTime;
                yield return null;
            }

            // Disable and enable game object
            from.gameObject.SetActive(false);
            to.gameObject.SetActive(true);

            // Run target animation
            to.SetBool("Active", true);

            // Release transition routine
            _transitionRoutine = null;
        }
    }

}

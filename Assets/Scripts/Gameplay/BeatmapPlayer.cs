using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WonderDanceProj
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(BeatmapPlayer))]
    public class BeatmapPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Convert target into component
            BeatmapPlayer player = (BeatmapPlayer)target;

            // NAIVE SOLUTION: Define beatmap objects data in inspector GUI one by one
            Beatmap beatmap = player.CurrentBeatmap;
            if (beatmap.Song != null)
            {
                // Clear/Reset objects button
                GUILayout.Space(8f);
                if (GUILayout.Button("Clear Objects"))
                {
                    beatmap.ClearBeatmapObjects();
                }

                // Create objects GUI section
                GUILayout.BeginVertical("Objects", "window");
                for (int i = 0; i < beatmap.GetSequenceCount(); i++)
                {
                    GUILayout.BeginVertical($"Sequence - {i}", "window");

                    // Define all subsequences
                    for (int j = 0; j < beatmap.SequenceDivision.GetDivisionDivider(); j++)
                    {
                        int[][][] obj = (int[][][])beatmap.ObjectsData;

                        // Define column and key
                        GUILayout.BeginVertical($"Subsequence - {j}", "window");
                        obj[i][j][0] = (int)(NoteType)EditorGUILayout.EnumPopup("Column 1", (NoteType)obj[i][j][0]);
                        obj[i][j][1] = (int)(NoteType)EditorGUILayout.EnumPopup("Column 2", (NoteType)obj[i][j][1]);
                        obj[i][j][2] = (int)(NoteType)EditorGUILayout.EnumPopup("Column 3", (NoteType)obj[i][j][2]);
                        obj[i][j][3] = (int)(NoteType)EditorGUILayout.EnumPopup("Column 4", (NoteType)obj[i][j][3]);
                        GUILayout.EndVertical();
                        GUILayout.Space(8f);
                    }

                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }

            // Check changes
            if (GUI.changed)
            {
                EditorUtility.SetDirty(player);
            }
        }
    }
    #endif

    public class BeatmapPlayer : MonoBehaviour
    {
        /// <summary>
        /// Event called when a new beatmap inserted to be create.
        /// </summary>
        public static event System.Action<Beatmap>          OnSetBeatmapIntoPlayer;

        /// <summary>
        /// Event called before the end play called for a certain of time delay.
        /// </summary>
        public static event System.Action                   OnPreEndPlay;

        /// <summary>
        /// Event called when start play the level in game.
        /// This should change any state into Gameplay state.
        /// </summary>
        public static event System.Action                   OnBeginPlay;

        /// <summary>
        /// Event called when sequence index on perfect line.
        /// </summary>
        public static event System.Action<int, int>         OnSequenceChange;

        /// <summary>
        /// Event called when there is no more notes on every column in gameplay only.
        /// This keeps stay in the Gameplay State.
        /// </summary>
        public static event System.Action                   OnEndPlay;

        // Singleton behaviour
        private static BeatmapPlayer                        _S;

        [Header("Requirements")]
        [SerializeField] private List<NoteColumn>           _columns = new List<NoteColumn>();

        [Header("Attributes")]
        [SerializeField] private float                      _endGameDelaySeconds = 3f;

        // Temporary variables
        [BoxGroup("DEBUG"), SerializeField, ReadOnly] 
        private InputMap                                    _inputMap = new InputMap();
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Beatmap                                     _beatMap = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        internal Character                                  _choosenCharacter = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                                         _sequenceIndex = 0;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                                         _subSequenceIndex = 0;

        #region Properties
        public static BeatmapPlayer Singleton => _S;
        internal Beatmap CurrentBeatmap 
        {
            set => _beatMap = value;
            get => _beatMap;
        }
        #endregion

        #region Unity BuiltIn Methods
        private void Awake()
        {
            // Check singleton exists
            if (_S != null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"Deleted extra object of singleton behaviour {this}, you can ignore this warning.");
                #endif
                Destroy(this);
                return;
            }

            // Set singleton
            _S = this;

            // Subscribe events
            UIGameManager.OnCountdownFinish += HandleFinishedCountdown;
            EventHandler.OnEditorModeActiveEvent += HandleEditorModeActive;
            EventHandler.OnHitNoteEvent += HandleHitNote;
            EventHandler.OnHoldNoteFinishEvent += HandleHoldNote;
        }

        private void HandleFinishedCountdown()
        {
            // Load beatmap
            SetBeatmap(GameManager.Singleton._selectedMap);

            // Play beatmap
            PlayBeatMap(UIMenuManager.InputControl);
        }

        private void Start()
        {
            // Check editor mode
            if (!UIGameManager.IsEditorModeActive)
            {
                // Set character
                Character @char = GameManager.Singleton.Asset.GetCharacter(GameManager.Singleton._characterKey);
                if (@char != null)
                {
                    _choosenCharacter = Instantiate(@char);
                    _choosenCharacter.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            // Check playing beatmap
            if (SoundMaster.Singleton.IsPlaying)
            {
                // Check sequence and subsequence on perfect hit line
                float currentTime = SoundMaster.Singleton.AtSongTime;
                if (_sequenceIndex != GetSequenceIndex(_beatMap, currentTime) || 
                    _subSequenceIndex != GetSubSequenceIndex(_beatMap, currentTime))
                {
                    // Set new sequence
                    _sequenceIndex = GetSequenceIndex(_beatMap, currentTime);
                    _subSequenceIndex = GetSubSequenceIndex(_beatMap, currentTime);

                    // Call event
                    OnSequenceChange?.Invoke(_sequenceIndex, _subSequenceIndex);
                }
            }
        }

        private void OnDestroy()
        {
            // Release singleton if match
            if (this.Equals(_S))
            {
                _S = null;

                // Unsubscribe events
                UIGameManager.OnCountdownFinish -= HandleFinishedCountdown;
                EventHandler.OnEditorModeActiveEvent -= HandleEditorModeActive;
                EventHandler.OnHitNoteEvent -= HandleHitNote;
                EventHandler.OnHoldNoteFinishEvent -= HandleHoldNote;
            }
        }
        #endregion

        #region Event Methods
        private void HandleEditorModeActive(EditorModeActiveEventArgs args)
        {
            // Check exit editor mode, the reply the level
            if (!args.IsSetActive)
            {
                SetBeatmap(GameManager.Singleton._selectedMap);
                PlayBeatMap(UIMenuManager.InputControl);
            }
        }

        private void HandleHitNote(NoteHitEventArgs args)
        {
            // Check if objects are exists in every column
            if (!_columns.Any(column => column.GetObjectsLeftover() > 0) && !UIGameManager.IsEditorModeActive)
            {
                // End the gameplay
                EndGame();
            }
        }

        private void HandleHoldNote(HoldNoteFinishedEventArgs args)
        {
            // Check if objects are exists in every column
            if (!_columns.Any(column => column.GetObjectsLeftover() > 0) && !UIGameManager.IsEditorModeActive)
            {
                // End the gameplay
                EndGame();
            }
        }
        #endregion

        public void SetBeatmap(Beatmap beatMap)
        {
            // Setting beatmap into beatmap player
            this._beatMap = beatMap;

            // Set column index
            for (int i = 0; i < _columns.Count; i++)
            {
                // Set column index
                NoteColumn col = _columns[i];
                col.ColumnIndex = i;
            }

            // Set song clip
            SoundMaster.Singleton.SetSongClip(beatMap.Song);

            // Call event
            OnSetBeatmapIntoPlayer?.Invoke(_beatMap);
        }

        public void PlayBeatMap(InputMap inputMap)
        {
            // Check if beatmap is not selected
            if (_beatMap.Song == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning("WARNING: Beatmap was not set into player, abort the process.");
                #endif
                return;
            }

            // Set all requirements
            this._inputMap.keys = inputMap.keys;

            // Determine sequence index and subs
            SoundMaster.Singleton.AtSongTime = _beatMap.StartPointSeconds;
            _sequenceIndex = GetSequenceIndex(_beatMap, _beatMap.StartPointSeconds);
            _subSequenceIndex = GetSubSequenceIndex(_beatMap, _beatMap.StartPointSeconds);

            // Set control key to each column
            for (int i = 0; i < _columns.Count; i++)
            {
                // Initialize controller keys
                NoteColumn col = _columns[i];
                col.ControlKey = inputMap.keys[i];
            }

            // Call events
            OnBeginPlay?.Invoke();
            OnSequenceChange?.Invoke(_sequenceIndex, _subSequenceIndex);

            // Check if there is no objects in beatmap, immediately call for end game
            if (!_columns.Any(column => column.GetObjectsLeftover() > 0) && !UIGameManager.IsEditorModeActive)
            {
                // End the gameplay
                EndGame();
            }
        }

        /// <summary>
        /// Set all existing columns active (Enable or Disable).
        /// </summary>
        /// <param name="active">Set active game object</param>
        public void SetColumnActive(bool active)
        {
            // Loop through each columns
            for (int i = 0; i < _columns.Count; i++)
            {
                // Check if it is not null object
                NoteColumn column = _columns[i];
                if (column != null) column.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Calculate BPM and at song current time into sequence definition.
        /// </summary>
        /// <param name="beatmap">Target beatmap</param>
        /// <param name="atSongTime">At song time</param>
        /// <returns>Sequence index</returns>
        public static int GetSequenceIndex(Beatmap beatmap, float atSongTime)
        {
            // Return sequence index
            return (int)(atSongTime / beatmap.GetTimePerSequence());
        }

        /// <summary>
        /// Calculate sub sequence index of current index.
        /// </summary>
        /// <returns></returns>
        public static int GetSubSequenceIndex(Beatmap beatmap, float atSongTime)
        {
            // Get current sequence and time per sequence
            int currentSequence = GetSequenceIndex(beatmap, atSongTime);
            float timePerSequence = beatmap.GetTimePerSequence();

            // Calculate seconds range between rows in sequence
            float subSequenceTime = beatmap.GetTimePerSubSequence();

            // Calculate which current subsequence index
            return (int)((atSongTime - (currentSequence * timePerSequence)) / subSequenceTime);
        }

        private void EndGame()
        {
            // Call event
            OnPreEndPlay?.Invoke();

            // End game
            StartCoroutine(DelayEndGame());
        }

        private IEnumerator DelayEndGame()
        {
            // Wait for delay
            float temp = _endGameDelaySeconds;
            while (temp > 0f && SoundMaster.Singleton.IsPlaying)
            {
                temp -= Time.deltaTime;
                yield return null;
            }

            // Call event
            OnEndPlay?.Invoke();
        }
    }

}

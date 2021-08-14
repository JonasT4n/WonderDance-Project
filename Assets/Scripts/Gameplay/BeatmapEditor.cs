using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NaughtyAttributes;
using TMPro;

namespace WonderDanceProj
{
    /// <summary>
    /// For level designer to edit current beatmap.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BeatmapEditor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// Event called when BPM was edited or changed.
        /// </summary>
        public static event System.Action<int>                          OnBPMChange;

        /// <summary>
        /// Event called when division was changed.
        /// </summary>
        public static event System.Action<Division>                     OnDivisionChange;

        /// <summary>
        /// Event called when drop speed was edited or changed.
        /// </summary>
        public static event System.Action<float>                        OnDropSpeedChange;

        /// <summary>
        /// Event called when cancel editing beatmap and reverting it back to previous one.
        /// </summary>
        public static event System.Action                               OnEditingRevert;

        /// <summary>
        /// Event called when song time is changed.
        /// </summary>
        public static event System.Action<float>                        OnTimeChange;

        /// <summary>
        /// Event called when testing is stopped.
        /// </summary>
        public static event System.Action                               OnStopTesting;

        /// <summary>
        /// Event called when hold note has been created.
        /// Insert target column, start and end subline index.
        /// </summary>
        public static event System.Action<NoteColumn, int, int>         OnHoldNoteCreated;

        /// <summary>
        /// Event called when an object in world is being move by cursor.
        /// It needs the object itself and the offset between cursor object in world position to call.
        /// </summary>
        public static event System.Action<Transform, Vector3>           OnWorldObjectBeingMove;

        [Header("UI Inputs")]
        [SerializeField]
        private TMP_InputField      _bpmInput = null;
        [SerializeField]
        private TMP_Dropdown        _divisionDropdown = null;
        [SerializeField]
        private TMP_InputField      _dropSpeedInput = null;
        [SerializeField]
        private Button              _saveButton = null;
        [SerializeField]
        private Button              _revertButton = null;

        [Header("UI Music Player")]
        [SerializeField]
        private Slider              _timeSlider = null;
        [SerializeField]
        private TextMeshProUGUI     _timeHint = null;
        [SerializeField]
        private Button              _playButton = null;
        [SerializeField]
        private Button              _stopButton = null;

        [Header("UI Debug Information")]
        [SerializeField]
        private TextMeshProUGUI     _mapNameTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _sequenceIndexTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _atSequenceTimeHintTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _sequenceCountTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _sublinesCountTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _mousePosHintTxt = null;

        [Header("UI Tool Activator")]
        [SerializeField]
        private Transform           _objHintPrefab = null;
        [SerializeField]
        private Button              _eraser = null;
        [SerializeField]
        private Button              _hitNotePainter = null;
        [SerializeField]
        private Button              _holdNotePainter = null;
        [SerializeField]
        private Color               _activationColor = Color.grey;

        [Header("Attributes")]
        [SerializeField]
        private LayerMask           _editorMask = 0;

        // Temporary variables
        [BoxGroup("DEBUG"), SerializeField]
        private bool                _debug = false;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Beatmap             _oldBeatmap = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Beatmap             _newBeatmap = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Transform           _targetObject = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private NoteColumn          _targetColumn = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Vector3             _mouseLocalCoordTarget = Vector3.zero;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Vector3             _startObjPos = Vector3.zero;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _atSublineIndexHint = 0;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private Transform           _objHint = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private NoteType            _paintType = NoteType.Blank;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private bool                _isPaintModeActive = false;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private bool                _isPainting = false;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        [Header("Hold Note Editing Only")]
        private HoldNote            _holdNoteTemp = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _startHoldNoteSubline = -1;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _endHoldNoteSubline = -1;

        #region Unity BuiltIn Methods
        private void Awake()
        {
            // Subscribe events
            UIGameManager.OnPauseGameActive += HandlePauseActive;
            BeatmapPlayer.OnSequenceChange += HandlePerfectLineSequenceChange;
        }

        private void OnEnable()
        {
            // Clear previous beatmap editing
            _bpmInput.onValueChanged.RemoveAllListeners();
            _divisionDropdown.onValueChanged.RemoveAllListeners();
            _dropSpeedInput.onValueChanged.RemoveAllListeners();
            _saveButton.onClick.RemoveAllListeners();
            _revertButton.onClick.RemoveAllListeners();
            _timeSlider.onValueChanged.RemoveAllListeners();
            _playButton.onClick.RemoveAllListeners();
            _stopButton.onClick.RemoveAllListeners();
            _eraser.onClick.RemoveAllListeners();
            _hitNotePainter.onClick.RemoveAllListeners();
            _holdNotePainter.onClick.RemoveAllListeners();

            // Check current player doesn't have a beatmap to be play
            _oldBeatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            if (_oldBeatmap.Song == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"There is no Beatmap set currently in player.");
                #endif
                gameObject.SetActive(false);
                return;
            }

            // Save old beatmap by reverting changes
            RevertChanges();

            // Set other debug info, calculate time on that index, then break down into minutes, seconds, and miliseconds
            int si = BeatmapPlayer.GetSequenceIndex(_newBeatmap, SoundMaster.Singleton.AtSongTime);
            int ssi = BeatmapPlayer.GetSubSequenceIndex(_newBeatmap, SoundMaster.Singleton.AtSongTime); ;
            UpdateCurrentPerfectLineInfo(si, ssi);
            _startHoldNoteSubline = -1;

            // Subscribe UI events
            _bpmInput.onValueChanged.AddListener(val =>
            {
                // Check input empty, then ignore changes
                if (string.IsNullOrEmpty(val))
                {
                    _bpmInput.text = $"{_newBeatmap.BPM}";
                    return;
                }

                // Check input is the same value
                int bpm = int.Parse(val);
                if (bpm == _newBeatmap.DropSpeed) return;

                // Set bpm
                _newBeatmap.BPM = bpm;

                // Call event
                OnBPMChange?.Invoke(_newBeatmap.BPM);
            });
            _divisionDropdown.onValueChanged.AddListener(index =>
            {
                // Set division
                _newBeatmap.TemporaryDivision = (Division)index;
            });
            _dropSpeedInput.onValueChanged.AddListener(val =>
            {
                // Check input empty, then ignore changes
                if (string.IsNullOrEmpty(val))
                {
                    _dropSpeedInput.text = $"{_newBeatmap.DropSpeed}";
                    return;
                }

                // Check input is the same value
                int dropSpeed = int.Parse(val);
                if (dropSpeed == _newBeatmap.DropSpeed) return;

                // Set drop speed
                _newBeatmap.DropSpeed = dropSpeed;

                // Call event
                OnDropSpeedChange?.Invoke(_newBeatmap.DropSpeed);
            });
            _saveButton.onClick.AddListener(() => SaveEditBeatmap());
            _revertButton.onClick.AddListener(() => RevertChanges());
            _timeSlider.onValueChanged.AddListener(atTime =>
            {
                // Update info only
                int si = BeatmapPlayer.GetSequenceIndex(_newBeatmap, SoundMaster.Singleton.AtSongTime);
                int ssi = BeatmapPlayer.GetSubSequenceIndex(_newBeatmap, SoundMaster.Singleton.AtSongTime); ;
                UpdateCurrentPerfectLineInfo(si, ssi);

                // Check whether the speaker is currently playing the song, then ignore
                if (SoundMaster.Singleton.IsPlaying || !UIGameManager.IsEditorModeActive) return;

                // Set at time play
                SoundMaster.Singleton.AtSongTime = atTime;
                _timeHint.text = ToTimeString(_timeSlider.value);

                // Call event
                OnTimeChange?.Invoke(atTime);
            });
            _playButton.onClick.AddListener(() => SetPlayTest(true));
            _stopButton.onClick.AddListener(() => SetPlayTest(false));
            _eraser.onClick.AddListener(() => SelectPainter(NoteType.Blank));
            _hitNotePainter.onClick.AddListener(() => SelectPainter(NoteType.HitNote));
            _holdNotePainter.onClick.AddListener(() => SelectPainter(NoteType.HoldNote));
        }

        private void Update()
        {
            // Shortcut play/pause control
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Check if current test play is active
                if (!_playButton.interactable) SetPlayTest(false);
                else SetPlayTest(true);
            }

            // Update UI at song time when playing
            if (SoundMaster.Singleton.IsPlaying)
            {
                _timeSlider.value = SoundMaster.Singleton.AtSongTime;
                _timeHint.text = ToTimeString(_timeSlider.value);
                return;
            }

            // Check a column is being drag
            if (_targetObject != null && !_isPaintModeActive)
            {
                // Calculate target position at zero local mouse position
                float yTargetDir = Camera.main.ScreenToWorldPoint(Input.mousePosition).y - _mouseLocalCoordTarget.y;
                Vector3 targetPos = new Vector3(_targetObject.position.x, yTargetDir + _startObjPos.y, 0f);

                // Call event
                OnWorldObjectBeingMove?.Invoke(_targetObject, targetPos);

                // Update current UI timeline
                _timeSlider.value = SoundMaster.Singleton.AtSongTime;
                _timeHint.text = ToTimeString(_timeSlider.value);
            }

            // Check drawing notes into column
            if (_isPaintModeActive)
            {
                HandleWorldObjDetection();
                if (_targetColumn != null)
                {
                    // Get subline index, then convert into snapped position on that index, default direction is 'down'
                    float detectionOffset = _newBeatmap.GetTimePerSubSequence() * _newBeatmap.DropSpeed / 2f;
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, detectionOffset);
                    _atSublineIndexHint = _targetColumn.GetSublineByPosition(mousePos);
                    _isPainting = Input.GetMouseButton(0);

                    // Write down mouse sequence position hint
                    int si = _atSublineIndexHint / _newBeatmap.SequenceDivision.GetDivisionDivider();
                    int ssi = _atSublineIndexHint % _newBeatmap.SequenceDivision.GetDivisionDivider();
                    _mousePosHintTxt.text = $"{si} - {ssi} - {_targetColumn.ColumnIndex}";

                    // SPECIAL CASE: Handle painting hold note
                    if (_holdNoteTemp != null)
                    {
                        if (_isPainting)
                        {
                            // Disable object hint
                            if (_objHint.gameObject.activeSelf) _objHint.gameObject.SetActive(false);

                            // Handle drawing note on column
                            HandleDrawing();
                        }
                        else
                        {
                            // Set and save hold note object
                            int rangeNote = _endHoldNoteSubline - _startHoldNoteSubline + 1;
                            _newBeatmap.AddHoldObjectKey(_startHoldNoteSubline, _targetColumn.ColumnIndex, range: rangeNote);

                            // Call event
                            OnHoldNoteCreated?.Invoke(_targetColumn, _startHoldNoteSubline, _endHoldNoteSubline);

                            // Release temporary data
                            _holdNoteTemp = null;
                            _targetColumn = null;
                            _targetObject = null;
                            _endHoldNoteSubline = -1;
                            _startHoldNoteSubline = -1;
                        }
                        return;
                    }

                    // Check if the note is already occupied and going to build the note
                    if (!_targetColumn.IsNoteOccupied(_atSublineIndexHint) && _paintType != NoteType.Blank)
                    {
                        // Activate and set object hint position
                        if (!_objHint.gameObject.activeSelf) _objHint.gameObject.SetActive(true);
                        _objHint.position = new Vector2(_targetObject.position.x, _targetColumn.GetPositionByIndex(_atSublineIndexHint).y);

                        // Handle drawing notes on column
                        HandleDrawing();
                    }
                    else if (_targetColumn.IsNoteOccupied(_atSublineIndexHint) && _paintType == NoteType.Blank)
                    {
                        // Activate and set object hint position
                        if (!_objHint.gameObject.activeSelf) _objHint.gameObject.SetActive(true);
                        _objHint.position = new Vector2(_targetObject.position.x, _targetColumn.GetPositionByIndex(_atSublineIndexHint).y);

                        // Handle erasing notes on column
                        HandleDrawing();
                    }
                }
                else if (_objHint != null)
                {
                    // Deactivate hint if not detected
                    if (_targetColumn == null && _objHint.gameObject.activeSelf)
                    {
                        _objHint.gameObject.SetActive(false);
                    }

                    // Reset mouse hint text
                    _mousePosHintTxt.text = "XX - XX - XX";
                }
            }
        }

        private void OnDisable()
        {
            // Disable everything
            DisableTools();
        }

        private void OnDestroy()
        {
            // Unsubscribe events
            UIGameManager.OnPauseGameActive -= HandlePauseActive;
            BeatmapPlayer.OnSequenceChange -= HandlePerfectLineSequenceChange;
        }
        #endregion

        #region Event Methods
        private void HandlePauseActive(bool active)
        {
            // Check if currently testing while playing in editor mode, then stop it immediately
            if (!_playButton.interactable)
                SetPlayTest(false);
        }

        private void HandlePerfectLineSequenceChange(int si, int ssi)
        {
            // Update secondary info
            UpdateCurrentPerfectLineInfo(si, ssi);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Cannot call when the song is playing
            if (SoundMaster.Singleton.IsPlaying || _isPaintModeActive) return;

            // Check any object in world is detected
            _mouseLocalCoordTarget = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(_mouseLocalCoordTarget, Vector2.zero, 1f, _editorMask);

            #if UNITY_EDITOR // Debug
            if (_debug)
            {
                Color color = hit.collider != null ? Color.green : Color.red;
                Debug.DrawRay(_mouseLocalCoordTarget, Vector2.left * 0.5f, color, 5f);
                Debug.DrawRay(_mouseLocalCoordTarget, Vector2.up * 0.5f, color, 5f);
                Debug.DrawRay(_mouseLocalCoordTarget, Vector2.down * 0.5f, color, 5f);
                Debug.DrawRay(_mouseLocalCoordTarget, Vector2.right * 0.5f, color, 5f);
            }
            #endif

            // Check object raycast found
            if (hit.collider != null)
            {
                // Check if player select a column
                NoteColumn column = hit.collider.GetComponentInParent<NoteColumn>();
                if (column != null)
                {
                    // Assign target
                    _targetObject = hit.collider.transform;
                    _startObjPos = _targetObject.position;

                    #if UNITY_EDITOR // Debug
                    if (_debug)
                    {
                        Debug.Log($"Column detected: {_targetObject}");
                    }
                    #endif
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Release moving cache
            if (!_isPaintModeActive)
                _targetObject = null;
        }
        #endregion

        private void HandleWorldObjDetection()
        {
            // Cannot call when the song is playing
            if (SoundMaster.Singleton.IsPlaying || !_isPaintModeActive) return;

            // Check hold note creation, then lock the current built
            if (_paintType == NoteType.HoldNote && _startHoldNoteSubline != -1) return;

            // Check any object in world is detected
            _mouseLocalCoordTarget = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(_mouseLocalCoordTarget, Vector2.zero, 1f, _editorMask);

            // Check object raycast found
            if (hit.collider != null && _targetColumn == null)
            {
                #if UNITY_EDITOR // Debug
                if (_debug)
                {
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.left * 0.5f, Color.green, 5f);
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.up * 0.5f, Color.green, 5f);
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.down * 0.5f, Color.green, 5f);
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.right * 0.5f, Color.green, 5f);
                }
                #endif

                // Check pointer entering column area
                _targetObject = hit.collider.transform;
                _targetColumn = _targetObject.GetComponentInParent<NoteColumn>();
            }
            else if (hit.collider == null && _targetColumn != null)
            {
                #if UNITY_EDITOR // Debug
                if (_debug)
                {
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.left * 0.5f, Color.red, 5f);
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.up * 0.5f, Color.red, 5f);
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.down * 0.5f, Color.red, 5f);
                    Debug.DrawRay(_mouseLocalCoordTarget, Vector2.right * 0.5f, Color.red, 5f);
                }
                #endif

                // Release column holder object
                _targetColumn = null;
                _targetObject = null;

                // Reset mouse hint text
                _mousePosHintTxt.text = "XX - XX - XX";
            }
        }

        private void HandleDrawing()
        {
            // Check control drawing active
            if (_isPainting && _targetColumn != null)
            {
                // Check which paint tool will be using
                if (_paintType == NoteType.Blank)
                {
                    // Delete that note
                    _newBeatmap.DeleteObjectKey(_atSublineIndexHint, _targetColumn.ColumnIndex);

                    // Create object into that column
                    _targetColumn.DeleteObject(_atSublineIndexHint);
                }
                else if (_paintType == NoteType.HitNote)
                {
                    // Add into object data
                    _newBeatmap.AddHitObjectKey(_atSublineIndexHint, _targetColumn.ColumnIndex);

                    // create object into that column
                    _targetColumn.CreateNoteObject(NoteType.HitNote, _atSublineIndexHint);
                }
                else // SPECIAL CASE: await for hold note to be release, then set an object
                {
                    // Get starting point
                    if (_startHoldNoteSubline == -1)
                    {
                        // Set start point
                        _targetColumn.CreateNoteObject(NoteType.HoldNote, _atSublineIndexHint);
                        _holdNoteTemp = (HoldNote)_targetColumn.GetNoteObject(_atSublineIndexHint);
                        _startHoldNoteSubline = _atSublineIndexHint;
                    }
                    else if (_startHoldNoteSubline >= _atSublineIndexHint)
                    {
                        // Ignore when subline is less than start index
                        _atSublineIndexHint = _startHoldNoteSubline + 1;
                    }

                    // Check hold note is being extended or decreased
                    if (_holdNoteTemp != null)
                    {
                        // Check and set end hold
                        if (_endHoldNoteSubline != _atSublineIndexHint)
                        {
                            _endHoldNoteSubline = _atSublineIndexHint;
                        }

                        _holdNoteTemp.SetEndSublineIndex(_atSublineIndexHint);
                    }
                }
            }
            else
            {
                // Reset temporary values
                _startHoldNoteSubline = -1;
            }
        }

        public static string ToTimeString(float timeInSeconds)
        {
            // Calculate minutes and seconds
            int minute = (int)(timeInSeconds / 60f);
            int second = (int)(timeInSeconds % 60f);

            // Convert to time string
            string strMinute = minute < 10 ? $"0{minute}" : $"{minute}";
            string strSecond = second < 10 ? $"0{second}" : $"{second}";
            return $"{strMinute}:{strSecond}";
        }

        /// <summary>
        /// Revert any changes back to where it was.
        /// </summary>
        public void RevertChanges()
        {
            // Set back to old beatmap data, update all ui changes
            _newBeatmap = (Beatmap)_oldBeatmap.Clone();
            BeatmapPlayer.Singleton.CurrentBeatmap = _newBeatmap;
            _mapNameTxt.text = _oldBeatmap.MapName;
            _bpmInput.text = $"{_oldBeatmap.BPM}";
            _divisionDropdown.value = (int)_oldBeatmap.TemporaryDivision;
            _dropSpeedInput.text = $"{_oldBeatmap.DropSpeed}";
            _timeSlider.maxValue = _oldBeatmap.Song.length;
            _timeSlider.minValue = 0f;
            _timeSlider.value = SoundMaster.Singleton.AtSongTime;
            _playButton.interactable = !SoundMaster.Singleton.IsPlaying;
            _bpmInput.interactable = _playButton.interactable;
            _stopButton.interactable = !_playButton.interactable;
            _timeHint.text = ToTimeString(_timeSlider.value);

            // Update secondary debug info
            int sequenceCount = _newBeatmap.GetSequenceCount();
            _sequenceCountTxt.text = $"{sequenceCount}";
            _sublinesCountTxt.text = $"{sequenceCount * _newBeatmap.SequenceDivision.GetDivisionDivider()}";

            // Call event
            OnEditingRevert?.Invoke();
        }

        /// <summary>
        /// Save current beatmap.
        /// </summary>
        public void SaveEditBeatmap()
        {
            // Call event
            SaveEditBeatmapEventArgs arg = new SaveEditBeatmapEventArgs(_oldBeatmap, _newBeatmap);
            EventHandler.CallEvent(arg);
            if (!arg.IsCancelled)
            {
                // Save new beatmap
                _oldBeatmap = _newBeatmap;

                // Save current beatmap into external data folder
                DataFileLoader.SaveData(arg.EditedMap);
            }
        }

        private void UpdateCurrentPerfectLineInfo(int si, int ssi)
        {
            // Calculate time on that index, then break down into minutes, seconds, and miliseconds
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            float time = beatmap.GetTimeAtSequence(si, ssi);
            string timeStr = ToTimeString(time);
            int miliSec = (int)(time * 1000f % 1000f);
            string miliSecStr = miliSec < 10 ? $"00{miliSec}" : miliSec < 100 ? $"0{miliSec}" : $"{miliSec}";

            // Update UI debug in editor
            _sequenceIndexTxt.text = $"SQ {si}-{ssi}";
            _atSequenceTimeHintTxt.text = $"{timeStr}.{miliSecStr}";
        }

        private void SetPlayTest(bool test)
        {
            // Set UI actives when song is playing or has been stopped
            _playButton.interactable = !test;
            _bpmInput.interactable = !test;
            _stopButton.interactable = test;
            _divisionDropdown.interactable = !test;
            _dropSpeedInput.interactable = !test;
            _saveButton.interactable = !test;
            _revertButton.interactable = !test;
            _eraser.interactable = !test;
            _hitNotePainter.interactable = !test;
            _holdNotePainter.interactable = !test;

            // Check testing mode active or not
            if (test)
            {
                // Set play speaker for testing
                SoundMaster.Singleton.Play();

                // Disable tool usability when going testing mode
                DisableTools();
            }
            else
            {
                // Set stop speaker for testing
                SoundMaster.Singleton.Stop();

                // Call event
                OnStopTesting?.Invoke();
            }
        }

        private void DisableTools()
        {
            // Check and disable active tools
            _isPaintModeActive = false;

            // Release column holder object
            _targetColumn = null;
            _targetObject = null;

            // Destroy hint object if exists
            if (_objHint != null) Destroy(_objHint.gameObject);

            // Set inactive tools color
            _eraser.image.color = Color.white;
            _hitNotePainter.image.color = Color.white;
            _holdNotePainter.image.color = Color.white;
        }

        private void SelectPainter(NoteType type)
        {
            // Check deactivation
            if (_isPaintModeActive && _paintType == type)
            {
                // Destroy object hint
                Destroy(_objHint.gameObject);

                // Set inactive color
                _eraser.image.color = Color.white;
                _hitNotePainter.image.color = Color.white;
                _holdNotePainter.image.color = Color.white;

                // Deactivate painter tool
                _isPaintModeActive = false;
                _isPainting = false;
                return;
            }

            // Set paint type
            _paintType = type;
            _isPaintModeActive = true;

            // Create object hint placeholder if not exists
            if (_objHint == null)
            {
                _objHint = Instantiate(_objHintPrefab);
                _objHint.gameObject.SetActive(false);
            }

            // Set color activation for edit
            switch (type)
            {
                case NoteType.Blank:
                    _eraser.image.color = _activationColor;
                    _hitNotePainter.image.color = Color.white;
                    _holdNotePainter.image.color = Color.white;
                    break;

                case NoteType.HitNote:
                    _eraser.image.color = Color.white;
                    _hitNotePainter.image.color = _activationColor;
                    _holdNotePainter.image.color = Color.white;
                    break;

                default: // Hold Note
                    _eraser.image.color = Color.white;
                    _hitNotePainter.image.color = Color.white;
                    _holdNotePainter.image.color = _activationColor;
                    break;
            }
        }
    }

}

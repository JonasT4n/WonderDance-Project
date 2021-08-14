using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WonderDanceProj
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(NoteColumn))]
    public class NoteColumnEditor : Editor
    {
        // Create my custom inspector
        public override void OnInspectorGUI()
        {
            // Update serialized field objects
            serializedObject.Update();

            // Convert target into component
            NoteColumn column = (NoteColumn)target;

            // Components section
            EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
            EditorGUILayout.LabelField("Components", EditorGUIStyles.GetLabelHeaderStyle());
            EditorGUILayout.Space(8f);
            var collider = serializedObject.FindProperty("_areaDetect");
            EditorGUILayout.ObjectField(collider, new GUIContent("Area Detector"));
            EditorGUILayout.EndVertical();

            // Spacing
            EditorGUILayout.Space(4f);

            // Requirements section
            EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
            EditorGUILayout.LabelField("Requirements", EditorGUIStyles.GetLabelHeaderStyle());
            EditorGUILayout.Space(8f);
            var perfectLine = serializedObject.FindProperty("_perfectHitLine");
            EditorGUILayout.ObjectField(perfectLine, new GUIContent("Perfect Line"));
            var hitLineRenderer = serializedObject.FindProperty("_hitLineRenderer");
            EditorGUILayout.ObjectField(hitLineRenderer, new GUIContent("Hit Line Renderer"));
            var noteContainer = serializedObject.FindProperty("_noteObjectContainer");
            EditorGUILayout.ObjectField(noteContainer, new GUIContent("Note Container"));
            EditorGUILayout.EndVertical();

            // Spacing
            EditorGUILayout.Space(4f);

            // Prefabs section
            EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
            EditorGUILayout.LabelField("Prefabs", EditorGUIStyles.GetLabelHeaderStyle());
            EditorGUILayout.Space(8f);
            var emptyLine = serializedObject.FindProperty("_emptyLinePrefab");
            EditorGUILayout.ObjectField(emptyLine, new GUIContent("Empty Line"));
            var hitNote = serializedObject.FindProperty("_hitNotePrefab");
            EditorGUILayout.ObjectField(hitNote, new GUIContent("Hit Note"));
            var holdNote = serializedObject.FindProperty("_holdNotePrefab");
            EditorGUILayout.ObjectField(holdNote, new GUIContent("Hold Note"));
            EditorGUILayout.EndVertical();

            // Spacing
            EditorGUILayout.Space(4f);

            // Other Attribute Section
            EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
            EditorGUILayout.LabelField("Other Attributes", EditorGUIStyles.GetLabelHeaderStyle());
            EditorGUILayout.Space(8f);
            var releasedColor = serializedObject.FindProperty("_releasedColor");
            releasedColor.colorValue = EditorGUILayout.ColorField("On Release Color", releasedColor.colorValue);
            var onPressColor = serializedObject.FindProperty("_pressedColor");
            onPressColor.colorValue = EditorGUILayout.ColorField("On Press Color", onPressColor.colorValue);
            EditorGUILayout.EndVertical();

            // Spacing
            EditorGUILayout.Space(4f);

            // Debug section
            EditorGUILayout.BeginVertical(EditorGUIStyles.GetBoxStyle(Color.gray));
            EditorGUILayout.LabelField("Debug");
            GUI.enabled = false;
            EditorGUILayout.EnumPopup("Input Control Key", column.ControlKey);
            EditorGUILayout.IntField("At Column Index", column.ColumnIndex);
            EditorGUILayout.FloatField("Drop Speed", column._dropSpeed);
            EditorGUILayout.Toggle("Is Control Press Down", column._isControlDown);
            EditorGUILayout.Toggle("Is Control Press Hold", column._isControlHold);
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            // Check changes
            if (GUI.changed)
            {
                // Update changes
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(column);
            }
        }
    }
    #endif

    public class NoteColumn : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private BoxCollider2D       _areaDetect = null;

        [Header("Requirements")]
        [SerializeField]
        [Tooltip("Specific position when player hit control with perfect score. This must be below the spawn point.")]
        private Transform           _perfectHitLine = null;
        [SerializeField]
        private SpriteRenderer      _hitLineRenderer = null;
        [SerializeField]
        private BoxCollider2D       _noteObjectContainer = null;

        [Header("Prefabs")]
        [SerializeField] 
        private BlankNote           _emptyLinePrefab = null;
        [SerializeField] 
        private HitNote             _hitNotePrefab = null;
        [SerializeField] 
        private HoldNote            _holdNotePrefab = null;

        [Header("Attributes")]
        [SerializeField]
        [Tooltip("When control button released, it will change to this color.")]
        private Color               _releasedColor = Color.gray;
        [SerializeField]
        [Tooltip("When control button pressed, it will change to this color.")]
        private Color               _pressedColor = Color.gray;

        // Temporary variables
        private Dictionary<int, INoteKey>       _noteObjects = new Dictionary<int, INoteKey>();
        private Dictionary<int, BlankNote>      _blankNoteObjs = new Dictionary<int, BlankNote>();
        private int                             _atColumnIndex = 0;
        internal float                          _dropSpeed = 0f;
        internal bool                           _isControlDown = false;
        internal bool                           _isControlHold = false;

        #region Properties
        public KeyCode ControlKey { set; get; }
        public int ColumnIndex
        {
            set
            {
                // Set column index
                _atColumnIndex = value;

                // Render with assets by index
                _hitLineRenderer.sprite = GameManager.Singleton.Asset.GetKeySpriteByIndex(_atColumnIndex);
            }
            get => _atColumnIndex;
        }
        #endregion

        #region Unity BuiltIn Methods
        private void Awake()
        {
            // Subscribe event
            EventHandler.OnNoteMissedEvent += HandleNoteMissed;
            BeatmapPlayer.OnSetBeatmapIntoPlayer += HandleSetBeatmapToPlayer;
            BeatmapEditor.OnStopTesting += HandleStopTesting;
            BeatmapEditor.OnBPMChange += HandleBPMChange;
            BeatmapEditor.OnDropSpeedChange += HandleDropSpeedChange;
            BeatmapEditor.OnWorldObjectBeingMove += HandleMovingContainer;
            BeatmapEditor.OnTimeChange += HandleTimePostionChange;
            BeatmapEditor.OnEditingRevert += HandleEditorRevert;
            BeatmapEditor.OnHoldNoteCreated += HandleHoldNoteCreated;
        }

        private void Update()
        {
            // Check game running
            if (SoundMaster.Singleton.IsPlaying)
            {
                // Check hit control
                _isControlDown = Input.GetKeyDown(ControlKey);
                if (_isControlDown)
                {
                    _isControlHold = true;

                    // Set pressed color hint
                    _hitLineRenderer.color = _pressedColor;

                    // Check notes in detect area exists
                    if (_noteObjects.Count > 0)
                    {
                        // Check nearest note
                        INoteKey pressedNote = GetNearestNoteDrop();
                        if (pressedNote != null)
                        {
                            // Destroy or remove note
                            MonoBehaviour mono = (MonoBehaviour)pressedNote;
                            _noteObjects.Remove(pressedNote.GetSublineIndex());

                            // Get hit type timing
                            HitType hitType = GetHitScoreType(pressedNote);

                            // Check which note was hit
                            if (pressedNote.Type == NoteType.HitNote || hitType == HitType.Miss)
                            {
                                // Call event for hit note only
                                EventHandler.CallEvent(new NoteHitEventArgs(pressedNote, hitType));

                                // Immediately disable when hit
                                mono.gameObject.SetActive(false);
                            }
                            else if (pressedNote is HoldNote)
                            {
                                // Call hit and start handle the object itself
                                HoldNote holdNote = (HoldNote)pressedNote;
                                holdNote.Hit(hitType);
                            }
                        }
                    }
                }

                // Special case for hold note control
                if (Input.GetKeyUp(ControlKey))
                {
                    _isControlHold = false;

                    // Set normal color hint, by default is white
                    _hitLineRenderer.color = _releasedColor;
                }
            }
        }

        private void FixedUpdate()
        {
            // Drop note
            if (SoundMaster.Singleton.IsPlaying)
            {
                // just drop the note container
                _noteObjectContainer.transform.position -= new Vector3(0f, _dropSpeed, 0f) * Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe event
            EventHandler.OnNoteMissedEvent -= HandleNoteMissed;
            BeatmapPlayer.OnSetBeatmapIntoPlayer -= HandleSetBeatmapToPlayer;
            BeatmapEditor.OnStopTesting -= HandleStopTesting;
            BeatmapEditor.OnBPMChange -= HandleBPMChange;
            BeatmapEditor.OnDropSpeedChange -= HandleDropSpeedChange;
            BeatmapEditor.OnWorldObjectBeingMove -= HandleMovingContainer;
            BeatmapEditor.OnTimeChange -= HandleTimePostionChange;
            BeatmapEditor.OnEditingRevert -= HandleEditorRevert;
            BeatmapEditor.OnHoldNoteCreated -= HandleHoldNoteCreated;
        }
        #endregion

        #region Event Methods
        private void HandleNoteMissed(NoteMissEventArgs args)
        {
            // Check which column does the note object was dropped
            INoteKey note = args.Note;
            if (ColumnIndex != note.ColumnIndex) return;

            // Remove note object from column objects on surface list
            _noteObjects.Remove(note.GetSublineIndex());

            // Call missed note hit event
            if (note.Type != NoteType.Blank)
            {
                // Check control hit down, then ignore it
                if (!_isControlDown)
                    EventHandler.CallEvent(new NoteHitEventArgs(args.Note, HitType.Miss));
            }
        }

        private void HandleSetBeatmapToPlayer(Beatmap beatmap)
        {
            // Reinit all blanks
            ReInitalizeBlanks();

            // Reinit all objects
            ReInitializeObjects();
        }

        private void HandleStopTesting()
        {
            // Reinit all objects
            ReInitializeObjects();
        }

        private void HandleBPMChange(int bpm)
        {
            // Reinit all blanks
            ReInitalizeBlanks();

            // Reinit all objects
            ReInitializeObjects();
        }

        private void HandleDropSpeedChange(float dropSpeed)
        {
            // Set drop speed
            this._dropSpeed = dropSpeed;

            // Reinit all blanks
            ReInitalizeBlanks();

            // Reinit all objects
            ReInitializeObjects();
        }

        private void HandleMovingContainer(Transform targetObj, Vector3 targetPos)
        {
            // Get current timeline by converting position into time
            Transform conTrans = _noteObjectContainer.transform;
            Vector2 zeroOffset = conTrans.localPosition - _perfectHitLine.localPosition;
            float songLength = BeatmapPlayer.Singleton.CurrentBeatmap.Song.length;

            // Check if the timeline is exceeded the min and max, then cancel edit
            if (zeroOffset.y >= 0f && targetPos.y - _perfectHitLine.position.y >= 0f)
            {
                // Justify position
                conTrans.position = new Vector3(conTrans.position.x, _perfectHitLine.position.y, 0f);

                // Check column container is the same as target object, then set the current song time
                if (targetObj.Equals(conTrans))
                {
                    SoundMaster.Singleton.AtSongTime = 0f;
                }
                return;
            }
            else if (zeroOffset.y <= -songLength && targetPos.y <= -songLength * _dropSpeed)
            {
                // Justify position
                conTrans.position = new Vector3(conTrans.position.x, _dropSpeed * -songLength, 0f);

                // Check column container is the same as target object, then set the current song time
                if (targetObj.Equals(conTrans))
                {
                    SoundMaster.Singleton.AtSongTime = songLength;
                }
                return;
            }

            // Check column container is the same as target object, then set the current song time
            if (targetObj.Equals(conTrans))
            {
                SoundMaster.Singleton.AtSongTime = zeroOffset.y / Vector2.down.y / _dropSpeed;
            }

            // Handle moving other column
            conTrans.position = new Vector3(conTrans.position.x, targetPos.y, 0f);
        }

        private void HandleTimePostionChange(float atTime)
        {
            // Set position by time
            Vector2 perfectLineOffset = new Vector2(_perfectHitLine.position.x, _perfectHitLine.position.y);
            _noteObjectContainer.transform.position = perfectLineOffset + Vector2.down * _dropSpeed * atTime;
        }

        private void HandleEditorRevert()
        {
            // Reinit all objects
            ReInitializeObjects();
        }

        private void HandleHoldNoteCreated(NoteColumn targetColumn, int startSubline, int endSubline)
        {
            // Check the same target column
            if (!this.Equals(targetColumn)) return;

            // Check for existing object inside the note object list
            for (int i = startSubline + 1; i <= endSubline; i++)
            {
                // Any object existing in this range will be destroyed automatically
                if (_noteObjects.ContainsKey(i))
                {
                    // Get note object key from list and remove it
                    INoteKey note = _noteObjects[i];
                    _noteObjects.Remove(i);

                    // Convert into real object then destroy it
                    MonoBehaviour mono = (MonoBehaviour)note;
                    if (mono != null) Destroy(mono.gameObject);
                }
            }
        }
        #endregion

        /// <summary>
        /// Get a position by index.
        /// Automatically snapped on that exact position.
        /// </summary>
        public Vector2 GetPositionByIndex(int sublineIndex)
        {
            // Convert subline into time
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            float atTime = beatmap.GetTimePerSubSequence() * sublineIndex;

            // Convert time to exact position
            return _noteObjectContainer.transform.position + beatmap.GetNotePosition(Vector2.down, atTime);
        }

        /// <summary>
        /// Get perfect line position.
        /// </summary>
        /// <returns>Position of perfect line</returns>
        public Vector2 GetPerfectLinePosition()
        {
            // Return offset info
            return _perfectHitLine.position;
        }

        /// <summary>
        /// Get how many objects left in the column.
        /// </summary>
        /// <returns>Objects count</returns>
        public int GetObjectsLeftover()
        {
            // Return objects leftover count
            return _noteObjects.Count;
        }

        /// <summary>
        /// Get subline index on that position.
        /// This also affects by current timeline.
        /// </summary>
        /// <param name="pos">Given world position</param>
        /// <returns>Subline index</returns>
        public int GetSublineByPosition(Vector2 pos)
        {
            // Calculate position into seconds
            Transform conTrans = _noteObjectContainer.transform;
            Vector2 zeroPos = new Vector3(pos.x, pos.y) - conTrans.position;
            float onSeconds = zeroPos.y / _dropSpeed;

            // Calculate seconds into subline index
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            return Mathf.FloorToInt(onSeconds / beatmap.GetTimePerSubSequence());
        }

        /// <summary>
        /// Does on line sequence has been occupied by a note.
        /// </summary>
        /// <param name="sublineIndex">Subline index</param>
        /// <returns></returns>
        public bool IsNoteOccupied(int sublineIndex)
        {
            // Convert subline into sequence
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            int si = sublineIndex / beatmap.SequenceDivision.GetDivisionDivider();
            int ssi = sublineIndex % beatmap.SequenceDivision.GetDivisionDivider();
            return beatmap.IsObjectSet(si, ssi, ColumnIndex) && _noteObjects.ContainsKey(sublineIndex);
        }

        /// <summary>
        /// Spawn or create note object into the column.
        /// </summary>
        internal void CreateNoteObject(NoteType type, int sublineIndex)
        {
            // Convert subline index into sequence indexs
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            int si = sublineIndex / beatmap.SequenceDivision.GetDivisionDivider();
            int ssi = sublineIndex % beatmap.SequenceDivision.GetDivisionDivider();

            // Create blank note means deleting object
            if (type == NoteType.Blank)
            {
                DeleteObject(sublineIndex);
                return;
            }

            // Spawn a new note
            INoteKey newNote = SpawnNoteObject(si, ssi, type);

            // Check note object was not created
            if (newNote == null) return;

            // Check spawned object already exists, then destroy the old one
            if (_noteObjects.ContainsKey(sublineIndex))
            {
                INoteKey oldNote = _noteObjects[sublineIndex];
                _noteObjects.Remove(sublineIndex);

                // Convert into real object, then destroy
                MonoBehaviour oldMono = (MonoBehaviour)oldNote;
                if (oldMono != null) Destroy(oldMono.gameObject);
            }

            // Convert into real objects to be assigned
            MonoBehaviour mono = (MonoBehaviour)newNote;

            // Set current position at song time
            float posY = beatmap.GetSublineIndex(si, ssi) * beatmap.GetTimePerSubSequence() * beatmap.DropSpeed;
            mono.transform.localPosition = new Vector2(0f, posY);
            mono.gameObject.SetActive(true);

            // Insert object into list of spawned objects
            _noteObjects.Add(sublineIndex, newNote);

            // Set meta data
            newNote.SetMetaData(beatmap.MetaData);
        }

        internal void DeleteObject(int sublineIndex)
        {
            // Check object exists
            if (_noteObjects.ContainsKey(sublineIndex))
            {
                // Remove object from list
                INoteKey note = _noteObjects[sublineIndex];
                _noteObjects.Remove(sublineIndex);

                // Destroy object
                MonoBehaviour mono = (MonoBehaviour)note;
                Destroy(mono.gameObject);
            }
        }

        /// <summary>
        /// Get note object from column.
        /// Used in editor purpose only.
        /// </summary>
        /// <param name="sublineIndex">Subline index</param>
        /// <returns>Note object</returns>
        internal INoteKey GetNoteObject(int sublineIndex)
        {
            try
            {
                return _noteObjects[sublineIndex];
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private void ReInitializeObjects()
        {
            // Clear previous note objects
            ClearColumnObj();

            // Create all note objects
            InitColumnObj();
        }

        private void ReInitalizeBlanks()
        {
            // Clear previous blank objects
            ClearBlankObj();

            // Create blank objects when new beatmap set
            InitBlankObj();
        }

        /// <summary>
        /// Create initial note objects in range bertween time at perfect line with spawner line.
        /// </summary>
        /// <param name="player"></param>
        private void InitColumnObj()
        {
            // Check note keys in certain sequence range
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;

            // Load by metadata
            JSONNode rootNode = JSON.Parse(beatmap.MetaData);
            foreach (string nodeKey in rootNode.Keys)
            {
                // Get node
                JSONNode node = rootNode[nodeKey];

                // Check column index exists
                if (node[$"{_atColumnIndex}"] == null) continue;

                // Spawn a new note object
                int sublineIndex = int.Parse(nodeKey);
                int si = sublineIndex / beatmap.SequenceDivision.GetDivisionDivider();
                int ssi = sublineIndex % beatmap.SequenceDivision.GetDivisionDivider();
                INoteKey newNote = SpawnNoteObject(si, ssi, beatmap.GetNoteType(si, ssi, _atColumnIndex));

                // Check note was spawned
                if (newNote == null) continue;

                // Check spawned object already exists, then destroy the old one
                if (_noteObjects.ContainsKey(sublineIndex))
                {
                    INoteKey oldNote = _noteObjects[sublineIndex];
                    _noteObjects.Remove(sublineIndex);

                    // Convert into real object, then destroy
                    MonoBehaviour oldMono = (MonoBehaviour)oldNote;
                    Debug.Log($"Destroyed Object on Sequence {si} Subs {ssi}: {oldMono}\nReplaced with {newNote}");
                    if (oldMono != null) Destroy(oldMono.gameObject);
                }

                // Add object into list
                _noteObjects.Add(sublineIndex, newNote);

                // Convert into real objects to be assigned
                MonoBehaviour mono = (MonoBehaviour)newNote;

                // Set current position at song time
                float posY = beatmap.GetSublineIndex(si, ssi) * beatmap.GetTimePerSubSequence() * beatmap.DropSpeed;
                mono.transform.localPosition = new Vector2(0f, posY);
                mono.gameObject.SetActive(true);

                // Set meta data
                newNote.SetMetaData(beatmap.MetaData);
            }

            // Set drop note speed
            _dropSpeed = beatmap.DropSpeed;
        }

        /// <summary>
        /// Clear all existing note object that spawned in column.
        /// </summary>
        private void ClearColumnObj()
        {
            // Clear spawned objects in column
            List<INoteKey> notes = new List<INoteKey>(_noteObjects.Values);
            foreach (INoteKey note in notes)
            {
                // Skip if it is null
                if (note == null) continue;

                // Get object reference
                MonoBehaviour mono = (MonoBehaviour)note;

                // Destroy object if exists
                if (mono != null) Destroy(mono.gameObject);
            }
            _noteObjects.Clear();
        }

        private void InitBlankObj()
        {
            // Set initial position of container
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            float farLength = beatmap.GetTimePerSequence() * beatmap.DropSpeed * beatmap.GetSequenceCount();
            _noteObjectContainer.size = new Vector2(_noteObjectContainer.size.x, farLength);
            _noteObjectContainer.offset = new Vector2(_noteObjectContainer.offset.x, farLength / 2f);
            _noteObjectContainer.transform.localPosition = _perfectHitLine.localPosition;

            // Loop through sequences for creating blank notes
            for (int i = 0; i < beatmap.GetSequenceCount(); i++)
            {
                // Create blank note sequence line
                BlankNote blankKey = Instantiate(_emptyLinePrefab, _noteObjectContainer.transform);
                blankKey.SequenceIndex = i;
                blankKey.SubSequenceIndex = 0;
                _blankNoteObjs.Add(i * beatmap.SequenceDivision.GetDivisionDivider(), blankKey);

                // Set object position
                float posY = i * beatmap.GetTimePerSequence() * beatmap.DropSpeed;
                blankKey.transform.localPosition = new Vector2(0f, posY);
                blankKey.gameObject.SetActive(true);
            }
        }

        private void ClearBlankObj()
        {
            // Clear blank objects in column
            foreach (BlankNote note in _blankNoteObjs.Values)
            {
                // Destroy object if exists
                if (note != null) Destroy(note.gameObject);
            }
            _blankNoteObjs.Clear();
        }

        /// <summary>
        /// Spawn note by sequence and sub-sequence index.
        /// Required beatmap to be able to read it.
        /// </summary>
        /// <param name="si">Sequence index</param>
        /// <param name="ssi">Sub-sequence index</param>
        private INoteKey SpawnNoteObject(int si, int ssi, NoteType type)
        {
            // Calculate offset seconds between current time and sequence time
            Transform conTrans = _noteObjectContainer.transform;
            INoteKey noteKey = null;

            // Create object note
            if (type == NoteType.HitNote)
            {
                // Spawn hit note
                HitNote hitKey = Instantiate(_hitNotePrefab, conTrans);
                hitKey.SequenceIndex = si;
                hitKey.SubSequenceIndex = ssi;
                hitKey.AtColumn = this;
                noteKey = hitKey;
            }
            else if (type == NoteType.HoldNote)
            {
                // Spawn hold note from
                HoldNote holdKey = Instantiate(_holdNotePrefab, conTrans);
                holdKey.SequenceIndex = si;
                holdKey.SubSequenceIndex = ssi;
                holdKey.AtColumn = this;
                noteKey = holdKey;
            }
            return noteKey;
        }

        /// <summary>
        /// Get nearest distance between note and perfect line.
        /// This function ignores the blank type of note
        /// </summary>
        /// <returns>Nearest note drop, if there is no object in column, then it is null</returns>
        private INoteKey GetNearestNoteDrop()
        {
            // Cast a ray to detect note inside control area
            Vector2 origin = new Vector2(transform.position.x, transform.position.y) + _areaDetect.offset;
            float distance = _areaDetect.size.x > _areaDetect.size.y ? _areaDetect.size.x / 2f : _areaDetect.size.y / 2f;
            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, _areaDetect.size, 0f, Vector2.up, distance);

            // Check nearest note and return the object
            int nearestNoteIndex = -1;
            float nearestDistance = Mathf.Infinity;
            for (int i = 0; i < hits.Length; i++)
            {
                // Get current cast
                RaycastHit2D hit = hits[i];

                // Check null object not exists
                if (hit.collider == null) continue;

                // Check if raycast hit note object
                Collider2D collide = hit.collider;
                MonoBehaviour temp = null;
                if (collide.GetComponent<HitNote>()) temp = collide.GetComponent<HitNote>();
                else if (collide.GetComponent<HoldNote>()) temp = collide.GetComponent<HoldNote>();

                // Check object is not a note component
                if (temp == null) continue;

                // Check nearest note, then assign it
                if (Vector3.Distance(temp.transform.position, _perfectHitLine.position) < nearestDistance)
                {
                    nearestNoteIndex = i;
                    nearestDistance = Vector3.Distance(temp.transform.position, _perfectHitLine.position);
                }
            }

            // Check null object
            if (nearestNoteIndex == -1) return null;

            #if UNITY_EDITOR
            Debug.Log($"Object {hits[nearestNoteIndex].collider.gameObject.name} with nearest distance: {nearestDistance}");
            #endif

            // Return the defined note key object
            HitNote hitNote = hits[nearestNoteIndex].collider.GetComponent<HitNote>();
            if (hitNote != null) return hitNote;
            HoldNote holdNote = hits[nearestNoteIndex].collider.GetComponent<HoldNote>();
            if (holdNote != null) return holdNote;
            return null;
        }

        /// <summary>
        /// Check which scoring hit does player get.
        /// </summary>
        /// <param name="notePos">Position of note</param>
        /// <returns>Hit type scoring</returns>
        internal HitType GetHitScoreType(Vector2 notePos)
        {
            // Calculate distance between perfect line and note
            float offsetY = Vector2.Distance(_perfectHitLine.position, notePos);

            // Check case for more or less than 5% of block distance
            if (offsetY < 0.05f) return HitType.Perfect;
            else if (offsetY < 0.10f) return HitType.Good;
            else if (offsetY < 0.25f) return HitType.Bad;
            else return HitType.Miss;
        }

        /// <summary>
        /// Check which scoring hit does player get.
        /// </summary>
        /// <param name="note">Scored note</param>
        /// <returns>Hit type scoring</returns>
        private HitType GetHitScoreType(INoteKey note)
        {
            // Calculate distance between perfect line and note
            MonoBehaviour mono = (MonoBehaviour)note;
            float offsetY = Mathf.Abs(mono.transform.position.y > _perfectHitLine.position.y ?
                mono.transform.position.y - _perfectHitLine.position.y :
                _perfectHitLine.position.y - mono.transform.position.y);

            // Check case for more or less than 5% of block distance
            if (offsetY < 0.05f) return HitType.Perfect;
            else if (offsetY < 0.10f) return HitType.Good;
            else if (offsetY < 0.25f) return HitType.Bad;
            else return HitType.Miss;
        }

    }

}

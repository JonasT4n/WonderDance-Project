using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using SimpleJSON;

namespace WonderDanceProj
{
    internal class HoldNote : MonoBehaviour, INoteKey
    {
        [Header("Requirements")]
        [SerializeField]
        private Transform           _innerRenderContainer = null;
        [SerializeField]
        private SpriteRenderer      _startPoint = null;
        [SerializeField]
        private SpriteRenderer      _innerRender = null;
        [SerializeField]
        private SpriteRenderer      _endPoint = null;
        [SerializeField]
        private SpriteMask          _maskFader = null;

        // Temporary variables
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private NoteColumn          _column = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private HitType             _hitTiming = HitType.Miss;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly] 
        private int                 _atSequence = 0;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _atSubSequence = 0;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _atEndSequence = 0;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _atEndSubSequence = 0;
        private IEnumerator         _holdRoutine = null;

        #region Properties
        public NoteColumn AtColumn
        {
            internal set
            {
                // Set parent column
                _column = value;

                // Set asset by column index
                SpriteAssetObj asset = GameManager.Singleton.Asset;
                _endPoint.sprite = _startPoint.sprite = asset.GetKeySpriteByIndex(_column.ColumnIndex);
                _innerRender.color = asset.GetFillerColorByindex(_column.ColumnIndex);
            }
            get => _column;
        }
        public NoteType Type => NoteType.HoldNote;
        public int SequenceIndex
        {
            set
            {
                // Check end sequence index is less than current sequence index
                if (_atEndSequence < _atSequence)
                {
                    _atEndSequence = value;
                    _atEndSubSequence = _atSubSequence;
                }

                // Set sequence index
                _atSequence = value;
            }
            get => _atSequence;
        }
        public int SubSequenceIndex
        {
            set
            {
                // Check end sub-sequence index is less than current sub-sequence index
                if (_atEndSubSequence < _atSubSequence) _atEndSubSequence = _atSubSequence;

                // Set sub-sequence index
                _atSubSequence = value;
            }
            get => _atSubSequence;
        }
        public int ColumnIndex => _column.ColumnIndex;
        public int EndSequenceIndex
        {
            internal set => _atEndSequence = value < _atSequence ? _atSequence : value;
            get => _atEndSequence;
        }
        public int EndSubSequenceIndex
        {
            internal set => _atEndSubSequence = _atSequence != _atEndSequence ? value : value < _atSubSequence ? _atSubSequence : value;
            get => _atEndSubSequence;
        }
        #endregion

        #region Unity BuiltIn Methods
        private void OnEnable()
        {
            // Reset values
            _holdRoutine = null;
            _hitTiming = HitType.Miss;
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            // Check note key exited control area
            NoteColumn column = collision.GetComponent<NoteColumn>();
            if (column != null && !UIGameManager.IsEditorModeActive)
            {
                // Check column match
                if (column.Equals(_column) && _holdRoutine == null)
                {
                    // Destroy or remove note object
                    gameObject.SetActive(false);

                    // Call event
                    EventHandler.CallEvent(new NoteMissEventArgs(this));
                }
            }
        }
        #endregion

        public void SetEndSublineIndex(int sublineIndex)
        {
            // Convert current end sequence into end subline
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            int endSubline = beatmap.GetSublineIndex(_atEndSequence, _atEndSubSequence);

            // Compare end subline and current set subline, then ignore
            if (endSubline == sublineIndex) return;

            // Set new end sequence and then reconstruct
            _atEndSequence = sublineIndex / beatmap.SequenceDivision.GetDivisionDivider();
            _atEndSubSequence = sublineIndex % beatmap.SequenceDivision.GetDivisionDivider();
            ReconstructHoldLength();
        }

        public int GetSublineIndex()
        {
            // Get current beatmap play
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;

            // Return whole line index
            return beatmap.SequenceDivision.GetDivisionDivider() * _atSequence + _atSubSequence;
        }

        public void SetMetaData(string metadata)
        {
            // Parse into json data
            JSONNode node = JSONNode.Parse(metadata);

            // Get existing meta data for end sequence
            int sublineIndex = GetSublineIndex();
            if (node[$"{sublineIndex}"][$"{ColumnIndex}"] != null)
            {
                // Get subline index and beatmap data
                Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
                int endSublineIndex = node[$"{sublineIndex}"][$"{ColumnIndex}"]["endnote"].AsInt;

                // Set end sequence and end sub-sequence
                int div = beatmap.SequenceDivision.GetDivisionDivider();
                _atEndSequence = endSublineIndex / div;
                _atEndSubSequence = endSublineIndex % div;
            }

            // Reconstruct hold length
            ReconstructHoldLength();
        }

        /// <summary>
        /// Call this by
        /// </summary>
        internal void Hit(HitType timing)
        {
            // Set hit timing
            _hitTiming = timing;

            // Call routine to handle release detect on this hold note
            _holdRoutine = HandlePressHold();
            StartCoroutine(_holdRoutine);
        }

        /// <summary>
        /// Set height or width of the note object.
        /// </summary>
        private void ReconstructHoldLength()
        {
            // Destroy all objects inside container
            while (_innerRenderContainer.childCount > 0)
            {
                Destroy(_innerRenderContainer.GetChild(0).gameObject);
            }

            // Convert subline range into time range
            Beatmap beatmap = BeatmapPlayer.Singleton.CurrentBeatmap;
            int endSublineIndex = beatmap.GetSublineIndex(_atEndSequence, _atEndSubSequence);
            float timeRange = (endSublineIndex - GetSublineIndex()) * beatmap.GetTimePerSubSequence();

            // Convert time into length, then create copy of middle render to extend the hold note object
            float length = timeRange * beatmap.DropSpeed;
            _innerRender.transform.localScale = new Vector3(1f, length, 1f);

            // Set end position
            _endPoint.transform.localPosition = Vector2.up * length;
        }

        private IEnumerator HandlePressHold()
        {
            // Check if player keep press-holding the key
            Vector2 perfectLinePos = _column.GetPerfectLinePosition();
            while (Input.GetKey(_column.ControlKey))
            {
                // Check start point passes the perfect line, then disable the start point render
                if (_startPoint.transform.position.y < perfectLinePos.y && _startPoint.gameObject.activeSelf)
                    _startPoint.gameObject.SetActive(false);

                // Go to next frame
                yield return null;

                // Start scaling mask to hide the inner render smoothly
                float lengthScale = transform.position.y >= perfectLinePos.y ? 0f : perfectLinePos.y - transform.position.y;
                _maskFader.transform.localScale = new Vector3(_maskFader.transform.localScale.x, lengthScale, 1f);

                // Check if player hold until it reaches the end point
                if (_endPoint.transform.position.y < perfectLinePos.y && _endPoint.gameObject.activeSelf)
                    break;
            }

            // Check end hit type
            HitType endHit = _column.GetHitScoreType(_endPoint.transform.position);

            // Calculate result
            HitType result = (HitType)(((int)endHit + (int)_hitTiming) / 2);

            // Call event then disable object
            EventHandler.CallEvent(new HoldNoteFinishedEventArgs(this, result, AtColumn.ColumnIndex));
            gameObject.SetActive(false);
        }

    }

}

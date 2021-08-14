using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using SimpleJSON;

namespace WonderDanceProj
{
    internal class HitNote : MonoBehaviour, INoteKey
    {
        [Header("Requirements")]
        [SerializeField]
        private SpriteRenderer      _spriteRender = null;

        // Temporary variables
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private NoteColumn          _column = null;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly] 
        private int                 _atSequence = 0;
        [BoxGroup("DEBUG"), SerializeField, ReadOnly]
        private int                 _atSubSequence = 0;

        #region Properties
        public NoteColumn AtColumn
        {
            internal set
            {
                // Set parent column
                _column = value;

                // Set asset by column index
                _spriteRender.sprite = GameManager.Singleton.Asset.GetKeySpriteByIndex(_column.ColumnIndex);
            }
            get => _column;
        }
        public NoteType Type => NoteType.HitNote;
        public int SequenceIndex
        {
            set => _atSequence = value;
            get => _atSequence;
        }
        public int SubSequenceIndex
        {
            set => _atSubSequence = value;
            get => _atSubSequence;
        }
        public int ColumnIndex => _column.ColumnIndex;
        public int EndSequenceIndex => _atSequence;
        public int EndSubSequenceIndex => _atSubSequence;
        #endregion

        #region Unity BuiltIn Methods
        private void OnTriggerExit2D(Collider2D collision)
        {
            // Check note key exited control area
            NoteColumn column = collision.GetComponent<NoteColumn>();
            if (column != null && !UIGameManager.IsEditorModeActive)
            {
                // Check column match
                if (column.Equals(_column))
                {
                    // Destroy or remove note object
                    gameObject.SetActive(false);

                    // Call event
                    EventHandler.CallEvent(new NoteMissEventArgs(this));
                }
            }
        }
        #endregion

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

            // TODO: Any feature can be set into note using metadata
        }    
    }

}

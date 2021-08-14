using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WonderDanceProj
{
    public class GameObserver : MonoBehaviour
    {
        [Header("Requirements")]
        [SerializeField]
        private RectTransform       _scoreHintContainer = null;
        [SerializeField]
        private FloatFadeText       _scoreHintPrefab = null;

        [Header("Live UI Gameplay Info")]
        [SerializeField]
        private TextMeshProUGUI     _scoreTxt = null;

        // Temporary variables
        private int                 _perfectCount = 0;
        private int                 _goodCount = 0;
        private int                 _badCount = 0;
        private int                 _missCount = 0;
        private float               _scoreGain = 0;
        private float               _score = 0;

        #region Unity BuiltIn Methods
        private void Awake()
        {
            // Subscribe events
            BeatmapPlayer.OnSetBeatmapIntoPlayer += HandleBeatmapInit;
            EventHandler.OnHitNoteEvent += HandleHitNote;
            EventHandler.OnHoldNoteFinishEvent += HandleHoldNote;
            EventHandler.OnBeatmapSaveChangesEvent += HandleSaveEditedBeatmap;
            EventHandler.OnEditorModeActiveEvent += HandleEditorActive;
        }

        private void OnDestroy()
        {
            // Unsubscribe events
            BeatmapPlayer.OnSetBeatmapIntoPlayer -= HandleBeatmapInit;
            EventHandler.OnHitNoteEvent -= HandleHitNote;
            EventHandler.OnHoldNoteFinishEvent -= HandleHoldNote;
            EventHandler.OnBeatmapSaveChangesEvent -= HandleSaveEditedBeatmap;
            EventHandler.OnEditorModeActiveEvent -= HandleEditorActive;
        }
        #endregion

        #region Event Methods
        private void HandleBeatmapInit(Beatmap beatmap)
        {
            // Reset scores
            _perfectCount = 0;
            _goodCount = 0;
            _badCount = 0;
            _missCount = 0;
            _score = 0;

            // Define score gain
            _scoreGain = 1000000f / beatmap.GetRawObjectDataCount();
        }

        private void HandleHitNote(NoteHitEventArgs args)
        {
            // Update score and UI
            AddHitNoteScore(args.HitScoreType);
        }

        private void HandleHoldNote(HoldNoteFinishedEventArgs args)
        {
            // Update score and UI
            AddHitNoteScore(args.HitScoreType);
        }

        private void HandleSaveEditedBeatmap(SaveEditBeatmapEventArgs args)
        {
            // Create score hint on UI
            FloatFadeText txt = Instantiate(_scoreHintPrefab, _scoreHintContainer);
            txt.rectTransform.position = _scoreHintContainer.position;
            txt.Text = $"Saved Changes";
        }

        private void HandleEditorActive(EditorModeActiveEventArgs args)
        {
            // Assign activation into ui in gameplay
            _scoreTxt.gameObject.SetActive(!args.IsSetActive);
        }
        #endregion

        private void AddHitNoteScore(HitType type)
        {
            // Create score hint on UI
            FloatFadeText txt = Instantiate(_scoreHintPrefab, _scoreHintContainer);
            txt.rectTransform.position = _scoreHintContainer.position;
            txt.Text = $"{type}";

            // Check which score by gain and hit type will get
            if (type == HitType.Perfect)
            {
                // Add score 100% and perfect count
                _score += _scoreGain;
                _perfectCount++;
            }
            else if (type == HitType.Good)
            {
                // Add score 80% and perfect count
                _score += _scoreGain * 4f / 5f;
                _goodCount++;
            }
            else if (type == HitType.Bad)
            {
                // Add score 50% and perfect count
                _score += _scoreGain / 2f;
                _badCount++;
            }
            else
            {
                // Adding miss count only
                _missCount++;
            }

            // Update live UI
            _scoreTxt.text = $"{Mathf.CeilToInt(_score)}";
        }
    }

}

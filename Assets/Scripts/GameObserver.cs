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
        private TextMeshProUGUI     _liveComboTxt = null;
        [SerializeField]
        private FloatFadeText       _scoreHintPrefab = null;

        [Header("Scoreboard UI")]
        [SerializeField]
        private TextMeshProUGUI     _gradeTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _lastScoreTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _maxComboCountTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _perfectCountTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _goodCountTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _badCountTxt = null;
        [SerializeField]
        private TextMeshProUGUI     _missCountTxt = null;

        [Header("Live UI Gameplay Info")]
        [SerializeField]
        private TextMeshProUGUI     _scoreTxt = null;

        // Temporary variables
        private int                 _maxComboCount = 0;
        private int                 _comboCount = 0;
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
            BeatmapPlayer.OnEndPlay += HandleEndGame;
            UIGameManager.OnRestartGame += HandleGameRestart;
            EventHandler.OnHitNoteEvent += HandleHitNote;
            EventHandler.OnHoldNoteFinishEvent += HandleHoldNote;
            EventHandler.OnBeatmapSaveChangesEvent += HandleSaveEditedBeatmap;
            EventHandler.OnEditorModeActiveEvent += HandleEditorActive;
        }

        private void Start()
        {
            // Enable or disable gameplay UI in editor mode
            _scoreTxt.gameObject.SetActive(!UIGameManager.IsEditorModeActive);
            _liveComboTxt.gameObject.SetActive(!UIGameManager.IsEditorModeActive);

            // Reset scoreboard
            ResetScoreboard();
        }

        private void OnDestroy()
        {
            // Unsubscribe events
            BeatmapPlayer.OnSetBeatmapIntoPlayer -= HandleBeatmapInit;
            BeatmapPlayer.OnEndPlay -= HandleEndGame;
            UIGameManager.OnRestartGame -= HandleGameRestart;
            EventHandler.OnHitNoteEvent -= HandleHitNote;
            EventHandler.OnHoldNoteFinishEvent -= HandleHoldNote;
            EventHandler.OnBeatmapSaveChangesEvent -= HandleSaveEditedBeatmap;
            EventHandler.OnEditorModeActiveEvent -= HandleEditorActive;
        }
        #endregion

        #region Event Methods
        private void HandleBeatmapInit(Beatmap beatmap)
        {
            // Define score gain
            _scoreGain = 1000000f / beatmap.GetObjectDataCount();

            // Reset scoreboard
            ResetScoreboard();
        }

        private void HandleEndGame()
        {
            // Write scoreboard
            _gradeTxt.text = $"{GetGrade(_score)}";
            _maxComboCountTxt.text = $"x{_maxComboCount}";
            _lastScoreTxt.text = $"{_score}";
            _perfectCountTxt.text = $"x{_perfectCount}";
            _goodCountTxt.text = $"x{_goodCount}";
            _badCountTxt.text = $"x{_badCount}";
            _missCountTxt.text = $"x{_missCount}";
        }

        private void HandleGameRestart()
        {
            // Reset scoreboard
            ResetScoreboard();
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

        internal static Grade GetGrade(float score)
        {
            // Get grade by score
            if (score >= 1000000f) return Grade.FR;
            else if (score >= 950000f) return Grade.EXPlus;
            else if (score >= 950000f) return Grade.EXPlus;
            else if (score >= 900000f) return Grade.A;
            else if (score >= 800000f) return Grade.B;
            else return Grade.C;
        }

        private void ResetScoreboard()
        {
            // Reset scores
            _maxComboCount = 0;
            _comboCount = 0;
            _perfectCount = 0;
            _goodCount = 0;
            _badCount = 0;
            _missCount = 0;
            _score = 0;

            // Reset live UI
            _scoreTxt.text = $"{_score}";
            _liveComboTxt.text = $"Combo x{_comboCount}";
        }

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
                _comboCount++;
            }
            else if (type == HitType.Good)
            {
                // Add score 80% and perfect count
                _score += _scoreGain * 4f / 5f;
                _goodCount++;
                _comboCount++;
            }
            else if (type == HitType.Bad)
            {
                // Add score 50% and perfect count
                _score += _scoreGain / 2f;
                _badCount++;
                _comboCount++;
            }
            else
            {
                // Adding miss count only
                _missCount++;

                // Reset combo
                _comboCount = 0;
            }

            // Check max combo recorded
            if (_comboCount > _maxComboCount) _maxComboCount = _comboCount;

            // Update live UI
            _scoreTxt.text = $"{Mathf.CeilToInt(_score)}";
            _liveComboTxt.text = $"Combo x{_comboCount}";
        }
    }

}

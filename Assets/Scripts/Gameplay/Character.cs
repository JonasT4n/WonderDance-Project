using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderDanceProj
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class Character : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private Animator            _animator = null;

        [Header("Attribute")]
        [SerializeField]
        [Tooltip("Everytime character move, they will pose/freeze for a few seconds.")]
        private float               _showOffDuration = 1f;

        // Temporary variables
        private IEnumerator         _actionRoutine;

        #region Unity BuiltIn Methods
        private void Awake()
        {
            // Subscribe events
            EventHandler.OnHitNoteEvent += HandleHitNote;
            EventHandler.OnHoldNoteFinishEvent += HandleHoldNote;
        }

        private void OnDestroy()
        {
            // Unsubscribe events
            EventHandler.OnHitNoteEvent -= HandleHitNote;
            EventHandler.OnHoldNoteFinishEvent -= HandleHoldNote;
        }
        #endregion

        #region Event Methods
        private void HandleHoldNote(HoldNoteFinishedEventArgs args)
        {
            // Check current ongoing action
            if (_actionRoutine != null)
            {
                StopCoroutine(_actionRoutine);
            }

            // Call routine
            _actionRoutine = CallActionRoutine(args.MovementKey, (int)args.HitScoreType);
            StartCoroutine(_actionRoutine);
        }

        private void HandleHitNote(NoteHitEventArgs args)
        {
            // Check current ongoing action
            if (_actionRoutine != null)
            {
                StopCoroutine(_actionRoutine);
            }

            // Call routine
            _actionRoutine = CallActionRoutine(args.MovementKey, (int)args.HitScoreType);
            StartCoroutine(_actionRoutine);
        }
        #endregion

        private IEnumerator CallActionRoutine(int movement, int hitType)
        {
            // Activate action
            _animator.SetBool("Action", true);
            _animator.SetInteger("MoveKey", movement);
            _animator.SetInteger("HitType", hitType);

            // Pose or Freeze delay
            float tempSec = _showOffDuration;
            while (tempSec > 0f)
            {
                tempSec -= Time.deltaTime;
                yield return null;
            }

            // Deactivate action
            _animator.SetBool("Action", false);

            // Release routine cache
            _actionRoutine = null;
        }
    }

}

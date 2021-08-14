using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderDanceProj
{
    public static class EventHandler
    {
        // Delegate functions
        public delegate void PauseGame(PauseGameEventArgs args);
        public delegate void NoteHit(NoteHitEventArgs args);
        public delegate void HoldNoteFinished(HoldNoteFinishedEventArgs args);
        public delegate void NoteMiss(NoteMissEventArgs args);
        public delegate void EditorModeActive(EditorModeActiveEventArgs args);
        public delegate void SaveEditBeatmap(SaveEditBeatmapEventArgs args);

        /// <summary>
        /// Event called when player pause the current game.
        /// </summary>
        public static event PauseGame           OnPauseGameEvent;

        /// <summary>
        /// Event called when player hit the note.
        /// </summary>
        public static event NoteHit             OnHitNoteEvent;

        /// <summary>
        /// Event called after player press and hold the hold note object and then it reaches the end line.
        /// Or, player maybe released in the middle of the hold note before reaching the finish line.
        /// </summary>
        public static event HoldNoteFinished    OnHoldNoteFinishEvent;

        /// <summary>
        /// Event called when note drops too far outside column after passing through perfect line.
        /// </summary>
        public static event NoteMiss            OnNoteMissedEvent;

        /// <summary>
        /// Event called when entering or leaving editor mode.
        /// </summary>
        public static event EditorModeActive    OnEditorModeActiveEvent;

        /// <summary>
        /// Event called when saving changes in current editing beatmap.
        /// </summary>
        public static event SaveEditBeatmap     OnBeatmapSaveChangesEvent;

        public static void CallEvent(IEventArgs arg)
        {
            // Check which event called
            if (arg is PauseGameEventArgs) OnPauseGameEvent?.Invoke((PauseGameEventArgs)arg);
            else if (arg is NoteHitEventArgs) OnHitNoteEvent?.Invoke((NoteHitEventArgs)arg);
            else if (arg is HoldNoteFinishedEventArgs) OnHoldNoteFinishEvent?.Invoke((HoldNoteFinishedEventArgs)arg);
            else if (arg is NoteMissEventArgs) OnNoteMissedEvent?.Invoke((NoteMissEventArgs)arg);
            else if (arg is EditorModeActiveEventArgs) OnEditorModeActiveEvent?.Invoke((EditorModeActiveEventArgs)arg);
            else if (arg is SaveEditBeatmapEventArgs) OnBeatmapSaveChangesEvent?.Invoke((SaveEditBeatmapEventArgs)arg);
        }
    }

    public interface IEventArgs { }

    public interface ICancelableEvent
    {
        /// <summary>
        /// Check if an event was cancelled.
        /// </summary>
        bool IsCancelled { get; }

        /// <summary>
        /// Cancel an event.
        /// </summary>
        void SetCancel();
    }

    public class PauseGameEventArgs : IEventArgs
    {
        private bool isPause;

        #region Properties
        public bool IsPaused => isPause;
        #endregion

        public PauseGameEventArgs(bool isPause)
        {
            this.isPause = isPause;
        }
    }

    public class NoteHitEventArgs : IEventArgs
    {
        private INoteKey obj;
        private HitType type;

        #region Properties
        public INoteKey Note => obj;
        public HitType HitScoreType => type;
        #endregion

        public NoteHitEventArgs(INoteKey obj, HitType type)
        {
            this.obj = obj;
            this.type = type;
        }
    }

    public class HoldNoteFinishedEventArgs : IEventArgs
    {
        private INoteKey obj;
        private HitType type;

        #region Properties
        public INoteKey Note => obj;
        public HitType HitScoreType => type;
        #endregion

        public HoldNoteFinishedEventArgs(INoteKey obj, HitType type)
        {
            this.obj = obj;
            this.type = type;
        }
    }

    public class NoteMissEventArgs : IEventArgs
    {
        private INoteKey obj;

        #region Properties
        public INoteKey Note => obj;
        #endregion

        public NoteMissEventArgs(INoteKey obj)
        {
            this.obj = obj;
        }
    }

    public class EditorModeActiveEventArgs : IEventArgs
    {
        private bool activeChange;

        #region Properties
        public bool IsSetActive => activeChange;
        #endregion

        public EditorModeActiveEventArgs(bool activeChange)
        {
            this.activeChange = activeChange;
        }
    }

    public class SaveEditBeatmapEventArgs : IEventArgs, ICancelableEvent
    {
        private Beatmap oldMap;
        private Beatmap newMap;
        private bool isCancelled = false;

        #region Properties
        public bool IsCancelled => isCancelled;
        public Beatmap OldMap => oldMap;
        public Beatmap EditedMap => newMap;
        #endregion

        public SaveEditBeatmapEventArgs(Beatmap oldMap, Beatmap newMap)
        {
            this.newMap = newMap;
            this.oldMap = oldMap;
        }

        public void SetCancel() => isCancelled = true;
    }
}

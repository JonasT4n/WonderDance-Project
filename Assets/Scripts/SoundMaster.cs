using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WonderDanceProj
{
    public class SoundMaster : MonoBehaviour
    {
        // Singleton behaviour
        private static SoundMaster      _S;

        [Header("Component Attributes")]
        [SerializeField] 
        private AudioSource             audioBGM = null;
        [SerializeField] 
        private AudioSource[]           audioSFX = { null };

        [Header("Component Adjuster")]
        [SerializeField, Range(0f, 1f)] 
        internal float                  _volumeMaster = 1f;
        [SerializeField, Range(0f, 1f), OnValueChanged(nameof(BGMVolumeChanged))]
        internal float                  _volumeBGM = 1f;
        [SerializeField, Range(0f, 1f), OnValueChanged(nameof(SFXVolumeChanged))]
        internal float                  _volumeSFX = 1f;

        // Temporary variables
        [BoxGroup("DEBUG"), SerializeField]
        private float                   _atCurrentTime = 0f;
        [BoxGroup("DEBUG"), SerializeField]
        private bool                    _debug = false;
        private IEnumerator             _fadingRoutine;

        #region Properties
        public static SoundMaster Singleton => _S;
        public float AtSongTime
        {
            set => _atCurrentTime = value;
            get => _atCurrentTime;
        }
        public bool IsPlaying => audioBGM.isPlaying;
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
            UIGameManager.OnPauseGameActive += HandlePauseGame;
            UIGameManager.OnRestartGame += HandleRestartGame;
            BeatmapPlayer.OnBeginPlay += HandleBeginPlay;
            BeatmapPlayer.OnEndPlay += HandleEndPlay;
        }

        private void Start()
        {
            // Init components and set values
            audioBGM.playOnAwake = false;
        }

        private void Update()
        {
            // Update current time value
            if (IsPlaying) _atCurrentTime = audioBGM.time;
        }

        private void OnDestroy()
        {
            // Safe release static data
            if (this.Equals(_S))
            {
                _S = null;

                // Unsubscribe events
                UIGameManager.OnPauseGameActive -= HandlePauseGame;
                UIGameManager.OnRestartGame -= HandleRestartGame;
                BeatmapPlayer.OnBeginPlay -= HandleBeginPlay;
                BeatmapPlayer.OnEndPlay -= HandleEndPlay;
            }
        }
        #endregion

        #region Event Methods
        private void HandlePauseGame(bool active)
        {
            // Set music on or off
            if (GameManager.State == GameState.Gameplay && !UIGameManager.IsEditorModeActive)
            {
                if (active) Stop();
                else Play();
            }
        }

        private void HandleRestartGame()
        {
            // Set volume back to where it was
            audioBGM.volume = _volumeBGM;
        }

        private void HandleBeginPlay()
        {
            #if UNITY_EDITOR // Debug
            if (_debug)
            {
                Debug.Log($"Begin Play: {audioBGM.clip}");
            }
            #endif
            
            // Start play music
            Play();
        }

        private void HandleEndPlay()
        {
            // Check current fading routine exists, then stop it
            if (_fadingRoutine != null)
            {
                StopCoroutine(_fadingRoutine);
            }

            // Start fading out effect
            _fadingRoutine = FadeOutStopEffect();
            StartCoroutine(_fadingRoutine);
        }
        #endregion

        public void SetSongClip(AudioClip clip)
        {
            // Clip set
            audioBGM.clip = clip;

            #if UNITY_EDITOR // Debug
            if (_debug)
            {
                Debug.Log($"Song has been set: {audioBGM.clip}");
            }
            #endif
        }

        public void Play()
        {
            // Check if the audio is currently playing, then ignore the function
            if (IsPlaying) return;

            // Play current beatmap
            audioBGM.Play();
            audioBGM.time = _atCurrentTime;
        }

        public void Stop(bool saveTime = true)
        {
            // Check save current time play and then stop the music
            if (saveTime) _atCurrentTime = audioBGM.time;
            else _atCurrentTime = 0f;

            // Stop the music
            audioBGM.Stop();
        }

        public void Restart()
        {
            // Stop current 
            Stop(false);
            
            // Immediately play song
            Play();
        }

        public void SetMasterVolume(float volume)
        {

        }

        public void SetBGMVolume(float volume)
        {
            // Set volume on attributes and audio source
            _volumeBGM = volume;
            audioBGM.volume = _volumeBGM;
        }

        public void SetSFXVolume(float volume)
        {
            // Set volume on attributes
            _volumeSFX = volume;
            foreach (AudioSource sfxAudio in audioSFX)
            {
                // Check null object, then skip
                if (sfxAudio == null) continue;

                // Set volume on audio sources
                sfxAudio.volume = _volumeSFX;
            }
        }

        internal void Play(AudioClip clip, float atStartPoint)
        {
            // Check current routine is running
            if (_fadingRoutine != null)
            {
                StopCoroutine(_fadingRoutine);
            }

            // Play fading song routine
            _fadingRoutine = FadeInFadeOutEffect(clip, atStartPoint);
            StartCoroutine(_fadingRoutine);
        }

        private void BGMVolumeChanged() => SetBGMVolume(_volumeBGM);
        private void SFXVolumeChanged() => SetSFXVolume(_volumeSFX);

        private IEnumerator FadeOutStopEffect()
        {
            // Animate fade out sound
            float tempSec = 0.5f;
            while (tempSec > 0f)
            {
                tempSec -= Time.deltaTime;
                yield return null;

                // Fading out effect
                float percentFadeOut = tempSec / 0.5f;
                audioBGM.volume = Mathf.Lerp(0f, _volumeBGM, percentFadeOut);
            }

            // Stop playing the music
            Stop();
            audioBGM.volume = _volumeBGM;

            // Release routine
            _fadingRoutine = null;
        }

        private IEnumerator FadeInFadeOutEffect(AudioClip targetClip, float targetStartPoint)
        {
            // Animate fade out sound
            float tempSec = audioBGM.volume * 0.5f;
            while (tempSec > 0f)
            {
                tempSec -= Time.deltaTime;
                yield return null;

                // Fading out effect
                float percentFadeOut = tempSec / 0.5f;
                audioBGM.volume = Mathf.Lerp(0f, _volumeBGM, percentFadeOut);
            }

            // Set new song
            Stop();
            audioBGM.clip = targetClip;
            _atCurrentTime = targetStartPoint;
            Play();

            // Animate fade in sound
            tempSec = 0.5f;
            while (tempSec > 0f)
            {
                tempSec -= Time.deltaTime;
                yield return new WaitForEndOfFrame();

                // Fading in effect
                float percentFadeIn = 1 - tempSec / 0.5f;
                audioBGM.volume = Mathf.Lerp(0f, _volumeBGM, percentFadeIn);
            }

            // Release routine
            _fadingRoutine = null;
        }
    }

}

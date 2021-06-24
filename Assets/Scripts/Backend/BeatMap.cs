using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace WonderDanceProj
{
    public enum NoteType
    {
        Blank = 0,
        HitNote = 1,
        HoldNote = 2
    }

    [System.Serializable]
    public class BeatMap
    {
        [System.Serializable]
        public class BeatMapInnerData
        {
            // Default BPM can be set
            public int bpm;
            public string dataPath;
        }

        [SerializeField] private AudioClip musicClip = null;
        private BeatMapInnerData privateData;

        #region Properties
        public int BPM
        {
            set => privateData.bpm = value;
            get => privateData.bpm;
        }

        public string FilePath => privateData.dataPath;
        #endregion

        // Disable non-parametric constructor
        private BeatMap() { }

        /// <summary>
        /// Load beatmap using audio, automatically create map data.
        /// You can save this map data using Data File Loader.
        /// </summary>
        /// <param name="music">Audio Music</param>
        public BeatMap(AudioClip musicClip, string dataPath)
        {
            this.musicClip = musicClip;
            privateData = new BeatMapInnerData()
            {
                bpm = 130,
                dataPath = dataPath,
            };
        }

        /// <summary>
        /// Load beatmap using audio and beatmap file (.wdmap)
        /// </summary>
        /// <param name="music">Audio Music</param>
        /// <param name="innerData">Existing saved map</param>
        public BeatMap(AudioClip musicClip, BeatMapInnerData innerData)
        {
            this.musicClip = musicClip;
            privateData = innerData;
        }
    }
}

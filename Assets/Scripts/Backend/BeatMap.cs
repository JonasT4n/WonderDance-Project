using System.Collections;
using System.Collections.Generic;
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
        public BeatMap(AudioClip music)
        {
            
        }
    }

}

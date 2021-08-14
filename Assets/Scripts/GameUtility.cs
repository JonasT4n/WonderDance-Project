using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderDanceProj
{
    /// <summary>
    /// General utility for game.
    /// </summary>
    public static class GameUtility
    {
        public static Division GetDivision(int divideBy)
        {
            // Check valid division type
            if (divideBy < 8) return Division.One;
            else if (divideBy < 16) return Division.Half;
            else if (divideBy < 32) return Division.Quarter;
            else if (divideBy < 64) return Division.Eighth;
            else return Division.Sixteenth;
        }

        public static int GetDivisionDivider(this Division d)
        {
            switch (d)
            {
                case Division.One:
                    return 4;

                case Division.Half:
                    return 8;

                case Division.Eighth:
                    return 32;

                case Division.Sixteenth:
                    return 64;

                default: // Quarter
                    return 16;
            }
        }
    }

    /// <summary>
    /// To define note type in Rhythm Game.
    /// </summary>
    public enum NoteType : int
    {
        Blank = 0,
        HitNote = 1,
        HoldNote = 2,
    }

    /// <summary>
    /// Line division in one sequence.
    /// </summary>
    public enum Division : int { One = 0, Half = 1, Quarter = 2, Eighth = 3, Sixteenth = 4 }

    /// <summary>
    /// To define which score does player get.
    /// </summary>
    public enum HitType : int
    {
        Miss = 0,
        Bad = 1,
        Good = 2,
        Perfect = 3,
    }

    internal enum GameState { MainMenu, LevelMenu, Gameplay }
}

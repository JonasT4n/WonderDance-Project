using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderDanceProj
{
    public interface INoteKey
    {
        /// <summary>
        /// Get type of note.
        /// </summary>
        NoteType Type { get; }

        /// <summary>
        /// At sequence index of note key.
        /// </summary>
        int SequenceIndex { get; }

        /// <summary>
        /// At sub-sequence index of note key.
        /// </summary>
        int SubSequenceIndex { get; }

        /// <summary>
        /// On which column does the note was placed.
        /// </summary>
        int ColumnIndex { get; }

        /// <summary>
        /// Hold note type only, if there's an end sequence index.
        /// Else then it is the same value as Sequence Index.
        /// </summary>
        int EndSequenceIndex { get; }

        /// <summary>
        /// Hold note type only, if there's an end sub sequence index.
        /// Else then it is the same value as sub sequence Index.
        /// </summary>
        int EndSubSequenceIndex { get; }

        /// <summary>
        /// Instead of getting 2 data in one, convert sequence index into sub-sequences.
        /// </summary>
        /// <returns>Whole sub-sequence count</returns>
        int GetSublineIndex();

        /// <summary>
        /// Set metadata into note key object.
        /// </summary>
        /// <param name="metadata">Structured string data that can be parse</param>
        void SetMetaData(string metadata);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using NaughtyAttributes;
using SimpleJSON;

namespace WonderDanceProj
{
    [Serializable]
    public class Beatmap : ICloneable
    {
        [Serializable]
        public class BeatmapPrivateData : ICloneable
        {
            [SerializeField]
            public int          bpm = 130; // Default BPM can be set
            [SerializeField]
            public string       mapName = string.Empty; // Beatmap name
            [SerializeField]
            public float        startPointSeconds = 0f; // Where Beatmap start play
            [SerializeField]
            public float        dropSpeed = 5f; // How fast notes are dropping
            [SerializeField]
            internal int        sequenceDiv = Division.Quarter.GetDivisionDivider(); // Divide each beat in one sequence
            [SerializeField]
            internal int        tempSeqDiv = Division.Quarter.GetDivisionDivider(); // Temporary divide each beat in one sequence
            [SerializeField]
            public int          lines = 4; // How many lines|columns in rhythm board
            public int[][][]    objectsData = null; // Objects that placed in columns [AtSequence][AtColumn][AtLine]
            [SerializeField]
            public string       objectsMetaData; // Metadata to translate objects data into more detail configuration

            #region Inner Properties
            #if UNITY_EDITOR
            internal Division InitialDivision { set => sequenceDiv = value.GetDivisionDivider(); }
            #endif
            public Division FixedDivision
            {
                set
                {
                    // Double check set lines is less than the current line, then ignore
                    if (sequenceDiv < value.GetDivisionDivider()) 
                        sequenceDiv = value.GetDivisionDivider();
                }
                get => (Division)Mathf.Log(sequenceDiv, 2) - 2;
            }
            #endregion

            public object Clone()
            {
                // Clone object
                return new BeatmapPrivateData
                {
                    mapName = mapName,
                    bpm = bpm,
                    dropSpeed = dropSpeed,
                    lines = lines,
                    sequenceDiv = sequenceDiv,
                    startPointSeconds = startPointSeconds,
                    tempSeqDiv = tempSeqDiv,
                    objectsData = objectsData,
                    objectsMetaData = objectsMetaData,
                };
            }
        }

        [Header("Components")]
        [SerializeField] 
        private AudioClip               _audioClip = null;
        [SerializeField, ReadOnly] 
        private BeatmapPrivateData      _privateData = new BeatmapPrivateData();

        // Temporary variables
        internal JSONNode               _jsonRootNode;

        #region Properties
        public AudioClip Song => _audioClip;
        public int BPM
        {
            set
            {
                // Set new bpm
                _privateData.bpm = value;

                // Extend array object if it went exceeded
                ExtendByBPM();
            }
            get => _privateData.bpm;
        }

        public string MapName
        {
            set => _privateData.mapName = value;
            get => _privateData.mapName;
        }

        public float DropSpeed
        {
            set => _privateData.dropSpeed = value;
            get => _privateData.dropSpeed;
        }

        public float StartPointSeconds
        {
            set => _privateData.startPointSeconds = value;
            get => _privateData.startPointSeconds;
        }

        public int LinesCount => _privateData.lines;
        public int SequenceCount => _privateData.objectsData.Length;
        internal object ObjectsData => _privateData.objectsData;
        internal string MetaData => _privateData.objectsMetaData;
        public Division SequenceDivision => _privateData.FixedDivision;
        internal Division TemporaryDivision
        {
            set => _privateData.tempSeqDiv = value.GetDivisionDivider();
            get => (Division)Mathf.Log(_privateData.tempSeqDiv, 2) - 2;
        }
        public string MusicTitle => _audioClip.name;
        #endregion

        // Disable non-parametric constructor
        private Beatmap() { }

        /// <summary>
        /// Load beatmap using audio, automatically create map data.
        /// You can save this map data using Data File Loader.
        /// </summary>
        /// <param name="audioClip">Audio Music</param>
        /// <param name="mapName">Name of current song</param>
        public Beatmap(AudioClip audioClip, string mapName)
        {
            // Init new beatmap
            this._audioClip = audioClip;
            _privateData = new BeatmapPrivateData()
            {
                bpm = 130,
                mapName = mapName,
                startPointSeconds = 0f,
                lines = InputMap.MAX_KEY,
            };

            // Make sure the beatmap object data is empty with its metadata
            ClearBeatmapObjects();
        }

        /// <summary>
        /// Load beatmap using audio and beatmap file (.wdmap)
        /// </summary>
        /// <param name="audioClip">Audio Music</param>
        /// <param name="innerData">Existing saved map</param>
        public Beatmap(AudioClip audioClip, BeatmapPrivateData innerData)
        {
            this._audioClip = audioClip;
            this._privateData = innerData;
            _jsonRootNode = JSONNode.Parse(innerData.objectsMetaData);
        }

        /// <summary>
        /// Does the sequence object is set on that sequence line.
        /// </summary>
        /// <param name="si">Sequence index</param>
        /// <param name="ssi">Sub-sequence index</param>
        /// <param name="ci">Column index</param>
        /// <returns>True if the object was already set on that sequence or index out of that range, else then false</returns>
        public bool IsObjectSet(int si, int ssi, int ci)
        {
            try
            {
                // Check non blank
                return _privateData.objectsData[si][ssi][ci] != 0;
            }
            catch (IndexOutOfRangeException exc)
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"Sequence out of range: {exc}");
                #endif
                return true;
            }
        }

        /// <summary>
        /// Delete object at subline index.
        /// </summary>
        /// <param name="sublineIndex">Subline index target</param>
        /// <param name="ci">Column index</param>
        public void DeleteObjectKey(int sublineIndex, int ci)
        {
            // Convert subline into sequence
            int si = sublineIndex / _privateData.sequenceDiv;
            int ssi = sublineIndex % _privateData.sequenceDiv;
            DeleteObjectKey(si, ssi, ci);
        }

        /// <summary>
        /// Delete object at sequence index and sub-sequence index.
        /// </summary>
        /// <param name="si">Sequence index target</param>
        /// <param name="ssi">Sub-sequence index target</param>
        /// <param name="ci">Column index</param>
        /// <param name="limit">Limit starting index to remove</param>
        public void DeleteObjectKey(int si, int ssi, int ci, int limit = 0)
        {
            // Check which note type will be deleted
            int type = (int)GetNoteType(si, ssi, ci);
            if (type == 0)
            {
                // Nothing to be delete if it is blank
                return;
            }
            else if (type == 1)
            {
                // Set object data blank
                _privateData.objectsData[si][ssi][ci] = 0;

                // Delete meta data
                RemoveNoteMeta(GetSublineIndex(si, ssi), ci);
            }
            else if (type == 2)
            {
                // find the root for hold note
                int sublineIndex = GetSublineIndex(si, ssi);
                int esi = -1, essi = -1;
                while (sublineIndex >= limit)
                {
                    // Check root by using meta data
                    if (_jsonRootNode[$"{sublineIndex}"] != null)
                    {
                        if (_jsonRootNode[$"{sublineIndex}"][$"{ci}"] != null)
                        {
                            // Get end note subline index
                            int endSubline = _jsonRootNode[$"{sublineIndex}"][$"{ci}"]["endnote"].AsInt;

                            // Convert subline into sequence, then remove it
                            si = sublineIndex / _privateData.sequenceDiv;
                            ssi = sublineIndex % _privateData.sequenceDiv;

                            // Convert end subline into end sequence
                            esi = endSubline / _privateData.sequenceDiv;
                            essi = endSubline % _privateData.sequenceDiv;
                            break;
                        }
                    }

                    // Go back to previous subs
                    sublineIndex--;
                }

                // Set all element array object data on that range into blank
                for (int i = si; i <= esi; i++)
                {
                    // Create conditional for start and end sub-sequence index
                    int startSub = si <= i ? ssi : 0;
                    int endSub = si >= i ? essi : _privateData.sequenceDiv - 1;

                    // Loop through end sub sequence
                    for (int j = startSub; j <= endSub; j++)
                    {
                        // Set object blank
                        _privateData.objectsData[i][j][ci] = 0;
                    }
                }

                // Remove meta data
                RemoveNoteMeta(GetSublineIndex(si, ssi), ci);
            }
        }

        /// <summary>
        /// Add hit note key object.
        /// </summary>
        /// <param name="sublineIndex">Subline index target</param>
        /// <param name="ci">Column index</param>
        public void AddHitObjectKey(int sublineIndex, int ci)
        {
            // Convert subline index into sequence
            int si = sublineIndex / _privateData.sequenceDiv;
            int ssi = sublineIndex % _privateData.sequenceDiv;
            AddHitObjectKey(si, ssi, ci);
        }

        /// <summary>
        /// Add hit note key object.
        /// </summary>
        /// <param name="si">Sequence index target</param>
        /// <param name="ssi">Sub-sequence index target</param>
        /// <param name="ci">Column index</param>
        public void AddHitObjectKey(int si, int ssi, int ci)
        {
            // Handle invalid index
            while (ssi >= _privateData.sequenceDiv)
            {
                si++;
                ssi -= _privateData.sequenceDiv;
            }

            try
            {
                // Check if array data already filled, then delete an existsing object to be replace
                if (_privateData.objectsData[si][ssi][ci] != 0)
                {
                    DeleteObjectKey(si, ssi, ci);
                }

                // Set object data in array
                _privateData.objectsData[si][ssi][ci] = 1;
            }
            catch (IndexOutOfRangeException)
            {
                // Abort process
                return;
            }

            // Convert into subline index
            int sublineIndex = GetSublineIndex(si, ssi);

            // Check node existance
            if (_jsonRootNode[$"{sublineIndex}"] == null)
                _jsonRootNode[$"{sublineIndex}"] = JSONNode.Parse("{}");

            // Check column exists
            if (_jsonRootNode[$"{sublineIndex}"][$"{ci}"])
                _jsonRootNode[$"{sublineIndex}"][$"{ci}"] = JSON.Parse("{}");

            // Set note type object
            _jsonRootNode[$"{sublineIndex}"][$"{ci}"]["type"] = 1;

            // Overwrite edit progress
            _privateData.objectsMetaData = _jsonRootNode.ToString();
        }

        /// <summary>
        /// Add hold note key object.
        /// </summary>
        /// <param name="sublineIndex">Subline index target</param>
        /// <param name="ci">Column index</param>
        /// <param name="range">How long should press and hold</param>
        public void AddHoldObjectKey(int sublineIndex, int ci, int range = 2)
        {
            // Abort if the hold note range is less than 2
            if (range < 2) return;

            // Convert subline index into sequence
            int si = sublineIndex / _privateData.sequenceDiv;
            int ssi = sublineIndex % _privateData.sequenceDiv;
            AddHoldObjectKey(si, ssi, ci, range: range);
        }

        /// <summary>
        /// Add hold note key object.
        /// </summary>
        /// <param name="si">Sequence index target</param>
        /// <param name="ssi">Sub-sequence index target</param>
        /// <param name="ci">Column index</param>
        /// <param name="range">How long should press and hold</param>
        public void AddHoldObjectKey(int si, int ssi, int ci, int range = 2)
        {
            // Abort if the hold note range is less than 2
            if (range < 2) return;

            // Set object data in array
            int endSI = si, endSSI = ssi;
            try
            {
                for (int i = 0; i < range; i++)
                {
                    // Check if array data already filled, then delete an existsing object to be replace
                    if (_privateData.objectsData[endSI][endSSI][ci] != 0)
                    {
                        DeleteObjectKey(endSI, endSSI, ci, GetSublineIndex(si, ssi) + 1);
                    }

                    // Set object data
                    _privateData.objectsData[endSI][endSSI][ci] = 2;

                    // Adding index into end sequence
                    endSSI++;
                    if (endSSI >= _privateData.sequenceDiv)
                    {
                        endSI++;
                        endSSI = 0;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Abort process
                return;
            }

            // Insert hold note meta data
            int sublineIndex = GetSublineIndex(si, ssi);
            int endSublineIndex = GetSublineIndex(endSI, endSSI);

            // Check if subline does not exists yet, then create empty object
            if (_jsonRootNode[$"{sublineIndex}"] == null)
                _jsonRootNode[$"{sublineIndex}"] = JSON.Parse("{}");

            // Check column exists
            if (_jsonRootNode[$"{sublineIndex}"][$"{ci}"])
                _jsonRootNode[$"{sublineIndex}"][$"{ci}"] = JSON.Parse("{}");

            // Write down metadata
            _jsonRootNode[$"{sublineIndex}"][$"{ci}"]["type"] = 2;
            _jsonRootNode[$"{sublineIndex}"][$"{ci}"]["endnote"] = endSublineIndex;

            // Overwrite edit progress
            _privateData.objectsMetaData = _jsonRootNode.ToString();
        }

        /// <summary>
        /// Set line count in one sequence.
        /// Best number to be set are 2 power by N lines.
        /// This cannot be set less than the current line per sequence but can be placed in temporary variable.
        /// </summary>
        /// <param name="lines">Lines in one sequence</param>
        public void SetDivision(Division division)
        {
            // Check set lines is less than the current line, then ignore
            if (SequenceDivision.GetDivisionDivider() < division.GetDivisionDivider())
            {
                _privateData.FixedDivision = division;

                // Extend objects array
                ExtendByDivision();
            }

            // Always set it in temporary
            _privateData.tempSeqDiv = division.GetDivisionDivider();
        }

        /// <summary>
        /// Completely format objects in beatmap.
        /// </summary>
        public void ClearBeatmapObjects()
        {
            // Initialize objects placeholder
            int seqCount = _privateData.objectsData == null ? GetSequenceCount() : _privateData.objectsData.Length;
            _privateData.objectsData = new int[seqCount][][];
            for (int h = 0; h < seqCount; h++)
            {
                int div = SequenceDivision.GetDivisionDivider();
                _privateData.objectsData[h] = new int[div][];
                for (int i = 0; i < div; i++)
                {
                    _privateData.objectsData[h][i] = new int[_privateData.lines];
                    for (int j = 0; j < _privateData.lines; j++)
                    {
                        // Set object into blank note
                        _privateData.objectsData[h][i][j] = 0;
                    }
                }
            }

            // Empty meta data
            _jsonRootNode = JSON.Parse("{}");
            _privateData.objectsMetaData = _jsonRootNode.ToString();
        }

        /// <summary>
        /// Get position on current column note by sequence index.
        /// </summary>
        /// <param name="perfectLinePosition">Position on perfect score line</param>
        /// <param name="dropDir">Drop direction</param>
        /// <param name="offsetTime">Offset time from perfect line to define position</param>
        /// <returns>Exact position on that sequence when current timeline is on zero seconds</returns>
        public Vector3 GetNotePosition(Vector3 dropDir, float offsetTime)
        {
            // Convert offset time to distance by speed and direction
            return -dropDir * (offsetTime * DropSpeed);
        }

        /// <summary>
        /// Count objects in beatmap.
        /// Note: This will count each subline index that is filled including how much long the hold note does.
        /// </summary>
        /// <returns>Raw object data count</returns>
        public int GetRawObjectDataCount()
        {
            // Count all objects in level, deep into sequences
            int countingObj = 0;
            for (int i = 0; i < GetSequenceCount(); i++)
            {
                for (int j = 0; j < _privateData.sequenceDiv; j++)
                {
                    for (int k = 0; k < _privateData.lines; k++)
                    {
                        // Add counter, ignore blanks
                        countingObj += _privateData.objectsData[i][j][k] == 0 ? 0 : 1;
                    }
                }
            }

            // Return total count
            return countingObj;
        }

        /// <summary>
        /// Count objects in beatmap.
        /// </summary>
        /// <returns>Objects count</returns>
        public int GetObjectDataCount()
        {
            // Count all objects in level, only surfaces
            int countingObj = 0;
            foreach (JSONNode childNode in _jsonRootNode.Children)
            {
                countingObj += childNode.Count;
            }

            // Return object count
            return _jsonRootNode.Count;
        }

        /// <summary>
        /// Instead using sequence and sub sequence, convert index into global.
        /// This will convert sequence index into whole sub sequence count index.
        /// </summary>
        /// <param name="si">Sequence Index</param>
        /// <param name="ssi">Sub Sequence Index</param>
        /// <returns>Subline Index</returns>
        public int GetSublineIndex(int si, int ssi)
        {
            // Get whole sequences at index
            return si * _privateData.FixedDivision.GetDivisionDivider() + ssi;
        }

        /// <summary>
        /// Calculate time range in one sequence.
        /// </summary>
        /// <returns>Time per sequence</returns>
        public float GetTimePerSequence()
        {
            // Convert bpm to bps (beats per second), then spb (seconds per beat)
            float spb = 1f / (BPM / 60f);

            // Times 4/4 beat
            return spb * 4f;
        }

        /// <summary>
        /// Calculate time range in one sub sequence.
        /// </summary>
        /// <returns>Time per sub sequence</returns>
        public float GetTimePerSubSequence()
        {
            // Calculate time per sub sequence
            return GetTimePerSequence() / SequenceDivision.GetDivisionDivider();
        }

        /// <summary>
        /// Calculate specific song time by sequence index and sub sequence index.
        /// </summary>
        /// <returns>Specific at song time</returns>
        public float GetTimeAtSequence(int sublineIndex)
        {
            // Convert subline into sequence index
            int si = sublineIndex / _privateData.sequenceDiv;
            int ssi = sublineIndex % _privateData.sequenceDiv;
            return GetTimeAtSequence(si, ssi);
        }

        /// <summary>
        /// Calculate specific song time by sequence index and sub sequence index.
        /// </summary>
        /// <returns>Specific at song time</returns>
        public float GetTimeAtSequence(int si, int ssi)
        {
            // Calculate time on that sequence
            return GetTimePerSequence() * si + GetTimePerSubSequence() * ssi;
        }

        /// <summary>
        /// Count sequences from current song.
        /// </summary>
        /// <returns>Sequences by song length</returns>
        public int GetSequenceCount()
        {
            // Count sequence by bpm
            float spb = 1f / (_privateData.bpm / 60f); // Get seconds per beat
            float sumBeats = _audioClip.length / spb;
            return Mathf.CeilToInt(sumBeats / 4f);
        }

        /// <summary>
        /// Get note type from object data.
        /// </summary>
        /// <param name="si">Sequence index</param>
        /// <param name="ssi">Sub-sequence index</param>
        /// <param name="ci">Column index</param>
        public NoteType GetNoteType(int si, int ssi, int ci)
        {
            try
            {
                // Return object type from indexes
                return (NoteType)_privateData.objectsData[si][ssi][ci];
            }
            catch (IndexOutOfRangeException exc)
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"Getting beatmap object index out of range. " +
                    $"Index of object is out of range, Trying to access Sequence {si} Sub {ssi} at column {ci}. " +
                    $"You can ignore this message: {exc}");
                #endif

                // Default return blank note
                return NoteType.Blank;
            }
        }

        public object Clone()
        {
            // Clone object
            return new Beatmap(_audioClip, (BeatmapPrivateData)_privateData.Clone());
        }

        private void RemoveNoteMeta(int sublineIndex, int columnIndex)
        {
            // Check subline meta data exists
            if (_jsonRootNode[$"{sublineIndex}"] != null)
            {
                // Check column exists, then remove
                if (_jsonRootNode[$"{sublineIndex}"][$"{columnIndex}"] != null)
                {
                    _jsonRootNode[$"{sublineIndex}"].Remove($"{columnIndex}");
                }

                // If there's no more data in that subline, then remove it
                if (_jsonRootNode[$"{sublineIndex}"].Count == 0)
                {
                    _jsonRootNode.Remove($"{sublineIndex}");
                }

                // Overwrite edit progress
                _privateData.objectsMetaData = _jsonRootNode.ToString();

                Debug.Log($"Meta data has been deleted on subline {sublineIndex} at column {columnIndex}");
            }
        }

        /// <summary>
        /// Get information about the root of the note itself.
        /// Useful to catch hold note root.
        /// </summary>
        /// <param name="sublineIndex">Subline index target</param>
        /// <param name="columnIndex">At column index</param>
        /// <returns>Subline index for root</returns>
        public int GetRootNote(int sublineIndex, int columnIndex)
        {
            // Convert subline index into sequences
            int si = sublineIndex / SequenceDivision.GetDivisionDivider();
            int ssi = sublineIndex % SequenceDivision.GetDivisionDivider();
            int typeInObjectData = _privateData.objectsData[si][ssi][columnIndex];

            // Check which type it does, get root of that note
            if (typeInObjectData != 2) return sublineIndex;
            else
            {
                // SPECIAL CASE: Search for hold note root
                do
                {
                    // Go back one step sub sequence
                    ssi--;

                    // Check subs out of range
                    if (ssi < 0)
                    {
                        si--;
                        ssi = SequenceDivision.GetDivisionDivider() - 1;
                    }

                    // Check hold note is the root note to find the root
                    if (_privateData.objectsData[si][ssi][columnIndex] == 2 && _jsonRootNode[$"{GetSublineIndex(si, ssi)}"])
                        break;
                } while (si != 0 && ssi != 0);

                // Convert back into subline index
                return GetSublineIndex(si, ssi);
            }
        }

        private void ExtendByBPM()
        {
            // Compare changes on current length object data of sequences
            if (_privateData.objectsData.Length < GetSequenceCount())
            {
                // Copy objects into new extended array
                int seqCount = GetSequenceCount();
                int[][][] temp = new int[seqCount][][];
                for (int h = 0; h < seqCount; h++)
                {
                    int div = SequenceDivision.GetDivisionDivider();
                    temp[h] = new int[div][];
                    for (int i = 0; i < div; i++)
                    {
                        temp[h][i] = new int[_privateData.lines];
                        for (int j = 0; j < _privateData.lines; j++)
                        {
                            // Check current length of current object data
                            if (h >= _privateData.objectsData.Length)
                            {
                                // Set object into blank note on the extended objects
                                temp[h][i][j] = 0;
                            }
                            else
                            {
                                // Copy object data
                                temp[h][i][j] = _privateData.objectsData[h][i][j];
                            }
                        }
                    }
                }

                // Assign new object data
                _privateData.objectsData = temp;
            }
        }

        private void ExtendByDivision()
        {

        }
    }

}

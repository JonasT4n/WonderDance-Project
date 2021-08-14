using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace WonderDanceProj
{
    [CreateAssetMenu(fileName = "Sprite Asset", menuName = "Project Design/Sprite Asset")]
    public class SpriteAssetObj : ScriptableObject
    {
        [Header("Asset Requirements")]
        [SerializeField, ShowAssetPreview]
        private Sprite[]    _keySprites = { null };
        [SerializeField]
        private Color[]     _fillerColor = { Color.white };

        public Sprite GetKeySpriteByIndex(int index)
        {
            try
            {
                return _keySprites[index];
            }
            catch (System.IndexOutOfRangeException)
            {
                return null;
            }
        }

        public Color GetFillerColorByindex(int index)
        {
            try
            {
                return _fillerColor[index];
            }
            catch (System.IndexOutOfRangeException)
            {
                return Color.white;
            }
        }
    }

}

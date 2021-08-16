using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace WonderDanceProj
{
    [RequireComponent(typeof(RectTransform))]
    public class FloatFadeText : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] 
        private TextMeshProUGUI     _textMesh = null;

        [Header("Attributes")]
        [SerializeField] 
        private AnimationCurve      _inOutMotion = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] 
        private AnimationCurve      _inOutOpacity = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField] 
        private Vector2             _moveByDir = new Vector2(0f, 20f);

        // Temporary variables
        private IEnumerator         _fadingRoutine;

        #region Properties
        public RectTransform rectTransform => (RectTransform)transform;
        public string Text
        {
            get => _textMesh.text;
            set => _textMesh.text = value;
        }
        #endregion

        #region Unity BuiltIn Methods
        private void Start()
        {
            // Create fading routine
            _fadingRoutine = FadingRoutine();
            StartCoroutine(_fadingRoutine);
        }

        private void OnDestroy()
        {
            // Release routine
            _fadingRoutine = null;
        }
        #endregion

        public void SetInitPosition(Vector2 rectTransformPosition)
        {
            // Get all components
            rectTransform.position = rectTransformPosition;
        }

        private IEnumerator FadingRoutine()
        {
            // Set all required values
            float tempSec = 0f;
            float maxMotionTime = _inOutMotion[_inOutMotion.length - 1].time;
            float maxOpacityTime = _inOutOpacity[_inOutOpacity.length - 1].time;
            float endSecond = maxMotionTime > maxOpacityTime ? maxMotionTime : maxOpacityTime;
            Vector2 initPosition = rectTransform.anchoredPosition;
            Color currentColor = _textMesh.color;

            // Run animation
            while (tempSec < endSecond)
            {
                tempSec += Time.deltaTime;

                float motionPercent = tempSec > maxMotionTime ? maxMotionTime : _inOutMotion.Evaluate(tempSec);
                float opacityPercent = tempSec > maxOpacityTime ? maxOpacityTime : _inOutOpacity.Evaluate(tempSec);

                Vector2 currentPos = Vector2.Lerp(initPosition, initPosition + _moveByDir, motionPercent);
                _textMesh.color = new Color(currentColor.r, currentColor.g, currentColor.b, opacityPercent);
                rectTransform.anchoredPosition = currentPos;

                yield return new WaitForEndOfFrame();
            }

            // Destroy object immediately after animation is finished
            Destroy(gameObject);
        }
    }

}

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace WonderDanceProj
{
    public static class EditorGUIStyles
    {
        public static GUIStyle GetLabelHeaderStyle()
        {
            // Create label header style
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            return style;
        }

        public static GUIStyle GetLabelHeaderStyle(TextAnchor alignment, int fontSize)
        {
            // Create label header style
            GUIStyle style = new GUIStyle();
            style.alignment = alignment;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = fontSize;
            style.normal.textColor = Color.white;
            return style;
        }

        public static GUIStyle GetBoxStyle(Color color)
        {
            // Create box style
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = BoxTexture(2, 2, color);
            return style;
        }

        private static Texture2D BoxTexture(int width, int height, Color color)
        {
            // Create texture 2d for GUI
            Texture2D resultTexture = new Texture2D(width, height);

            // Set each pixel color
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                    resultTexture.SetPixel(i, j, color);
            }

            // Apply texture changes
            resultTexture.Apply();
            return resultTexture;
        }
    }
}
#endif

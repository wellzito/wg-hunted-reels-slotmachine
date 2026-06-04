using UnityEngine;
using System.Collections.Generic;
using Spine.Unity;

namespace WG_Casino.Setup
{
    [CreateAssetMenu(fileName = "Symbol", menuName = "WG Casino/Symbol")]
    public class WG_SymbolSO : ScriptableObject
    {
        public Sprite defaultSprite;
        public Sprite moveSprite;

        [Header("Spine Default Settings")]
        public bool useSpineDefault = false;
        public SkeletonGraphic defaultSpinePreview;

        [Header("Spine Default Animations")]
        [Tooltip("Animação que toca quando o símbolo está parado (idle)")]
        public string defaultIdleAnimation = "";
        [Tooltip("Animação que toca quando o símbolo está girando (spin)")]
        public string defaultSpinAnimation = "";

        [Header("Animation Settings")]
        [Tooltip("Lista de sprites que compőem a animaçăo")]
        public List<Sprite> m_reelIconsList = new List<Sprite>();
        public Material materialsLine;
        public bool onSpine;
        public SkeletonGraphic skeletonGraphic;

        [System.Serializable]
        public class RectTransformVector
        {
            public Vector2 anchorMin = Vector2.zero;
            public Vector2 anchorMax = Vector2.one;
            public Vector2 offsetMin = Vector2.zero;
            public Vector2 offsetMax = Vector2.zero;
            public Vector2 pivot = Vector2.zero;
            public Vector2 anchoredPosition = Vector2.zero;
            public Vector3 localScale = Vector3.one;
        }

        [Header("RectTransform Settings")]
        public RectTransformVector rectTransformVector = new RectTransformVector();
        public RectTransformVector rectTransformVectorIcon = new RectTransformVector();
        public RectTransformVector rectTransformVectorIconSpin = new RectTransformVector();

        // Método para obter a animação correta baseada no estado
        public string GetDefaultAnimation(bool isSpinning)
        {
            if (isSpinning && !string.IsNullOrEmpty(defaultSpinAnimation))
                return defaultSpinAnimation;

            if (!string.IsNullOrEmpty(defaultIdleAnimation))
                return defaultIdleAnimation;

            // Fallback: primeira animação disponível
            if (defaultSpinePreview != null && defaultSpinePreview.SkeletonData != null &&
                defaultSpinePreview.SkeletonData.Animations != null && defaultSpinePreview.SkeletonData.Animations.Count > 0)
            {
                return defaultSpinePreview.SkeletonData.Animations.Items[0].Name;
            }

            return "";
        }

        // Método para obter o RectTransformVector correto baseado no estado
        public RectTransformVector GetRectTransformVector(bool isSpinning, bool isIcon = true)
        {
            if (isIcon)
            {
                return isSpinning ? rectTransformVectorIconSpin : rectTransformVectorIcon;
            }
            else
            {
                return rectTransformVector;
            }
        }
    }
}
using PrimeTween;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WG_Casino.SlotMachine;
using WG_Casino.Setup;

namespace WG_Casino
{
    public class WG_SlotMachineIcon : MonoBehaviour
    {
        public GameObject bg;
        public Image m_renderer;
        public RectTransform m_rectTransformAnim;
        [HideInInspector] public Sprite m_sprite;
        public int m_spriteNumber;

        public GameObject lineMark;
        public Image linePayment;
        public Image markImg;
        public Image fadeImg;
        public Image cardGlowImg;
        public TextMeshProUGUI text;

        private List<Sprite> animationSprites;
        private Coroutine animationCoroutine;
        private bool isAnimating = false;
        [SerializeField] private float animationSpeed = 10f;
        private SkeletonGraphic skeletonGraphic;
        private SkeletonGraphic defaultSpineInstance;
        [HideInInspector] public bool onShakeSlot = false;
        public bool isActive { get; set; } = false;

        private RectTransform rectIcon;
        private bool useSpineDefaultCache = false;
        private bool isSpinningCache = false;
        private WG_SlotMachineReel parentReel;

        private WG_SlotMachineReel GetParentReel()
        {
            if (parentReel == null)
            {
                parentReel = GetComponentInParent<WG_SlotMachineReel>();
            }
            return parentReel;
        }

        private bool IsReelSpinning()
        {
            var reel = GetParentReel();
            if (reel == null) return false;

            // Acessa diretamente as variáveis públicas do reel
            return reel.m_spinning || reel.m_preSpinning || reel.m_stopping || reel.m_stoppingEnd;
        }

        private void ApplyRectTransformVector(WG_SymbolSO.RectTransformVector rectVector)
        {
            if (m_renderer == null) return;

            var rect = m_renderer.rectTransform;
            rect.anchorMin = rectVector.anchorMin;
            rect.anchorMax = rectVector.anchorMax;
            rect.offsetMin = rectVector.offsetMin;
            rect.offsetMax = rectVector.offsetMax;
            rect.pivot = rectVector.pivot;
            rect.anchoredPosition = rectVector.anchoredPosition;
            rect.localScale = rectVector.localScale;
        }

        private void ApplyRectTransformToSpine(WG_SymbolSO.RectTransformVector rectVector)
        {
            if (defaultSpineInstance == null) return;

            var rect = defaultSpineInstance.GetComponent<RectTransform>();
            rect.anchorMin = rectVector.anchorMin;
            rect.anchorMax = rectVector.anchorMax;
            rect.offsetMin = rectVector.offsetMin;
            rect.offsetMax = rectVector.offsetMax;
            rect.pivot = rectVector.pivot;
            rect.anchoredPosition = rectVector.anchoredPosition;
            rect.localScale = rectVector.localScale;
        }

        public void SetIconData(Sprite _sprite, int _num, float _y, bool useAnimation = false)
        {
            m_sprite = _sprite;
            m_spriteNumber = _num;

            var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
            if (m_symbols != null && m_spriteNumber < m_symbols.Count)
            {
                useSpineDefaultCache = m_symbols[m_spriteNumber].useSpineDefault;
            }

            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 currentPos = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(currentPos.x, _y);
            }
            else
            {
                transform.localPosition = new Vector3(transform.localPosition.x, _y, transform.localPosition.z);
            }

            if (!useSpineDefaultCache)
            {
                if (m_renderer != null)
                {
                    m_renderer.sprite = _sprite;
                    m_renderer.enabled = true;

                    if (m_symbols != null && m_spriteNumber < m_symbols.Count)
                    {
                        bool isSpinning = IsReelSpinning();
                        var rectVector = m_symbols[m_spriteNumber].GetRectTransformVector(isSpinning, true);
                        ApplyRectTransformVector(rectVector);
                    }
                }
                if (text != null)
                {
                    text.text = _num.ToString();
                    text.enabled = true;
                }
            }
            else
            {
                if (m_renderer != null)
                {
                    m_renderer.enabled = false;
                }
                if (text != null)
                {
                    text.enabled = false;
                }
            }

            var setup = WG_SlotMachine.Instance;
            if (setup != null)
            {
                setup.OnAnimationSlots = setup.m_useAnimationSlots;
            }
        }

        public void SetAnimationSprites(List<Sprite> sprites)
        {
            animationSprites = sprites;
        }

        public void StartAnimation()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (isAnimating || animationSprites == null || animationSprites.Count == 0) return;

            StopAnimation();
            animationCoroutine = StartCoroutine(AnimateSprite());
        }

        public void StopAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            isAnimating = false;
            RestoreDefaultState();
        }

        private void RestoreDefaultState()
        {
            var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
            if (m_symbols == null || m_spriteNumber >= m_symbols.Count) return;

            var symbolData = m_symbols[m_spriteNumber];

            if (symbolData.useSpineDefault && symbolData.defaultSpinePreview != null)
            {
                if (m_renderer != null)
                    m_renderer.enabled = false;
                if (text != null)
                    text.enabled = false;

                bool isSpinning = IsReelSpinning();
                ShowDefaultSpine(isSpinning);
            }
            else
            {
                if (defaultSpineInstance != null)
                {
                    Destroy(defaultSpineInstance.gameObject);
                    defaultSpineInstance = null;
                }

                if (m_renderer != null)
                {
                    m_renderer.enabled = true;
                    m_renderer.sprite = m_sprite;

                    if (m_symbols != null && m_spriteNumber < m_symbols.Count)
                    {
                        bool isSpinning = IsReelSpinning();
                        var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                        ApplyRectTransformVector(rectVector);
                    }
                }
                if (text != null)
                {
                    text.enabled = true;
                    text.text = m_spriteNumber.ToString();
                }
            }
        }

        private void ClearAllSpineInstances()
        {
            if (defaultSpineInstance != null)
            {
                Destroy(defaultSpineInstance.gameObject);
                defaultSpineInstance = null;
            }

            if (skeletonGraphic != null)
            {
                Destroy(skeletonGraphic.gameObject);
                skeletonGraphic = null;
            }
        }

        private IEnumerator AnimateSprite()
        {
            isAnimating = true;
            int currentFrame = 0;
            float frameDuration = 1f / animationSpeed;
            float timer = 0f;

            while (true)
            {
                if (!gameObject.activeInHierarchy)
                {
                    StopAnimation();
                    yield break;
                }

                timer += Time.deltaTime;

                if (timer >= frameDuration)
                {
                    timer = 0f;
                    currentFrame = (currentFrame + 1) % animationSprites.Count;
                    if (m_renderer != null)
                    {
                        m_renderer.sprite = animationSprites[currentFrame];
                    }
                }

                yield return null;
            }
        }

        public void SetAnimationStart()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            var setup = WG_SlotMachine.Instance;
            if (setup == null) return;

            setup.OnAnimationSlots = setup.m_useAnimationSlots;

            var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
            if (m_symbols == null || m_spriteNumber >= m_symbols.Count) return;

            if (!setup.OnAnimationSlots) return;

            ClearAllSpineInstances();

            if (!m_symbols[m_spriteNumber].onSpine)
            {
                List<Sprite> animSprites = setup.GetAnimationSpritesForSymbol(m_spriteNumber);
                if (animSprites != null && animSprites.Count > 0)
                {
                    if (defaultSpineInstance != null)
                    {
                        defaultSpineInstance.gameObject.SetActive(false);
                    }

                    m_renderer.enabled = true;
                    if (text != null) text.enabled = false;

                    SetAnimationSprites(animSprites);
                    StartAnimation();
                }
            }
            else
            {
                if (m_renderer != null)
                    m_renderer.enabled = false;
                if (text != null)
                    text.enabled = false;

                skeletonGraphic = Instantiate(m_symbols[m_spriteNumber].skeletonGraphic, m_rectTransformAnim.transform);
                var rect = skeletonGraphic.GetComponent<RectTransform>();
                rect.SetParent(m_rectTransformAnim.transform, false);

                var rectVector = m_symbols[m_spriteNumber].rectTransformVector;

                rect.anchorMin = rectVector.anchorMin;
                rect.anchorMax = rectVector.anchorMax;
                rect.offsetMin = rectVector.offsetMin;
                rect.offsetMax = rectVector.offsetMax;
                rect.pivot = rectVector.pivot;
                rect.anchoredPosition = rectVector.anchoredPosition;
                rect.localScale = rectVector.localScale;
            }
        }

        public void Show(Sprite _sprite, int _num)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            m_sprite = _sprite;
            m_spriteNumber = _num;

            var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
            if (m_symbols == null || m_spriteNumber >= m_symbols.Count) return;

            var symbolData = m_symbols[m_spriteNumber];

            useSpineDefaultCache = symbolData.useSpineDefault;

            // IMPORTANTE: PEGAR O ESTADO REAL DO REEL AGORA
            bool isSpinningNow = IsReelSpinning();

            if (symbolData.useSpineDefault && symbolData.defaultSpinePreview != null)
            {
                if (m_renderer != null)
                {
                    m_renderer.enabled = false;
                }
                if (text != null)
                {
                    text.enabled = false;
                }

                ShowDefaultSpine(isSpinningNow);
            }
            else
            {
                if (defaultSpineInstance != null)
                {
                    Destroy(defaultSpineInstance.gameObject);
                    defaultSpineInstance = null;
                }

                if (m_renderer != null)
                {
                    m_renderer.sprite = _sprite;
                    m_renderer.enabled = true;

                    if (m_symbols != null && m_spriteNumber < m_symbols.Count)
                    {
                        var rectVector = symbolData.GetRectTransformVector(isSpinningNow, true);
                        ApplyRectTransformVector(rectVector);
                    }
                }

                if (text != null)
                {
                    text.text = _num.ToString();
                    text.enabled = true;
                }
            }

            if (onShakeSlot && gameObject.activeInHierarchy)
                Tween.ShakeScale(transform, strength: new Vector3(0.25f, 0.25f, 0.25f), duration: 0.5f);
        }

        public void SetSpinningState(bool isSpinning)
        {
            isSpinningCache = isSpinning;

            // Para símbolos SEM spine default: atualizar o rect transform do sprite
            if (!useSpineDefaultCache && m_renderer != null && m_renderer.enabled)
            {
                var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
                if (m_symbols != null && m_spriteNumber < m_symbols.Count)
                {
                    var symbolData = m_symbols[m_spriteNumber];
                    var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                    ApplyRectTransformVector(rectVector);
                }
            }

            // Para símbolos COM spine default: trocar a animação E o rect transform
            if (useSpineDefaultCache && defaultSpineInstance != null && defaultSpineInstance.IsValid)
            {
                var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
                if (m_symbols != null && m_spriteNumber < m_symbols.Count)
                {
                    var symbolData = m_symbols[m_spriteNumber];
                    string animationName = symbolData.GetDefaultAnimation(isSpinning);

                    // TROCAR ANIMAÇÃO
                    if (!string.IsNullOrEmpty(animationName))
                    {
                        defaultSpineInstance.AnimationState.SetAnimation(0, animationName, true);
                    }

                    // TROCAR RECT TRANSFORM TAMBÉM
                    var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                    ApplyRectTransformToSpine(rectVector);

                    //Debug.LogError($"SetSpinningState: isSpinning={isSpinning}, scale={rectVector.localScale}");
                }
            }
        }

        private void ShowDefaultSpine(bool isSpinning)
        {
            var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
            if (m_symbols == null || m_spriteNumber >= m_symbols.Count) return;

            if (defaultSpineInstance != null)
            {
                if (!defaultSpineInstance.name.Contains(m_spriteNumber.ToString()))
                {
                    Destroy(defaultSpineInstance.gameObject);
                    defaultSpineInstance = null;
                }
            }


            var symbolData = m_symbols[m_spriteNumber];

            if (defaultSpineInstance != null)
            {
                if (defaultSpineInstance.skeletonDataAsset == symbolData.defaultSpinePreview.skeletonDataAsset)
                {
                    defaultSpineInstance.gameObject.SetActive(true);

                    string animationName = symbolData.GetDefaultAnimation(isSpinning);
                    if (!string.IsNullOrEmpty(animationName))
                    {
                        defaultSpineInstance.AnimationState.SetAnimation(0, animationName, true);
                    }

                    // ATUALIZAR RECT TRANSFORM
                    var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                    ApplyRectTransformToSpine(rectVector);

                    //Debug.LogError($"ShowDefaultSpine (reutilizando): isSpinning={isSpinning}, scale={rectVector.localScale}");
                    return;
                }
                else
                {
                    Destroy(defaultSpineInstance.gameObject);
                    defaultSpineInstance = null;
                }
            }

            if (skeletonGraphic != null)
            {
                Destroy(skeletonGraphic.gameObject);
                skeletonGraphic = null;
            }

            if (symbolData.defaultSpinePreview != null)
            {
                defaultSpineInstance = Instantiate(symbolData.defaultSpinePreview, m_rectTransformAnim.transform);
                defaultSpineInstance.name = $"{defaultSpineInstance.name} {m_spriteNumber}";

                var rect = defaultSpineInstance.GetComponent<RectTransform>();
                rect.SetParent(m_rectTransformAnim.transform, false);

                var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                rect.anchorMin = rectVector.anchorMin;
                rect.anchorMax = rectVector.anchorMax;
                rect.offsetMin = rectVector.offsetMin;
                rect.offsetMax = rectVector.offsetMax;
                rect.pivot = rectVector.pivot;
                rect.anchoredPosition = rectVector.anchoredPosition;
                rect.localScale = rectVector.localScale;

                //Debug.LogError($"ShowDefaultSpine (criando nova): isSpinning={isSpinning}, scale={rectVector.localScale}");

                string animationName = symbolData.GetDefaultAnimation(isSpinning);
                if (!string.IsNullOrEmpty(animationName))
                {
                    defaultSpineInstance.AnimationState.SetAnimation(0, animationName, true);
                }
            }
        }

        void ShowWin()
        {
            if (onShakeSlot && gameObject.activeInHierarchy)
                Tween.ShakeScale(transform, strength: new Vector3(0.25f, 0.25f, 0.25f), duration: 0.5f);
        }

        public void SetPosY(float _y)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 currentPos = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(currentPos.x, _y);
            }
            else
            {
                transform.localPosition = new Vector3(transform.localPosition.x, _y, transform.localPosition.z);
            }
        }

        public void ShowLineMark()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
            if (m_symbols == null || m_spriteNumber >= m_symbols.Count) return;

            if (!m_symbols[m_spriteNumber].onSpine)
                lineMark.SetActive(true);

            if (markImg != null)
                markImg.sprite = m_sprite;

            ShowWin();
            SetAnimationStart();
        }

        public void ShowLineS(int lineIndex)
        {
            if (gameObject.activeInHierarchy)
                StartCoroutine(DisableLines());
        }

        public void ShowLine(Sprite s)
        {
            if (!gameObject.activeInHierarchy) return;

            var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
            if (m_symbols == null || m_spriteNumber >= m_symbols.Count) return;

            linePayment.material = m_symbols[m_spriteNumber].materialsLine;
            linePayment.sprite = s;
            linePayment.gameObject.SetActive(true);
            StartCoroutine(DisableLines());
        }

        public void Fade(bool val)
        {
            if (fadeImg != null)
                fadeImg.gameObject.SetActive(val);
        }

        public void CardGlow(bool val)
        {
            if (cardGlowImg != null)
                cardGlowImg.gameObject.SetActive(val);
        }

        IEnumerator DisableLines()
        {
            yield return new WaitForSeconds(2);
            if (lineMark != null)
                lineMark.SetActive(false);
            if (linePayment != null)
                linePayment.gameObject.SetActive(false);
        }

        public RectTransform GetRectTransform()
        {
            return GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            ClearAllSpineInstances();
        }

        private void OnDisable()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            isAnimating = false;
        }

        private void OnEnable()
        {
            if (useSpineDefaultCache)
            {
                RestoreDefaultState();
            }
            else if (m_renderer != null && m_sprite != null)
            {
                m_renderer.sprite = m_sprite;
                m_renderer.enabled = true;

                var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
                if (m_symbols != null && m_spriteNumber < m_symbols.Count)
                {
                    bool isSpinning = IsReelSpinning();
                    var symbolData = m_symbols[m_spriteNumber];
                    var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                    ApplyRectTransformVector(rectVector);
                }

                if (text != null)
                {
                    text.text = m_spriteNumber.ToString();
                    text.enabled = true;
                }
            }
        }
    }
}
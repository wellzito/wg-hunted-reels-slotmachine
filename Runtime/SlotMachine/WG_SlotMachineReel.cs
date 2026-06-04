using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static WG_Casino.SlotMachine.WG_SlotMachine;

namespace WG_Casino.SlotMachine
{
    public enum ReelAnimationType
    {
        CascadeFall,
        IndependentRows
    }

    [System.Serializable]
    public class WG_SlotMachineReel : MonoBehaviour
    {
        [Header("Animation Configuration")]
        [SerializeField] private ReelAnimationType m_animationType = ReelAnimationType.IndependentRows;
        public ReelAnimationType AnimationType { get { return m_animationType; } set { m_animationType = value; } }

        [Header("Configuration")]
        private bool m_initialized = false;
        [HideInInspector] public float m_initialDelay = 0f;
        private float m_initialDelayCurrent = 0f;

        [SerializeField] private float m_preSpinSpeed = 0.5f;
        [HideInInspector] public float m_preSpinDuration = 1f;
        [SerializeField] private float m_speed = 0.125f;
        [SerializeField] private float m_speedTurbo = 100f;
        [SerializeField] private float m_endSpinSpeed = 0.5f;
        public float m_endSpinDuration = 1f;
        [SerializeField] private float m_offset = 150f;
        public bool m_spinning = false;
        public bool m_preSpinning = false;
        [Header("PreSpin Pause Configuration")]
        [SerializeField] private float m_preSpinPauseDuration = 0.2f;
        public bool m_preSpinPause = false;
        private float m_preSpinPauseTimer = 0f;
        public bool m_stopping = false;
        public bool m_stoppingEnd = false;

        [Header("Icons")]
        public WG_SlotMachineIcon[] m_icons;

        [Header("Position References")]
        [SerializeField] private Vector2 m_topPosition;
        [SerializeField] private float m_topPos = 0;
        private float m_initialTopPos = 0;

        public int m_index = 0;
        public List<int> m_IndexIcons = new List<int>();
        public List<int> m_finalIcons = new List<int>();

        private int m_slipping = 0;
        private float m_preSpinTimer = 0f;
        private float m_endSpinTimer = 0f;
        private float m_finalSpinTimer = 0f;
        WG_SlotMachine m_machine;
        [HideInInspector] public bool onShakeSlot = false;

        public List<int> Reel => m_reel;
        private List<int> m_reel = new List<int>();
        private int[,] m_iconspool = new int[,]
        {
            { 0, 0, 1, 1, 2, 2, 3, 4, 4, 4, 5, 5, 6, 6, 7, 8, 9, 10, 11, 12, 13 }
        };

        private int[] m_slippingPool = new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 2, 2, 3 };

        public bool IsSpinning { get { return m_spinning; } }
        public bool onTest;
        private RectMask2D mask2D;

        // Variáveis para CascadeFall
        [HideInInspector] public float m_cascadeFallSpeedStart = 2500f;
        [HideInInspector] public float m_cascadeFallSpeedEnd = 1000f;
        [HideInInspector] public float m_cascadeDelayBetweenIcons = 0.1f;
        public float m_cascadeResetHeightOffset = 300f;
        [SerializeField] private int m_cascadeStartSlot = 2;
        [SerializeField] private int m_cascadeEndSlot = 5;
        [SerializeField] private float m_cascadeStartTopOffset = 0f;
        [SerializeField] private float m_cascadeStartDelay = 0f;

        private bool m_cascadeActive = false;
        private bool m_cascadeReturning = false;
        private float m_cascadeTimer = 0f;
        private bool m_cascadeWaitingStart = false;
        private bool m_cascadeCompleted = false;

        // NOVA VARIÁVEL PARA CONTROLAR O ESTADO ANTERIOR
        private bool m_lastSpinState = false;

        public void InitializeReel(WG_SlotMachine _machine, Sprite[] iconsArray, Sprite[] iconsArrayMove)
        {
            mask2D = GetComponent<RectMask2D>();
            if (mask2D) mask2D.enabled = false;

            m_machine = _machine;
            m_slipping = 0;

            m_topPosition = m_icons[0].GetComponent<RectTransform>().anchoredPosition;
            m_topPos = m_topPosition.y;
            m_initialTopPos = m_topPos;

            if (m_animationType == ReelAnimationType.CascadeFall)
            {
                if (m_icons.Length > 1)
                    m_icons[1].gameObject.SetActive(false);
            }
            else
            {
                if (m_icons.Length > 1)
                    m_icons[1].gameObject.SetActive(WG_SlotMachine.Instance.onSimulatorIcon);
            }

            m_reel = new List<int>();
            int length = m_iconspool.GetLength(1);
            for (int i = 0; i < length; i++)
            {
                int val = m_iconspool[0, i];
                m_reel.Add(val);

                if (!_machine.m_reelIcons.ContainsKey(val))
                    _machine.m_reelIcons.Add(val, iconsArray[val]);

                if (!_machine.m_reelIconsMove.ContainsKey(val))
                    _machine.m_reelIconsMove.Add(val, iconsArrayMove[val]);
            }

            Shuffle(m_reel);
            m_index = Random.Range(0, m_reel.Count);

            for (int i = 0; i < m_icons.Length; i++)
            {
                float _y = m_topPos - i * m_offset;
                int symbolIndex = (m_index + i) % m_reel.Count;
                if (symbolIndex < 0) symbolIndex += m_reel.Count;
                int spriteNumber = m_reel[symbolIndex];
                Sprite sprite = _machine.m_reelIcons[spriteNumber];
                m_icons[i].SetIconData(sprite, spriteNumber, _y);
            }

            m_icons[1].gameObject.SetActive(false);
            m_initialized = true;
        }

        public void UpdateAllIconsSpinState(bool isSpinning)
        {
            foreach (var icon in m_icons)
            {
                if (icon != null)
                {
                    icon.SetSpinningState(isSpinning);
                }
            }
        }

        public void StartSpinning()
        {
            if (mask2D != null) mask2D = GetComponent<RectMask2D>();

            for (int i = 0; i < m_icons.Length; i++)
            {
                var icon = m_icons[i];
                icon.StopAnimation();
                icon.lineMark.SetActive(false);
                icon.linePayment.gameObject.SetActive(false);
                icon.Fade(false);
                icon.CardGlow(false);
                icon.isActive = false;
            }

            if (m_animationType == ReelAnimationType.CascadeFall)
            {
                m_cascadeCompleted = false;
                m_cascadeActive = false;
                m_cascadeReturning = false;
                m_cascadeWaitingStart = false;
                m_cascadeTimer = 0f;
                m_spinning = true;
                StartCascadeFall();
                UpdateAllIconsSpinState(true);
                return;
            }

            m_preSpinning = true;
            m_preSpinTimer = 0f;
            m_endSpinTimer = 0f;
            m_finalSpinTimer = 0f;
            m_initialDelayCurrent = 0;
            m_preSpinPause = false;
            m_preSpinPauseTimer = 0f;
            UpdateAllIconsSpinState(true);
        }

        public void StopSpinning(bool _slipping = true)
        {
            if (m_animationType == ReelAnimationType.CascadeFall)
            {
                return;
            }

            m_stopping = true;
            m_slipping = (_slipping) ? m_slippingPool[Random.Range(0, m_slippingPool.Length - 1)] : 0;

            if (!_slipping)
            {
                m_spinning = false;
                UpdateAllIconsSpinState(false);
                ApplyIndependentRowsIcons();

                if (m_topPos != m_topPosition.y)
                {
                    m_topPos = m_topPosition.y;
                }

                m_icons[1].gameObject.SetActive(false);
            }
        }

        private void ApplyIndependentRowsIcons()
        {
            if (m_finalIcons == null || m_finalIcons.Count == 0)
            {
                Debug.LogWarning("m_finalIcons está vazio ou nulo. Pulando ApplyIndependentRowsIcons.");
                return;
            }

            int lineIndex = 0;
            for (int i = 0; i < WG_SlotMachine.Instance.lineRows.Count; i++)
            {
                WG_LineRows lineRows = WG_SlotMachine.Instance.lineRows[i];
                if (lineRows.m_reels.Contains(this))
                {
                    lineIndex = i;
                    break;
                }
            }

            for (int i = 0; i < m_icons.Length; i++)
            {
                int finalIconArrayIndex = (lineIndex + i) % m_finalIcons.Count;
                if (finalIconArrayIndex >= 0 && finalIconArrayIndex < m_finalIcons.Count)
                {
                    int symbolIndexInReel = m_finalIcons[finalIconArrayIndex];
                    if (symbolIndexInReel >= 0 && symbolIndexInReel < m_reel.Count)
                    {
                        int symbolValue = m_reel[symbolIndexInReel];
                        m_icons[i].m_spriteNumber = symbolValue;
                        m_icons[i].StopAnimation();
                        m_icons[i].Show(m_machine.m_reelIcons[symbolValue], symbolValue);
                    }
                }
            }
        }

        private void ApplyCascadeIcons()
        {
            if (m_finalIcons == null || m_finalIcons.Count == 0)
            {
                return;
            }

            int lineIndex = 0;
            for (int i = 0; i < WG_SlotMachine.Instance.lineRows.Count; i++)
            {
                WG_LineRows lineRows = WG_SlotMachine.Instance.lineRows[i];
                if (lineRows.m_reels.Contains(this))
                {
                    lineIndex = i;
                    break;
                }
            }

            for (int i = 0; i < m_icons.Length; i++)
            {
                int finalIconArrayIndex = (lineIndex + i) % m_finalIcons.Count;
                if (finalIconArrayIndex >= 0 && finalIconArrayIndex < m_finalIcons.Count)
                {
                    int symbolIndexInReel = m_finalIcons[finalIconArrayIndex];
                    if (symbolIndexInReel >= 0 && symbolIndexInReel < m_reel.Count)
                    {
                        int symbolValue = m_reel[symbolIndexInReel];

                        if (m_icons[i].m_spriteNumber != symbolValue)
                        {
                            m_icons[i].m_spriteNumber = symbolValue;
                            m_icons[i].Show(m_machine.m_reelIcons[symbolValue], symbolValue);

                            if (m_icons[i].text != null)
                            {
                                m_icons[i].text.text = symbolValue.ToString();
                            }
                        }
                    }
                }
            }
        }

        public void GetIcons()
        {
            if (m_IndexIcons.Count != 0)
            {
                m_finalIcons.Clear();
                for (int i = 0; i < m_IndexIcons.Count; i++)
                {
                    int index = FindIndexForValue(m_IndexIcons[i]);
                    if (index >= 0)
                        m_finalIcons.Add(index);
                    else
                        Debug.LogError($"Não encontrou índice para o símbolo {m_IndexIcons[i]}");
                }
            }
        }

        void Update()
        {
            if (onTest)
            {
                onTest = false;
                if (m_IndexIcons.Count != 0)
                {
                    for (int i = 0; i < m_IndexIcons.Count; i++)
                    {
                        m_finalIcons[i] = FindIndexForValue(m_IndexIcons[i]);
                    }
                }
                UpdateIcons();
                StopSpinning(false);
            }

            if (m_initialized)
                UpdateIcons();
        }

        public void UpdateIcons()
        {
            // VERIFICA O ESTADO DE SPIN A CADA FRAME E ATUALIZA OS ÍCONES
            bool currentSpinState = m_spinning || m_preSpinning || m_stopping || m_stoppingEnd;
            if (currentSpinState != m_lastSpinState)
            {
                m_lastSpinState = currentSpinState;
                UpdateAllIconsSpinState(currentSpinState);
            }

            switch (m_animationType)
            {
                case ReelAnimationType.IndependentRows:
                    UpdateNormalSpinning();
                    break;
                case ReelAnimationType.CascadeFall:
                    UpdateCascadeFall();
                    break;
            }
        }

        private void UpdateNormalSpinning()
        {
            if (m_initialDelayCurrent < m_initialDelay)
            {
                m_initialDelayCurrent += Time.deltaTime;
                return;
            }

            if (!m_preSpinning && !m_spinning && !m_stopping && !m_stoppingEnd && !m_preSpinPause)
            {
                for (int i = 0; i < m_icons.Length; i++)
                {
                    float _y = m_topPos - i * m_offset;
                    m_icons[i].SetPosY(_y);
                }
                return;
            }

            var machine = WG_SlotMachine.Instance;
            float dt = Time.deltaTime;
            float currentSpeed = 0f;

            if (m_preSpinning)
            {
                if (m_animationType == ReelAnimationType.IndependentRows)
                    m_icons[1].gameObject.SetActive(true);

                if (mask2D)
                    mask2D.enabled = WG_SlotMachine.Instance.onMask2D;

                float preSpinProgress = m_preSpinTimer / (machine.onSpeedMode ? m_preSpinDuration / 2f : m_preSpinDuration);
                float easedProgress = 1f - Mathf.Pow(1f - preSpinProgress, 2f);
                currentSpeed = Mathf.Lerp(0f, m_preSpinSpeed, easedProgress);
                m_preSpinTimer += dt;

                if (m_preSpinTimer >= (machine.onSpeedMode ? m_preSpinDuration / 2f : m_preSpinDuration))
                {
                    m_preSpinning = false;
                    m_preSpinPause = true;
                    if (m_animationType == ReelAnimationType.IndependentRows)
                        m_preSpinPauseTimer = 0f;
                    else
                        m_preSpinPauseTimer = m_preSpinPauseDuration;
                }
            }
            else if (m_preSpinPause)
            {
                currentSpeed = 0f;
                m_preSpinPauseTimer += dt;
                if (m_preSpinPauseTimer >= m_preSpinPauseDuration)
                {
                    m_preSpinPause = false;
                    m_spinning = true;
                }
            }
            else if (m_spinning)
            {
                currentSpeed = (machine.onSpeedMode ? m_speedTurbo : m_speed);
            }
            else if (m_stopping)
            {
                float stopProgress = m_endSpinTimer / (machine.onSpeedMode ? m_endSpinDuration / 2f : m_endSpinDuration);
                float easedStopProgress = 1f - Mathf.Pow(1f - stopProgress, 2f);
                currentSpeed = Mathf.Lerp((machine.onSpeedMode ? m_speedTurbo : m_speed), m_endSpinSpeed, easedStopProgress);
                m_endSpinTimer += dt;
                if (m_endSpinTimer >= (machine.onSpeedMode ? m_endSpinDuration / 2f : m_endSpinDuration))
                {
                    m_stopping = false;
                    m_stoppingEnd = true;
                    m_finalSpinTimer = 0f;
                }
            }
            else if (m_stoppingEnd)
            {
                currentSpeed = m_endSpinSpeed;
                m_finalSpinTimer += dt;
            }

            float frameMove = currentSpeed * dt * 60f;
            if (m_stoppingEnd)
                m_topPos += frameMove;
            else
                m_topPos -= frameMove;

            int reelCount = m_reel.Count;
            if (reelCount == 0) return;

            if (currentSpeed > 0)
            {
                while (m_topPos <= -m_offset)
                {
                    m_topPos += m_offset;
                    m_index = (m_index + 1) % reelCount;
                    if (m_spinning || m_preSpinning) UpdateAllSprites();
                }
                while (m_topPos > 0f)
                {
                    m_topPos -= m_offset;
                    m_index = (m_index - 1 + reelCount) % reelCount;
                    if (m_spinning || m_preSpinning) UpdateAllSprites();
                }
            }

            for (int i = 0; i < m_icons.Length; i++)
            {
                float _y = m_topPos - i * m_offset;
                m_icons[i].SetPosY(_y);
            }

            if (m_stoppingEnd && m_finalSpinTimer >= (machine.onSpeedMode ? m_endSpinDuration / 2f : m_endSpinDuration))
            {
                m_stoppingEnd = false;
                m_topPos = m_initialTopPos;
                for (int i = 0; i < m_icons.Length; i++)
                {
                    float _y = m_topPos - i * m_offset;
                    m_icons[i].SetPosY(_y);
                }

                int idxMachine = 0;
                foreach (WG_LineRows line in WG_SlotMachine.Instance.lineRows)
                {
                    if (line.m_reels.Contains(this))
                    {
                        idxMachine = WG_SlotMachine.Instance.lineRows.IndexOf(line);
                        break;
                    }
                }

                WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_slotStop[idxMachine]);
                if (onShakeSlot) Tween.ShakeScale(transform, strength: new Vector3(0.25f, 0.25f, 0.25f), duration: 0.5f);
                if (mask2D) mask2D.enabled = false;
            }
        }

        private void StartCascadeFall()
        {
            m_cascadeActive = true;
            m_cascadeReturning = false;
            m_cascadeTimer = 0f;
            m_cascadeWaitingStart = m_cascadeStartDelay > 0f;

            float startTopY = m_topPosition.y + m_cascadeStartTopOffset;
            for (int i = 0; i < m_icons.Length; i++)
            {
                float posY = startTopY - i * m_offset;
                m_icons[i].SetPosY(posY);
            }
        }

        private void UpdateCascadeFall()
        {
            if (!m_cascadeActive && m_cascadeCompleted) return;
            if (m_icons == null || m_icons.Length == 0) return;

            float dt = Time.deltaTime;
            m_cascadeTimer += dt;

            if (m_cascadeWaitingStart)
            {
                if (m_cascadeTimer >= m_cascadeStartDelay)
                {
                    m_cascadeWaitingStart = false;
                    m_cascadeTimer = 0f;
                }
                return;
            }

            float currentTopY = m_topPosition.y + m_cascadeStartTopOffset;
            float slot1Y = currentTopY - (1 * m_offset);
            float slot7Y = currentTopY - (7 * m_offset);

            if (!m_cascadeReturning)
            {
                bool allReachedBottom = true;

                for (int i = m_cascadeEndSlot; i >= m_cascadeStartSlot; i--)
                {
                    if (i < 0 || i >= m_icons.Length)
                    {
                        allReachedBottom = false;
                        continue;
                    }

                    float delay = (m_cascadeEndSlot - i) * m_cascadeDelayBetweenIcons;
                    if (m_cascadeTimer < delay)
                    {
                        allReachedBottom = false;
                        continue;
                    }

                    RectTransform rt = m_icons[i].GetComponent<RectTransform>();
                    float currentY = rt.anchoredPosition.y;

                    if (currentY > slot7Y)
                    {
                        currentY -= m_cascadeFallSpeedStart * dt;
                        if (currentY < slot7Y) currentY = slot7Y;
                        m_icons[i].SetPosY(currentY);
                        allReachedBottom = false;
                    }
                }

                if (allReachedBottom && m_cascadeTimer >= (m_cascadeEndSlot - m_cascadeStartSlot + 1) * m_cascadeDelayBetweenIcons)
                {
                    m_cascadeReturning = true;
                    m_cascadeTimer = 0f;
                }
            }
            else
            {
                if (m_cascadeTimer < 0.1f)
                {
                    float resetY = slot1Y + m_cascadeResetHeightOffset;
                    for (int i = m_cascadeStartSlot; i <= m_cascadeEndSlot; i++)
                    {
                        if (i < 0 || i >= m_icons.Length) continue;
                        m_icons[i].SetPosY(resetY);
                    }

                    ApplyCascadeIcons();
                }
                else
                {
                    bool allDone = true;
                    float fallSpeed = m_cascadeFallSpeedEnd * 1.5f;

                    for (int i = m_cascadeStartSlot; i <= m_cascadeEndSlot; i++)
                    {
                        if (i < 0 || i >= m_icons.Length) continue;

                        RectTransform rt = m_icons[i].GetComponent<RectTransform>();
                        float currentY = rt.anchoredPosition.y;
                        float targetY = currentTopY - i * m_offset;

                        if (Mathf.Abs(currentY - targetY) > 0.1f)
                        {
                            currentY -= fallSpeed * dt;
                            if (currentY < targetY) currentY = targetY;
                            m_icons[i].SetPosY(currentY);
                            allDone = false;
                        }
                    }

                    if (allDone)
                    {
                        ApplyCascadeIcons();

                        for (int i = 0; i < m_icons.Length; i++)
                        {
                            float finalY = currentTopY - i * m_offset;
                            m_icons[i].SetPosY(finalY);
                        }

                        m_cascadeActive = false;
                        m_cascadeReturning = false;
                        m_cascadeWaitingStart = false;
                        m_cascadeCompleted = true;
                        m_spinning = false;
                        m_cascadeTimer = 0f;

                        for (int i = 0; i < WG_SlotMachine.Instance.lineRows.Count; i++)
                        {
                            WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_slotStop[i]);
                        }

                        if (onShakeSlot) Tween.ShakeScale(transform, strength: new Vector3(0.25f, 0.25f, 0.25f), duration: 0.5f);
                    }
                }
            }
        }

        private void UpdateAllSprites()
        {
            int reelCount = m_reel.Count;
            if (reelCount == 0) return;

            for (int i = 0; i < m_icons.Length; i++)
            {
                int symbolIndex = (m_index + i) % reelCount;
                if (symbolIndex < 0) symbolIndex += reelCount;
                int symbolValue = m_reel[symbolIndex];

                var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
                var symbolData = m_symbols[symbolValue];

                if (symbolData.useSpineDefault && symbolData.defaultSpinePreview != null)
                {
                    m_icons[i].m_spriteNumber = symbolValue;
                    if (!m_spinning && !m_preSpinning)
                    {
                        if (m_machine != null && m_machine.m_reelIcons != null && m_machine.m_reelIcons.ContainsKey(symbolValue))
                        {
                            m_icons[i].Show(m_machine.m_reelIcons[symbolValue], symbolValue);
                        }
                    }
                }
                else
                {
                    Sprite sp = null;
                    if (m_spinning)
                    {
                        if (m_machine != null && m_machine.m_reelIconsMove != null && m_machine.m_reelIconsMove.ContainsKey(symbolValue))
                            sp = m_machine.m_reelIconsMove[symbolValue];
                    }
                    else
                    {
                        if (m_machine != null && m_machine.m_reelIcons != null && m_machine.m_reelIcons.ContainsKey(symbolValue))
                            sp = m_machine.m_reelIcons[symbolValue];
                    }

                    if (m_icons[i].m_renderer != null)
                        m_icons[i].m_renderer.sprite = sp;

                    m_icons[i].m_sprite = sp;
                    m_icons[i].m_spriteNumber = symbolValue;
                }
            }
        }

        public void Shuffle(List<int> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public int FindIndexForValue(int targetValue)
        {
            for (int i = 0; i < m_reel.Count; i++)
            {
                if (m_reel[i] == targetValue)
                    return i;
            }
            return -1;
        }

        public bool IsReelActive()
        {
            if (m_animationType == ReelAnimationType.CascadeFall)
            {
                return !m_cascadeCompleted && m_cascadeActive;
            }
            return m_spinning || m_preSpinning || m_stopping || m_stoppingEnd || m_preSpinPause;
        }

        public bool AreReelsCascadeCompleted()
        {
            if (m_animationType == ReelAnimationType.CascadeFall)
            {
                return m_cascadeCompleted;
            }
            return !m_spinning && !m_preSpinning && !m_stopping && !m_stoppingEnd && !m_preSpinPause;
        }
    }
}
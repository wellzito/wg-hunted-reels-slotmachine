// WG_SlotPaymentSystem.cs - VERSÃO FINAL CORRIGIDA

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WG_Casino.Setup;
using static WG_Casino.SlotMachine.WG_SlotMachine;
using WG_Casino.SlotMachine;
using UnityEngine.Events;
using System.Linq;
using SlotMachineMath;

namespace WG_Casino.Systems
{
    public class WG_SlotPaymentSystem : MonoBehaviour
    {
        public WG_PaymentSetup paymentConfig;
        public bool isWinningPatterns = true;

        private Coroutine currentPatternCoroutine;
        private bool stopPatternAnimation = false;


        [Header("Payment Callback System")]
        [SerializeField] private List<UnityEvent> m_onPaymentStartedCallbacks = new List<UnityEvent>();
        [SerializeField] private List<UnityEvent> m_onPaymentCompletedCallbacks = new List<UnityEvent>();
        [SerializeField] private List<UnityEvent> m_onSymbolHighlightedCallbacks = new List<UnityEvent>();
        [SerializeField] private List<UnityEvent> m_onPaymentAmountCallbacks = new List<UnityEvent>();

        [SerializeField] private bool m_isPaymentCallbackInProgress = false;
        [SerializeField] private float m_paymentCallbackCooldown = 0.1f;
        private Coroutine m_currentPaymentCallbackCoroutine;

        public System.Action OnPaymentStarted;
        public System.Action OnPaymentCompleted;
        public System.Action<int> OnSymbolHighlighted;
        public System.Action<int, int> OnPaymentAmount;

        public List<UnityEvent> OnPaymentStartedCallbacks => m_onPaymentStartedCallbacks;
        public List<UnityEvent> OnPaymentCompletedCallbacks => m_onPaymentCompletedCallbacks;
        public List<UnityEvent> OnSymbolHighlightedCallbacks => m_onSymbolHighlightedCallbacks;
        public List<UnityEvent> OnPaymentAmountCallbacks => m_onPaymentAmountCallbacks;
        public bool IsPaymentCallbackInProgress => m_isPaymentCallbackInProgress;

        public int symbolIdCurrent;

        public void ClearAllLineMarks(WG_LineRows line)
        {
            foreach (var reel in line.m_reels)
            {
                if (WG_SlotMachine.Instance.m_animationType == ReelAnimationType.IndependentRows)
                    reel.m_icons[1].gameObject.SetActive(false);
                foreach (var icon in reel.m_icons)
                {
                    icon.lineMark.SetActive(false);
                    icon.linePayment.gameObject.SetActive(false);
                    icon.Fade(false);
                    icon.CardGlow(false);
                    icon.isActive = false;
                }
            }
        }

        public void CheckWinningCombinationsAndShowLines(WG_LineRows line)
        {
            if (line == null)
            {
                PayoutAnimation.Instance.isPayout = false;
                Debug.LogError("[PRIZE CHECK] Line is null!");
                return;
            }

            StartPaymentStartedCallbacks();

            if (isWinningPatterns)
            {
                // MODO PADRÃO - USA OS PADRÕES DO SlotMathRunner
                StartCoroutine(ShowPatternsMode());
            }
            else
            {
                // MODO ORIGINAL - CÓDIGO EXATAMENTE IGUAL AO ORIGINAL
                var playResponse = SlotClient.Instance.playResponse;

                if (playResponse?.waysSummary == null || playResponse.waysSummary.total <= 0)
                {
                    PayoutAnimation.Instance.isPayout = false;
                    StartPaymentCompletedCallbacks();
                    return;
                }

                foreach (var l in WG_SlotMachine.Instance.lineRows)
                {
                    ClearAllLineMarks(l);
                }

                if (playResponse.waysSummary.perSymbol != null && playResponse.waysSummary.perSymbol.Length > 0)
                {
                    StartCoroutine(ShowSymbolsSequentiallyLoopOriginal(line, playResponse.waysSummary.perSymbol));
                }
                else
                {
                    StartPaymentCompletedCallbacks();
                }
            }
        }

        public void StopCurrentPatternAnimation()
        {
            stopPatternAnimation = true;

            if (currentPatternCoroutine != null)
            {
                StopCoroutine(currentPatternCoroutine);
                currentPatternCoroutine = null;
            }

            // Limpar todos os marks
            foreach (var l in WG_SlotMachine.Instance.lineRows)
            {
                ClearAllLineMarks(l);
            }

            // Resetar flags
            if (PayoutAnimation.Instance != null)
                PayoutAnimation.Instance.isPayout = false;
        }

        // ========== MÉTODO PARA MOSTRAR APENAS O PADRÃO ATUAL EXTERNO ==========
        public void ShowCurrentPatternAnimation()
        {
            StartCoroutine(ShowCurrentPatternAnimationRoutine());   
        }

        IEnumerator ShowCurrentPatternAnimationRoutine()
        {
            StopCurrentPatternAnimation();
            yield return new WaitForSeconds(0.1f);

            foreach (var line in WG_SlotMachine.Instance.lineRows)
            {
                CheckWinningCombinationsAndShowLines(line);
                line.EnableMask(false);
            }
        }

        // ========== MODO PADRÃO COM LOOP INFINITO (IGUAL AO ORIGINAL) ==========
        private IEnumerator ShowPatternsMode()
        {
            stopPatternAnimation = false;
            currentPatternCoroutine = StartCoroutine(ShowPatternsModeCoroutine());
            yield return currentPatternCoroutine;
        }

        private IEnumerator ShowPatternsModeCoroutine()
        {
            yield return new WaitUntil(() => WG_SlotMachine.Instance.AreAllReelsCompleted());

            var winningPatterns = SlotMathRunner.Instance.GetLastWinningPatterns();

            Debug.Log($"[MODO PADRÃO] Total de padrões: {winningPatterns.Count}");

            if (winningPatterns == null || winningPatterns.Count == 0)
            {
                StartPaymentCompletedCallbacks();
                yield break;
            }

            int index = 0;
            HashSet<int> playedSoundPatterns = new HashSet<int>();

            while (WG_SlotMachine.Instance.AreAllReelsCompleted() && !stopPatternAnimation && winningPatterns.Count > 0)
            {
                if (stopPatternAnimation) break;

                if (index >= winningPatterns.Count)
                {
                    index = 0;
                }

                if (index < 0 || index >= winningPatterns.Count)
                {
                    Debug.LogError($"Índice inválido: {index}, total: {winningPatterns.Count}");
                    yield break;
                }

                var pattern = winningPatterns[index];

                foreach (var l in WG_SlotMachine.Instance.lineRows)
                {
                    ClearAllLineMarks(l);
                }

                if (!playedSoundPatterns.Contains(pattern.patternId))
                {
                    WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_line);
                    playedSoundPatterns.Add(pattern.patternId);
                    TriggerPaymentAmount((int)(pattern.payout * 100), pattern.symbolId);
                }

                TriggerSymbolHighlighted(pattern.symbolId);
                MarkPatternUsingOriginalMethod(pattern);

                //Debug.Log($"[PADRÃO] {pattern.patternName} - {pattern.symbolName} - Pagamento: {pattern.payout:F2}");

                // Usar WaitForSecondsRealtime para não ser afetado por timeScale
                float waitTime = 0;
                while (waitTime < 3f && !stopPatternAnimation)
                {
                    waitTime += Time.deltaTime;
                    yield return null;
                }

                index++;
            }

            foreach (var l in WG_SlotMachine.Instance.lineRows)
            {
                ClearAllLineMarks(l);
            }

            StartPaymentCompletedCallbacks();
            currentPatternCoroutine = null;
        }

        // MÉTODO PARA MARCAR APENAS AS POSIÇÕES DO PADRÃO
        // WG_SlotPaymentSystem.cs - Modifique o método MarkPatternUsingOriginalMethod

        private void MarkPatternUsingOriginalMethod(WinningPatternInfo pattern)
        {
            for (int rowIdx = 0; rowIdx < WG_SlotMachine.Instance.lineRows.Count; rowIdx++)
            {
                var line = WG_SlotMachine.Instance.lineRows[rowIdx];

                for (int colIdx = 0; colIdx < line.m_reels.Count; colIdx++)
                {
                    var reel = line.m_reels[colIdx];

                    for (int iconIdx = 0; iconIdx < reel.m_icons.Length; iconIdx++)
                    {
                        var icon = reel.m_icons[iconIdx];

                        bool isInPattern = false;
                        foreach (var pos in pattern.positions)
                        {
                            // IMPORTANTE: row = linha, column = coluna (reel)
                            if (pos.row == rowIdx && pos.column == colIdx)
                            {
                                isInPattern = true;
                                break;
                            }
                        }

                        if (isInPattern)
                        {
                            icon.ShowLineMark();
                            icon.Fade(false);
                            icon.CardGlow(true);
                            icon.isActive = true;
                            symbolIdCurrent = pattern.symbolId;
                        }
                    }
                }
            }
        }

        // ========== MODO ORIGINAL - EXATAMENTE IGUAL ==========
        private IEnumerator ShowSymbolsSequentiallyLoopOriginal(WG_LineRows line, WaysSymbolSummaryDto[] symbols)
        {
            if (symbols == null || symbols.Length == 0)
            {
                StartPaymentCompletedCallbacks();
                yield break;
            }

            int index = 0;
            HashSet<int> playedSoundSymbols = new HashSet<int>();

            yield return new WaitUntil(() => WG_SlotMachine.Instance.AreAllReelsCompleted());

            while (WG_SlotMachine.Instance.AreAllReelsCompleted())
            {
                var symbolSummary = symbols[index];

                if (symbolSummary.symbolId > 0 &&
                    (symbolSummary.ways3 > 0 || symbolSummary.ways4 > 0 || symbolSummary.ways5 > 0))
                {
                    ClearAllLineMarks(line);

                    if (!playedSoundSymbols.Contains(symbolSummary.symbolId))
                    {
                        WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_line);
                        playedSoundSymbols.Add(symbolSummary.symbolId);
                        TriggerPaymentAmount(symbolSummary.payout, symbolSummary.symbolId);
                    }

                    TriggerSymbolHighlighted(symbolSummary.symbolId);

                    int waysCount = 0;
                    if (symbolSummary.ways5 > 0) waysCount = 5;
                    else if (symbolSummary.ways4 > 0) waysCount = 4;
                    else if (symbolSummary.ways3 > 0) waysCount = 3;

                    for (int reelIndex = 0; reelIndex < waysCount && reelIndex < line.m_reels.Count; reelIndex++)
                    {
                        var reel = line.m_reels[reelIndex];
                        foreach (var icon in reel.m_icons)
                        {
                            icon.lineMark.SetActive(false);
                            icon.linePayment.gameObject.SetActive(false);
                            icon.Fade(false);
                            icon.CardGlow(false);
                            icon.isActive = false;
                            icon.StopAnimation();
                        }
                        foreach (var icon in reel.m_icons)
                        {
                            if (icon.m_spriteNumber == symbolSummary.symbolId)
                            {
                                symbolIdCurrent = symbolSummary.symbolId;
                                icon.ShowLineMark();
                                icon.Fade(false);
                                icon.CardGlow(true);
                                icon.isActive = true;
                                break;
                            }
                        }
                    }

                    for (int reelIndex = waysCount; reelIndex < line.m_reels.Count; reelIndex++)
                    {
                        var reel = line.m_reels[reelIndex];
                        foreach (var icon in reel.m_icons)
                        {
                            icon.lineMark.SetActive(false);
                            icon.linePayment.gameObject.SetActive(false);
                            icon.Fade(false);
                            icon.CardGlow(false);
                            icon.isActive = false;
                        }
                    }
                }

                yield return new WaitForSeconds(3f);
                index = (index + 1) % symbols.Length;
            }

            ClearAllLineMarks(line);
            StartPaymentCompletedCallbacks();
        }

        private void TriggerPaymentAmount(int payoutAmount, int symbolId)
        {
            OnPaymentAmount?.Invoke(payoutAmount, symbolId);
            foreach (var callbackEvent in m_onPaymentAmountCallbacks)
            {
                if (callbackEvent != null) callbackEvent.Invoke();
            }
        }

        private void StartPaymentStartedCallbacks()
        {
            OnPaymentStarted?.Invoke();
            foreach (var callbackEvent in m_onPaymentStartedCallbacks)
            {
                if (callbackEvent != null) callbackEvent.Invoke();
            }
        }

        private void StartPaymentCompletedCallbacks()
        {
            if (m_isPaymentCallbackInProgress) return;
            if (m_currentPaymentCallbackCoroutine != null) StopCoroutine(m_currentPaymentCallbackCoroutine);
            m_currentPaymentCallbackCoroutine = StartCoroutine(ExecutePaymentCompletedCallbacks());
        }

        private IEnumerator ExecutePaymentCompletedCallbacks()
        {
            m_isPaymentCallbackInProgress = true;
            OnPaymentCompleted?.Invoke();
            foreach (var callbackEvent in m_onPaymentCompletedCallbacks)
            {
                if (callbackEvent != null)
                {
                    callbackEvent.Invoke();
                    yield return new WaitForSeconds(m_paymentCallbackCooldown);
                }
            }
            yield return new WaitForEndOfFrame();
            m_isPaymentCallbackInProgress = false;
            m_currentPaymentCallbackCoroutine = null;
        }

        private void TriggerSymbolHighlighted(int symbolId)
        {
            OnSymbolHighlighted?.Invoke(symbolId);
            foreach (var callbackEvent in m_onSymbolHighlightedCallbacks)
            {
                if (callbackEvent != null) callbackEvent.Invoke();
            }
        }

        public void AddPaymentStartedCallback(System.Action callback)
        {
            if (callback != null) OnPaymentStarted += callback;
        }

        public void RemovePaymentStartedCallback(System.Action callback)
        {
            if (callback != null) OnPaymentStarted -= callback;
        }

        public void AddPaymentCompletedCallback(System.Action callback)
        {
            if (callback != null) OnPaymentCompleted += callback;
        }

        public void RemovePaymentCompletedCallback(System.Action callback)
        {
            if (callback != null) OnPaymentCompleted -= callback;
        }

        public void AddSymbolHighlightedCallback(System.Action<int> callback)
        {
            if (callback != null) OnSymbolHighlighted += callback;
        }

        public void RemoveSymbolHighlightedCallback(System.Action<int> callback)
        {
            if (callback != null) OnSymbolHighlighted -= callback;
        }

        public void AddUnityEventPaymentStartedCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null && !m_onPaymentStartedCallbacks.Contains(callbackEvent))
                m_onPaymentStartedCallbacks.Add(callbackEvent);
        }

        public void RemoveUnityEventPaymentStartedCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null) m_onPaymentStartedCallbacks.Remove(callbackEvent);
        }

        public void AddUnityEventPaymentCompletedCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null && !m_onPaymentCompletedCallbacks.Contains(callbackEvent))
                m_onPaymentCompletedCallbacks.Add(callbackEvent);
        }

        public void RemoveUnityEventPaymentCompletedCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null) m_onPaymentCompletedCallbacks.Remove(callbackEvent);
        }

        public void AddUnityEventSymbolHighlightedCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null && !m_onSymbolHighlightedCallbacks.Contains(callbackEvent))
                m_onSymbolHighlightedCallbacks.Add(callbackEvent);
        }

        public void RemoveUnityEventSymbolHighlightedCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null) m_onSymbolHighlightedCallbacks.Remove(callbackEvent);
        }

        public void ClearAllPaymentCallbacks()
        {
            OnPaymentStarted = null;
            OnPaymentCompleted = null;
            OnSymbolHighlighted = null;
            m_onPaymentStartedCallbacks.Clear();
            m_onPaymentCompletedCallbacks.Clear();
            m_onSymbolHighlightedCallbacks.Clear();
        }

        public bool PaymentCallbackInProgress()
        {
            return m_isPaymentCallbackInProgress;
        }

        #region Paymount Line
        public void CheckPayLine(WG_LineRows line)
        {
            var paymentPatterns = WG_SlotMachine.Instance.gameSetup.paymentPatterns;
            paymentPatterns.Clear();
            paymentPatterns = FindMatchingPaymentPatterns(line);
        }

        public bool CheckPayment(PaymentPattern LinePay)
        {
            List<PreviewsReels> previewsReels = WG_SlotMachine.Instance.previewsReels;
            var paymentPatterns = WG_SlotMachine.Instance.gameSetup.paymentPatterns;

            if (paymentPatterns.Count != 0) return true;

            bool isWinningLine = true;
            int firstSymbol = -1;
            bool hasWild = false;

            for (int i = 0; i < LinePay.selectedSlots.Count; i++)
            {
                var slotInfo = LinePay.selectedSlots[i];
                int currentSymbol = -1;

                switch (slotInfo.rowIndex)
                {
                    case 3: currentSymbol = previewsReels[slotInfo.reelIndex].slots[0]; break;
                    case 4: currentSymbol = previewsReels[slotInfo.reelIndex].slots[1]; break;
                    case 5: currentSymbol = previewsReels[slotInfo.reelIndex].slots[2]; break;
                    case 6: currentSymbol = previewsReels[slotInfo.reelIndex].slots[3]; break;
                    case 7: currentSymbol = previewsReels[slotInfo.reelIndex].slots[4]; break;
                }

                if (currentSymbol == 11) { hasWild = true; continue; }
                if (firstSymbol == -1) { firstSymbol = currentSymbol; continue; }
                if (currentSymbol != firstSymbol) { isWinningLine = false; break; }
            }

            return (firstSymbol != -1) || (hasWild);
        }

        public List<PaymentPattern> FindMatchingPaymentPatterns(WG_LineRows line)
        {
            List<PaymentPattern> matchedPatterns = new List<PaymentPattern>();
            var allPatterns = paymentConfig.paymentPatterns;

            foreach (var pattern in allPatterns)
            {
                if (AreAllSelectedSpriteNumbersEqual(pattern, line))
                {
                    matchedPatterns.Add(pattern);
                }
            }

            return matchedPatterns;
        }

        bool AreAllSelectedSpriteNumbersEqual(PaymentPattern payment, WG_LineRows line)
        {
            var symbols = WG_SlotMachine.Instance.gameSetup.symbols;
            var _selectedSlots = payment.selectedSlots;

            if (_selectedSlots == null || _selectedSlots.Count == 0) return false;

            int? firstSpriteNumber = null;
            bool hasWild = false;

            foreach (var slot in _selectedSlots)
            {
                if (slot.reelIndex < 0 || slot.reelIndex >= line.m_reels.Count) return false;
                var reel = line.m_reels[slot.reelIndex];
                if (slot.rowIndex < 0 || slot.rowIndex >= reel.m_icons.Length) return false;

                int currentSpriteNumber = reel.m_icons[slot.rowIndex].m_spriteNumber;
                bool isWild = symbols.Exists(s => s.special == WG_SlotSetup.SymbolSpecialIndex.wild && s.indexSymbol == currentSpriteNumber);

                if (isWild) { hasWild = true; continue; }
                if (firstSpriteNumber == null) { firstSpriteNumber = currentSpriteNumber; }
                else if (firstSpriteNumber != currentSpriteNumber) return false;
            }

            return (firstSpriteNumber != null) || hasWild;
        }
        #endregion
    }
}
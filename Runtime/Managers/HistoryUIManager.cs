// HistoryUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using WG_Casino.SlotMachine;
using SlotMachineMath;
using WG_Casino.Setup;
using WG_Casino;
using WG_Casino.Systems;

public class HistoryUIManager : MonoBehaviour
{
    public static HistoryUIManager Instance;

    [Header("History UI References")]
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private Transform historyContentParent;
    [SerializeField] private GameObject historyItemPrefab;
    [SerializeField] private Button closeReplayButton;
    [SerializeField] private GameObject panelReplay;

    [Header("Speed Control")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button fastForwardButton;
    private bool isPaused = false;
    public float timeNextMax = 0.01f;
    private Coroutine fastForwardCoroutine;

    [Header("History Settings")]
    [SerializeField] private int maxHistoryItems = 50;

    private List<GameObject> historyItemInstances = new List<GameObject>();
    private bool isHistoryInitialized = false;
    private int historyIndexCur;
    private int historyIndexBonusCur;

    // ============ Dados salvos para replay ============
    [System.Serializable]
    public class ReelData
    {
        public int reelIndex;
        public List<int> indexIcons = new List<int>();
        public List<int> finalIcons = new List<int>();
        public List<int> actualSymbols = new List<int>();
    }

    private int savedCredits;
    private int savedBet;
    private int savedGain;
    private List<ReelData> savedReels = new List<ReelData>();
    private bool isReplayMode = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializeHistoryUI();
        InitializeSpeedControls();

        GameEvents.OnReplayStarting += SaveCurrentGameState;
        GameEvents.OnReplayEnding += RestoreSavedGameState;
    }

    private void OnDestroy()
    {
        var slotClient = SlotClient.Instance;

        if (slotClient != null)
        {
            slotClient.OnNewHistoryAdded -= OnNewHistoryAdded;
            slotClient.OnHistoryUpdated -= RefreshHistoryUI;
        }

        GameEvents.OnReplayStarting -= SaveCurrentGameState;
        GameEvents.OnReplayEnding -= RestoreSavedGameState;

        Time.timeScale = 1f;
        if (fastForwardCoroutine != null)
            StopCoroutine(fastForwardCoroutine);
    }

    void InitializeHistoryUI()
    {
        if (isHistoryInitialized) return;

        closeReplayButton.onClick.AddListener(() =>
        {
            panelReplay.SetActive(false);
            ExitReplayMode();

            StopAllCoroutines();
            if (fastForwardCoroutine != null)
                StopCoroutine(fastForwardCoroutine);

            fastForwardCoroutine = StartCoroutine(FastForwardRoutine(100f));
        });

        SlotClient.Instance.OnNewHistoryAdded += OnNewHistoryAdded;
        SlotClient.Instance.OnHistoryUpdated += RefreshHistoryUI;

        if (historyPanel != null)
            historyPanel.SetActive(false);

        isHistoryInitialized = true;
    }

    void InitializeSpeedControls()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (fastForwardButton != null)
            fastForwardButton.onClick.AddListener(ActivateFastForward);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log(isPaused ? "[PAUSE] Game PAUSED" : "[PAUSE] Game RESUMED");
    }

    public void ActivateFastForward()
    {
        if (fastForwardCoroutine != null)
            StopCoroutine(fastForwardCoroutine);

        fastForwardCoroutine = StartCoroutine(FastForwardRoutine());
    }

    private IEnumerator FastForwardRoutine(float time = 10)
    {
        if (isPaused)
        {
            isPaused = false;
        }

        Time.timeScale = time;
        Debug.Log("[SPEED] Fast Forward activated");

        yield return new WaitForSecondsRealtime(timeNextMax);

        Time.timeScale = 1f;
        Debug.Log("[SPEED] Back to Normal Speed");

        fastForwardCoroutine = null;
        ExitReplayMode();
    }

    public void OpenHistoryPanel()
    {
        if (historyPanel != null)
        {
            historyPanel.SetActive(true);
            RefreshHistoryUI();
        }
    }

    public void CloseHistoryPanel()
    {
        if (historyPanel != null)
        {
            historyPanel.SetActive(false);
        }
    }

    private void OnNewHistoryAdded(SerializableLastPlay newPlay)
    {
        AddHistoryItemToUI(newPlay, 0);
        LimitUIHistorySize();
    }

    public void RefreshHistoryUI()
    {
        ClearHistoryUI();
        var slotClient = SlotClient.Instance;

        if (slotClient == null || slotClient.GameHistory == null)
            return;

        for (int i = 0; i < slotClient.GameHistory.Count; i++)
        {
            var historyItem = slotClient.GameHistory[i];
            AddHistoryItemToUI(historyItem, i);
        }
    }

    private void AddHistoryItemToUI(SerializableLastPlay historyItem, int index)
    {
        if (historyItemPrefab == null || historyContentParent == null)
            return;

        GameObject historyItemGO = Instantiate(historyItemPrefab, historyContentParent);

        if (index == 0)
            historyItemGO.transform.SetAsFirstSibling();

        HistoryItemUI itemUI = historyItemGO.GetComponent<HistoryItemUI>();
        if (itemUI != null)
        {
            itemUI.Initialize(historyItem, index);
        }
        else
        {
            ConfigureHistoryItemManually(historyItemGO, historyItem, index);
        }

        historyItemInstances.Insert(index, historyItemGO);
    }

    private void ConfigureHistoryItemManually(GameObject itemGO, SerializableLastPlay historyItem, int index)
    {
        TextMeshProUGUI[] texts = itemGO.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (var text in texts)
        {
            if (text.name.Contains("MatchID") || text.gameObject.CompareTag("MatchID"))
                text.text = $"ID: {historyItem.matchid}";
            else if (text.name.Contains("Date") || text.gameObject.CompareTag("Date"))
                text.text = historyItem.date;
            else if (text.name.Contains("Bet") || text.gameObject.CompareTag("Bet"))
                text.text = $"Bet: {GameUtils.FormatCurrency(historyItem.playItem.betValue)}";
            else if (text.name.Contains("Reward") || text.gameObject.CompareTag("Reward"))
                text.text = $"Win: {GameUtils.FormatCurrency(historyItem.playItem.rewards)}";
            else if (text.name.Contains("Balance") || text.gameObject.CompareTag("Balance"))
                text.text = $"Balance: {GameUtils.FormatCurrency(historyItem.afterBet)}";
        }

        Button replayButton = itemGO.GetComponentInChildren<Button>();
        if (replayButton != null)
        {
            int currentIndex = index;
            replayButton.onClick.AddListener(() => OnHistoryItemClicked(currentIndex));
        }

        GameObject bonusIndicator = itemGO.transform.Find("BonusIndicator")?.gameObject;
        if (bonusIndicator != null)
        {
            bonusIndicator.SetActive(historyItem.playItem.hasBonusSpins);
        }
    }

    private void OnHistoryItemClicked(int historyIndex)
    {
        Debug.Log($"[HISTORY UI] History item {historyIndex} clicked");
        ExecuteHistoryReplay(historyIndex);
        CloseHistoryPanel();
    }

    private void ClearHistoryUI()
    {
        foreach (var item in historyItemInstances)
        {
            if (item != null)
                Destroy(item);
        }
        historyItemInstances.Clear();
    }

    private void LimitUIHistorySize()
    {
        while (historyItemInstances.Count > maxHistoryItems)
        {
            GameObject oldestItem = historyItemInstances[historyItemInstances.Count - 1];
            if (oldestItem != null)
                Destroy(oldestItem);
            historyItemInstances.RemoveAt(historyItemInstances.Count - 1);
        }
    }

    #region Replay System

    private void SaveCurrentGameState()
    {
        var gm = GameManager.Instance;

        savedCredits = gm.Credits;
        savedBet = gm.Bet;
        savedGain = gm.Gain;
        savedReels.Clear();

        GameEvents.SavePlayerState(savedCredits, savedBet, savedGain);

        if (WG_SlotMachine.Instance != null && WG_SlotMachine.Instance.lineRows != null)
        {
            foreach (var line in WG_SlotMachine.Instance.lineRows)
            {
                foreach (var reel in line.m_reels)
                {
                    var reelData = new ReelData
                    {
                        reelIndex = reel.m_index,
                        indexIcons = new List<int>(reel.m_IndexIcons),
                        finalIcons = new List<int>(reel.m_finalIcons),
                        actualSymbols = new List<int>()
                    };

                    foreach (var icon in reel.m_icons)
                    {
                        reelData.actualSymbols.Add(icon != null ? icon.m_spriteNumber : -1);
                    }

                    savedReels.Add(reelData);
                }
            }
        }

        /*if (SlotMathRunner.Instance != null)
        {
            SlotMathRunner.Instance.SaveLastWinningPatterns();
        }*/

        Debug.Log($"[HISTORY] Estado salvo: C={savedCredits}, B={savedBet}, Reels={savedReels.Count}");
    }

    private void RestoreSavedGameState()
    {
        Debug.Log($"[HISTORY] Iniciando restauração do estado");

        GameEvents.RestorePlayerState(savedCredits, savedBet, savedGain);

        if (SlotMathRunner.Instance != null)
        {
            SlotMathRunner.Instance.ReturnOldWin();
        }

        if (SlotClient.Instance != null && SlotClient.Instance.playResponse != null)
        {
            SlotClient.Instance.playResponse.waysSummary.perSymbol = null;
        }

        if (savedReels.Count > 0 && WG_SlotMachine.Instance != null && WG_SlotMachine.Instance.lineRows != null)
        {
            int i = 0;
            foreach (var line in WG_SlotMachine.Instance.lineRows)
            {
                if (WG_SlotMachine.Instance.PaymentSystem != null)
                {
                    WG_SlotMachine.Instance.PaymentSystem.ClearAllLineMarks(line);
                }

                foreach (var reel in line.m_reels)
                {
                    if (i < savedReels.Count)
                    {
                        RestoreReelUsingExistingMethods(reel, savedReels[i]);
                        i++;
                    }
                }
            }
        }

        // Forçar atualização visual da slot machine
        if (WG_SlotMachine.Instance != null)
        {
            WG_SlotMachine.Instance.ClearAnimations();
            WG_SlotMachine.Instance.PaymentSystem.ShowCurrentPatternAnimation();
        }

        Debug.Log($"[HISTORY] Estado restaurado completo!");
    }

    /// <summary>
    /// Restaura o reel usando os métodos existentes do WG_SlotMachineReel e WG_SlotMachineIcon
    /// </summary>
    private void RestoreReelUsingExistingMethods(WG_SlotMachineReel reel, ReelData savedReel)
    {
        if (reel == null) return;

        // Restaurar os dados do reel
        reel.m_index = savedReel.reelIndex;
        reel.m_IndexIcons = new List<int>(savedReel.indexIcons);
        reel.m_finalIcons = new List<int>(savedReel.finalIcons);

        // Forçar o reel a reaplicar os ícones usando o método existente GetIcons()
        if (reel.m_IndexIcons.Count > 0)
        {
            reel.GetIcons();
            reel.UpdateIcons();
            reel.onTest = true;
        }

        // Agora restaurar cada ícone individualmente usando o método Show() do WG_SlotMachineIcon
        var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
        var machine = WG_SlotMachine.Instance;

        for (int iconIndex = 0; iconIndex < reel.m_icons.Length && iconIndex < savedReel.actualSymbols.Count; iconIndex++)
        {
            var icon = reel.m_icons[iconIndex];
            if (icon == null) continue;

            int symbolValue = savedReel.actualSymbols[iconIndex];
            if (symbolValue < 0 || symbolValue >= m_symbols.Count) continue;

            // Usar o método Show() existente que já lida com Spines e Sprites corretamente
            Sprite sprite = null;
            if (machine != null && machine.m_reelIcons != null && machine.m_reelIcons.ContainsKey(symbolValue))
            {
                sprite = machine.m_reelIcons[symbolValue];
            }
            else if (m_symbols[symbolValue].defaultSprite != null)
            {
                sprite = m_symbols[symbolValue].defaultSprite;
            }

            if (sprite != null)
            {
                // O método Show já lida com Spine vs Sprite automaticamente
                icon.Show(sprite, symbolValue);
            }

            // Limpar marks e glows
            if (icon.lineMark != null)
                icon.lineMark.SetActive(false);
            if (icon.linePayment != null)
                icon.linePayment.gameObject.SetActive(false);
            icon.Fade(false);
            icon.CardGlow(false);
            icon.isActive = false;
        }
    }

    public void PrepareForReplay()
    {
        GameEvents.TriggerReplayStarting();
        SaveCurrentGameState();
        isReplayMode = true;
        Debug.Log("[HISTORY] Estado salvo para replay");
    }

    public void ExitReplayMode()
    {
        if (!isReplayMode) return;

        GameEvents.TriggerReplayEnding();
        RestoreSavedGameState();

        if (WG_SlotMachine.Instance != null)
        {
            WG_SlotMachine.Instance.ClearAnimations();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.onReplay = false;
        }

        if (WG_AudioManager.Instance != null)
        {
            WG_AudioManager.Instance.Play("tapbutton");
        }

        isReplayMode = false;

        Debug.Log("[HISTORY] Modo replay encerrado, jogo restaurado");
    }

    #endregion

    #region Replay Execution

    public PlayResponse ConvertHistoryToPlayResponse(SerializableLastPlay historyItem)
    {
        return new PlayResponse
        {
            rewards = historyItem.playItem.rewards,
            reel = historyItem.playItem.reel,
            gameState = (GameState)historyItem.playItem.gameState,
            waysSummary = historyItem.playItem.hasWays ? historyItem.playItem.ways : new WaysSummaryDto(),
            playerBalanceWithoutRewards = historyItem.beforeBet,
            matchId = historyItem.matchid
        };
    }

    public BonusResponse ConvertHistoryToBonusResponse(SerializableLastPlay historyItem, int bonusSpinIndex = 0)
    {
        if (!historyItem.playItem.hasBonusSpins || historyItem.playItem.bonusSpins.Count == 0)
            return null;

        if (bonusSpinIndex >= historyItem.playItem.bonusSpins.Count)
            bonusSpinIndex = historyItem.playItem.bonusSpins.Count - 1;

        var bonusSpin = historyItem.playItem.bonusSpins[bonusSpinIndex];

        return new BonusResponse
        {
            rewards = bonusSpin.rewards,
            gameState = GameState.Bonus,
            freespinsRemaining = bonusSpin.freespinsRemaining,
            bonusOver = bonusSpin.bonusOver,
            bonusMultipliers = bonusSpin.bonusMultipliers,
            bonusReel = ConvertGridToReelDto(bonusSpin.bonusGrid),
            bonusTentacles = bonusSpin.bonusTentacles,
            playerBalanceWithoutRewards = historyItem.beforeBet,
            matchId = historyItem.matchid
        };
    }

    private ReelDto ConvertGridToReelDto(List<List<byte>> grid)
    {
        var reelDto = new ReelDto();
        if (grid == null || grid.Count == 0)
            return reelDto;

        var rows = new Row[grid.Count];
        for (int i = 0; i < grid.Count; i++)
        {
            rows[i] = new Row { elements = grid[i].ToArray() };
        }

        reelDto.rows = rows;
        return reelDto;
    }

    public void ExecuteHistoryReplay(int historyIndex)
    {
        var slotClient = SlotClient.Instance;

        PrepareForReplay();

        if (GameManager.Instance != null)
            GameManager.Instance.onReplay = true;

        historyIndexCur = historyIndex;
        historyIndexBonusCur = 0;

        if (historyIndex < 0 || historyIndex >= slotClient.GameHistory.Count)
        {
            Debug.LogError($"[HISTORY] Invalid history index: {historyIndex}");
            return;
        }
        StartCoroutine(DelayedReplay(historyIndex));
    }

    /// <summary>
    /// Cria uma cópia profunda de SerializableLastPlay usando JSON serialization
    /// </summary>
    private SerializableLastPlay DeepCopySerializableLastPlay(SerializableLastPlay original)
    {
        if (original == null) return null;

        string json = JsonUtility.ToJson(original);
        return JsonUtility.FromJson<SerializableLastPlay>(json);
    }

    IEnumerator DelayedReplay(int historyIndex)
    {
        var slotClient = SlotClient.Instance;

        panelReplay.SetActive(true);

        CloseHistoryPanel();
        if (PopUpManager.instance != null)
            PopUpManager.instance.OpenSettingsPanel(false);

        // ⭐ CRIAR UMA CÓPIA PROFUNDA do historyItem ANTES de usar
        var originalHistoryItem = slotClient.GameHistory[historyIndex];
        var historyItem = DeepCopySerializableLastPlay(originalHistoryItem);

        Debug.Log($"[HISTORY] Executing replay for MatchID: {historyItem.matchid}");

        var playResponse = ConvertHistoryToPlayResponse(historyItem);

        // Salvar os dados dos ícones ANTES de chamar PlayResponse
        var savedIconsData = CaptureCurrentIconsState();

        slotClient.PlayResponse(playResponse);

        // Usar os padrões da CÓPIA, não do original
        SlotMathRunner.Instance.SetLastWinningPatterns = historyItem.playItem.lastWinningPatterns;

        yield return new WaitUntil(() => WG_SlotMachine.Instance.AreAllReelsCompleted());

        // Restaurar os ícones para os valores corretos
        RestoreIconsFromSavedData(savedIconsData, historyItem);

        yield return new WaitForSeconds(0.5f);

        if (historyItem.playItem.hasBonusSpins)
        {
            var bonusResponse = ConvertHistoryToBonusResponse(historyItem, 0);
            if (bonusResponse != null)
            {
                historyIndexBonusCur++;
            }
        }
    }

    /// <summary>
    /// Captura o estado atual dos ícones antes do replay
    /// </summary>
    private List<List<int>> CaptureCurrentIconsState()
    {
        var state = new List<List<int>>();

        if (WG_SlotMachine.Instance == null || WG_SlotMachine.Instance.lineRows == null)
            return state;

        foreach (var line in WG_SlotMachine.Instance.lineRows)
        {
            foreach (var reel in line.m_reels)
            {
                var reelState = new List<int>();
                foreach (var icon in reel.m_icons)
                {
                    if (icon != null)
                    {
                        reelState.Add(icon.m_spriteNumber);
                    }
                    else
                    {
                        reelState.Add(-1);
                    }
                }
                state.Add(reelState);
            }
        }

        return state;
    }

    /// <summary>
    /// Restaura os ícones dos dados salvos do histórico
    /// </summary>
    private void RestoreIconsFromSavedData(List<List<int>> savedIconsState, SerializableLastPlay historyItem)
    {
        if (WG_SlotMachine.Instance == null || WG_SlotMachine.Instance.lineRows == null)
            return;

        var m_symbols = WG_SlotMachine.Instance.gameSetup.m_symbols;
        var machine = WG_SlotMachine.Instance;

        int reelIndex = 0;
        foreach (var line in WG_SlotMachine.Instance.lineRows)
        {
            foreach (var reel in line.m_reels)
            {
                if (reelIndex < savedIconsState.Count)
                {
                    var savedReelState = savedIconsState[reelIndex];

                    for (int iconPos = 0; iconPos < reel.m_icons.Length && iconPos < savedReelState.Count; iconPos++)
                    {
                        var icon = reel.m_icons[iconPos];
                        if (icon == null) continue;

                        int targetSymbol = savedReelState[iconPos];
                        if (targetSymbol < 0) continue;

                        // Forçar a recriação do ícone com o símbolo correto
                        ForceIconToSymbol(icon, targetSymbol, m_symbols, machine);
                    }
                }
                reelIndex++;
            }
        }

        // Forçar atualização visual
        foreach (var line in WG_SlotMachine.Instance.lineRows)
        {
            foreach (var reel in line.m_reels)
            {
                reel.UpdateIcons();
            }
        }
    }

    /// <summary>
    /// Força um ícone a mostrar um símbolo específico, recriando spine se necessário
    /// </summary>
    private void ForceIconToSymbol(WG_SlotMachineIcon icon, int symbolValue, List<WG_SymbolSO> m_symbols, WG_SlotMachine machine)
    {
        if (icon == null) return;
        if (symbolValue < 0 || symbolValue >= m_symbols.Count) return;

        var symbolData = m_symbols[symbolValue];
        if (symbolData == null) return;

        // Se já é o símbolo correto e não é spine, não faz nada
        if (icon.m_spriteNumber == symbolValue && !symbolData.useSpineDefault)
        {
            return;
        }

        // Para qualquer mudança que envolva spine, força recriação completa
        icon.StopAnimation();

        // Limpar spines existentes via reflection
        ClearIconSpineComplete(icon);

        // Atualizar o número do símbolo
        icon.m_spriteNumber = symbolValue;

        bool isSpinning = false; // Durante replay, não está girando

        if (symbolData.useSpineDefault && symbolData.defaultSpinePreview != null)
        {
            // É SPINE - desabilita sprite e texto
            if (icon.m_renderer != null)
            {
                icon.m_renderer.enabled = false;
                icon.m_renderer.sprite = null;
            }
            if (icon.text != null)
            {
                icon.text.enabled = false;
            }

            // Criar novo spine
            if (icon.m_rectTransformAnim != null)
            {
                var newSpine = Instantiate(symbolData.defaultSpinePreview, icon.m_rectTransformAnim);
                newSpine.transform.SetParent(icon.m_rectTransformAnim, false);

                // Aplicar rect transform
                var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                var rect = newSpine.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = rectVector.anchorMin;
                    rect.anchorMax = rectVector.anchorMax;
                    rect.offsetMin = rectVector.offsetMin;
                    rect.offsetMax = rectVector.offsetMax;
                    rect.pivot = rectVector.pivot;
                    rect.anchoredPosition = rectVector.anchoredPosition;
                    rect.localScale = rectVector.localScale;
                }

                // Iniciar animação
                string animationName = symbolData.GetDefaultAnimation(isSpinning);
                if (!string.IsNullOrEmpty(animationName))
                {
                    newSpine.AnimationState.SetAnimation(0, animationName, true);
                }

                // Salvar referência (via reflection)
                var defaultSpineInstanceField = typeof(WG_SlotMachineIcon).GetField("defaultSpineInstance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (defaultSpineInstanceField != null)
                {
                    defaultSpineInstanceField.SetValue(icon, newSpine);
                }
            }
        }
        else
        {
            // É SPRITE NORMAL
            // Destruir qualquer spine remanescente
            var defaultSpineInstanceField = typeof(WG_SlotMachineIcon).GetField("defaultSpineInstance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (defaultSpineInstanceField != null)
            {
                var oldSpine = defaultSpineInstanceField.GetValue(icon) as Spine.Unity.SkeletonGraphic;
                if (oldSpine != null)
                {
                    DestroyImmediate(oldSpine.gameObject);
                    defaultSpineInstanceField.SetValue(icon, null);
                }
            }

            // Obter sprite
            Sprite sprite = null;
            if (machine != null && machine.m_reelIcons != null && machine.m_reelIcons.ContainsKey(symbolValue))
            {
                sprite = machine.m_reelIcons[symbolValue];
            }
            else if (symbolData.defaultSprite != null)
            {
                sprite = symbolData.defaultSprite;
            }

            // Aplicar sprite
            if (icon.m_renderer != null && sprite != null)
            {
                icon.m_renderer.sprite = sprite;
                icon.m_renderer.enabled = true;
                icon.m_sprite = sprite;

                var rectVector = symbolData.GetRectTransformVector(isSpinning, true);
                var rect = icon.m_renderer.rectTransform;
                if (rect != null)
                {
                    rect.anchorMin = rectVector.anchorMin;
                    rect.anchorMax = rectVector.anchorMax;
                    rect.offsetMin = rectVector.offsetMin;
                    rect.offsetMax = rectVector.offsetMax;
                    rect.pivot = rectVector.pivot;
                    rect.anchoredPosition = rectVector.anchoredPosition;
                    rect.localScale = rectVector.localScale;
                }
            }

            // Configurar texto
            if (icon.text != null)
            {
                icon.text.text = symbolValue.ToString();
                icon.text.enabled = true;
            }
        }

        // Limpar efeitos visuais
        if (icon.lineMark != null)
            icon.lineMark.SetActive(false);
        if (icon.linePayment != null)
            icon.linePayment.gameObject.SetActive(false);
        icon.Fade(false);
        icon.CardGlow(false);
        icon.isActive = false;
    }

    /// <summary>
    /// Limpa completamente todos os spines de um ícone
    /// </summary>
    private void ClearIconSpineComplete(WG_SlotMachineIcon icon)
    {
        // Limpar skeletonGraphic
        var skeletonGraphicField = typeof(WG_SlotMachineIcon).GetField("skeletonGraphic",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (skeletonGraphicField != null)
        {
            var skeletonGraphic = skeletonGraphicField.GetValue(icon) as Spine.Unity.SkeletonGraphic;
            if (skeletonGraphic != null)
            {
                DestroyImmediate(skeletonGraphic.gameObject);
                skeletonGraphicField.SetValue(icon, null);
            }
        }

        // Limpar defaultSpineInstance
        var defaultSpineInstanceField = typeof(WG_SlotMachineIcon).GetField("defaultSpineInstance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (defaultSpineInstanceField != null)
        {
            var defaultSpineInstance = defaultSpineInstanceField.GetValue(icon) as Spine.Unity.SkeletonGraphic;
            if (defaultSpineInstance != null)
            {
                DestroyImmediate(defaultSpineInstance.gameObject);
                defaultSpineInstanceField.SetValue(icon, null);
            }
        }

        // Também limpar spines nos filhos diretos
        var childrenSpines = icon.GetComponentsInChildren<Spine.Unity.SkeletonGraphic>(true);
        foreach (var spine in childrenSpines)
        {
            DestroyImmediate(spine.gameObject);
        }
    }

    public void PlayBonus()
    {
        var slotClient = SlotClient.Instance;

        var historyItem = slotClient.GameHistory[historyIndexCur];
        var bonusResponse = ConvertHistoryToBonusResponse(historyItem, historyIndexBonusCur);
        historyIndexBonusCur++;

        if (historyIndexBonusCur > historyItem.playItem.bonusSpins.Count - 1)
            historyIndexBonusCur = historyItem.playItem.bonusSpins.Count - 1;

        if (bonusResponse != null)
        {
            // slotClient.BonusResponse(bonusResponse);
        }
    }

    #endregion
}
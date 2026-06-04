using UnityEngine;
using TMPro;
using System.Collections;
using WG_Casino.Systems;
using Spine.Unity;
using WG_Casino.SlotMachine;
using WG_Casino;
using System.Collections.Generic;
using System.Linq;

public class PayoutAnimation : MonoBehaviour
{
    public static PayoutAnimation Instance;

    [Header("References")]
    public TextMeshProUGUI payoutText;
    public TextMeshProUGUI payoutTextWin;
    public RectTransform targetPosition;
    public GameObject hologramObj;

    [Header("Animation Settings")]
    public float bumpDuration = 0.3f;
    public float jumpDuration = 0.8f;
    public float jumpHeight = 100f;
    public float scaleToZeroDuration = 0.2f;
    public float delayBetweenAnimations = 0.1f;
    public bool onShake = true;
    public float shakeIntensity = 10f;

    public SkeletonGraphic creditsSpine;

    [Header("Text Settings")]
    public Color textColor = Color.yellow;

    [Header("Win Sequence System")]
    public WinSequence winSequence;
    public Transform winSequenceParent;
    public bool enableWinSequence = true;

    // Valores sugeridos para thresholds com base na aposta mínima de R$1,00:
    // Big Win: 20x a aposta → 20 × 100 = 2000 centavos
    // Mega Win: 50x a aposta → 50 × 100 = 5000 centavos  
    // Super Win: 100x a aposta → 100 × 100 = 10000 centavos

    private RectTransform payRect;
    private Vector2 initialPosition;
    private Vector3 initialScale;
    private CanvasGroup canvasGroup;
    private Coroutine currentAnimation;
    private Coroutine winSequenceCoroutine;
    private List<GameObject> activeWinObjects = new List<GameObject>();
    private bool isInWinSequence = false;

    public bool isPayout;
    public bool onBonus;

    // Valor atual da aposta em centavos
    private int currentBetInCents = 100;

    // Thresholds base (valores para aposta mínima)
    private int baseBigWinThreshold = 2000;   // 20x R$1,00 = R$20,00
    private int baseMegaWinThreshold = 5000;  // 50x R$1,00 = R$50,00
    private int baseSuperWinThreshold = 10000; // 100x R$1,00 = R$100,00

    private void Awake()
    {
        Instance = this;
        payRect = payoutText.GetComponent<RectTransform>();
        if (payRect == null)
        {
            Debug.LogError("❌ PayoutAnimation precisa estar em um objeto com RectTransform!");
            return;
        }

        initialPosition = payRect.anchoredPosition;
        initialScale = payRect.localScale;
        canvasGroup = payoutText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        ResetToInitialState();

        if (winSequenceParent == null)
        {
            winSequenceParent = new GameObject("WinSequenceParent").transform;
            winSequenceParent.SetParent(transform);
            winSequenceParent.localPosition = Vector3.zero;
        }
    }

    private void Start()
    {
        WG_SlotPaymentSystem paymentSystem = FindObjectOfType<WG_SlotPaymentSystem>();
        if (paymentSystem != null)
        {
            paymentSystem.OnPaymentAmount += StartPayoutAnimation;
        }

        // Inscrever no evento de mudança de aposta
        GameEvents.OnBetChanged += HandleBetChanged;

        // Obter aposta atual
        if (GameManager.Instance != null)
        {
            currentBetInCents = GameManager.Instance.Bet;
            UpdateWinSequenceThresholds();
        }
    }

    private void OnDestroy()
    {
        GameEvents.OnBetChanged -= HandleBetChanged;

        WG_SlotPaymentSystem paymentSystem = FindObjectOfType<WG_SlotPaymentSystem>();
        if (paymentSystem != null)
        {
            paymentSystem.OnPaymentAmount -= StartPayoutAnimation;
        }
    }

    private void HandleBetChanged(int newBetInCents)
    {
        currentBetInCents = newBetInCents;
        UpdateWinSequenceThresholds();
        if (GameEvents.EnableDebugLogs) Debug.Log($"[PayoutAnimation] Aposta atualizada: {GameUtils.FormatCurrency(currentBetInCents)}");
    }

    /// <summary>
    /// Atualiza os thresholds do Win Sequence com base na aposta atual
    /// </summary>
    private void UpdateWinSequenceThresholds()
    {
        if (winSequence == null || winSequence.winLevels == null) return;

        // Calcula o multiplicador relativo à aposta base
        int minBetInCents = 100; // R$1,00 é a aposta mínima do jogo
        float multiplier = ((float)currentBetInCents) / minBetInCents;

        foreach (var level in winSequence.winLevels)
        {
            // Salva o valor base original se ainda não foi salvo
            if (!level.hasOriginalThreshold)
            {
                level.originalThreshold = level.minRewardThreshold;
                level.hasOriginalThreshold = true;
            }

            // Calcula o novo threshold baseado na aposta atual
            int newThreshold = Mathf.RoundToInt(level.originalThreshold * multiplier);
            level.minRewardThreshold = newThreshold;

          if(GameEvents.EnableDebugLogs)  Debug.Log($"[PayoutAnimation] {level.winName}: Threshold ajustado de {level.originalThreshold} para {newThreshold} (aposta: {GameUtils.FormatCurrency(currentBetInCents)})");
        }
    }

    /// <summary>
    /// Configura os thresholds base para Win Sequence (chamar no Start do jogo)
    /// </summary>
    public void ConfigureWinSequenceThresholds(int bigWin, int megaWin, int superWin)
    {
        baseBigWinThreshold = bigWin;
        baseMegaWinThreshold = megaWin;
        baseSuperWinThreshold = superWin;

        if (winSequence != null && winSequence.winLevels.Count >= 3)
        {
            // Configura os valores base originais
            winSequence.winLevels[0].originalThreshold = bigWin;
            winSequence.winLevels[1].originalThreshold = megaWin;
            winSequence.winLevels[2].originalThreshold = superWin;
            winSequence.winLevels[0].hasOriginalThreshold = true;
            winSequence.winLevels[1].hasOriginalThreshold = true;
            winSequence.winLevels[2].hasOriginalThreshold = true;

            UpdateWinSequenceThresholds();
        }
    }

    public void StartPayoutAnimation(int payout, int symbolId)
    {
        isPayout = true;

        if (SlotClient.Instance.playResponse != null && SlotClient.Instance.playResponse.waysSummary != null &&
            SlotClient.Instance.playResponse.waysSummary.perSymbol != null && SlotClient.Instance.playResponse.waysSummary.perSymbol.Length == 0)
        {
            isPayout = false;
            return;
        }

        int totalReward = onBonus ? GetTotalRewardBonus() : GetTotalReward();
        bool justFinishedBonus = onBonus && SlotClient.Instance.bonusResponse != null && SlotClient.Instance.bonusResponse.bonusOver;

        if (totalReward == 0)
        {
            if (GameEvents.EnableDebugLogs) Debug.Log("[PayoutAnimation] Nenhum ganho, completando payout imediatamente");
            GameEvents.CompletePayout();
            isPayout = false;
            return;
        }

        if (enableWinSequence && ShouldTriggerWinSequence(totalReward))
        {
            StartWinSequence(totalReward);
        }
        else
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                ResetToInitialState();
            }
            currentAnimation = StartCoroutine(PayoutAnimationRoutine(payout, justFinishedBonus));
        }
    }

    #region SequenceWin

    private int GetTotalReward()
    {
        if (SlotClient.Instance != null && SlotClient.Instance.playResponse != null)
        {
            return SlotClient.Instance.playResponse.rewards;
        }
        return 0;
    }

    private int GetTotalRewardBonus()
    {
        if (SlotClient.Instance != null && SlotClient.Instance.bonusResponse != null)
        {
            return SlotClient.Instance.bonusResponse.rewards;
        }
        return 0;
    }

    private bool ShouldTriggerWinSequence(int totalReward)
    {
        if (winSequence == null || winSequence.winLevels.Count == 0)
            return false;

        foreach (var level in winSequence.winLevels)
        {
            if (totalReward >= level.minRewardThreshold)
                return true;
        }
        return false;
    }

    public void StartWinSequence(int totalPayout)
    {
        if (isInWinSequence)
        {
            if (winSequence.interruptible)
            {
                StopWinSequence();
            }
            else
            {
                return;
            }
        }

        winSequenceCoroutine = StartCoroutine(WinSequenceRoutine(totalPayout));
    }

    private IEnumerator WinSequenceRoutine(int totalReward)
    {
        isInWinSequence = true;
        isPayout = true;

        if (GameEvents.EnableDebugLogs) Debug.Log($"🎬 Iniciando Win Sequence para TOTAL reward: {GameUtils.FormatCurrency(totalReward)} (aposta: {GameUtils.FormatCurrency(currentBetInCents)})");

        var levelsToShow = winSequence.winLevels
            .Where(level => totalReward >= level.minRewardThreshold)
            .OrderBy(level => level.minRewardThreshold)
            .ToList();

        if (levelsToShow.Count == 0)
        {
            Debug.LogWarning("Nenhum nível de win encontrado para o reward total: " + totalReward);
            currentAnimation = StartCoroutine(PayoutAnimationRoutine(totalReward));
            yield return new WaitUntil(() => !isPayout);
            isInWinSequence = false;
            yield break;
        }

        if (GameEvents.EnableDebugLogs) Debug.Log($"🏆 Iniciando sequência com {levelsToShow.Count} níveis de win");

        if (payoutTextWin == null)
        {
            Debug.LogError("❌ payoutTextWin não está configurado no inspector!");
            yield break;
        }

        Vector3 originalPayoutTextScale = payoutTextWin.transform.localScale;
        Color originalPayoutTextColor = payoutTextWin.color;

        payoutTextWin.text = "";
        payoutTextWin.color = Color.yellow;

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        payoutTextWin.transform.localScale = Vector3.one;

        float currentDisplayedReward = 0f;

        for (int i = 0; i < levelsToShow.Count; i++)
        {
            var level = levelsToShow[i];
            bool isLastLevel = (i == levelsToShow.Count - 1);
            float targetForThisLevel = isLastLevel ? totalReward : level.minRewardThreshold;

            if (GameEvents.EnableDebugLogs) Debug.Log($"🏆 Nível {i}: {level.winName} - Target: {GameUtils.FormatCurrency((int)targetForThisLevel)} (Duração: {level.duration}s)");

            if (level.winObject != null)
            {
                GameObject winObj = Instantiate(level.winObject, winSequenceParent);
                activeWinObjects.Add(winObj);
                winObj.transform.localPosition = Vector3.zero;
                winObj.SetActive(true);
            }

            // Tocar som do nível se disponível
            if (level.soundEffect != null)
            {
                WG_AudioManager.PlaySE(level.soundEffect, 1f);
            }

            float distanceToTarget = targetForThisLevel - currentDisplayedReward;
            float incrementPerSecond = distanceToTarget / level.duration;
            float elapsedTime = 0f;

            while (elapsedTime < level.duration)
            {
                elapsedTime += Time.deltaTime;

                if (currentDisplayedReward < targetForThisLevel)
                {
                    float increment = incrementPerSecond * Time.deltaTime;
                    currentDisplayedReward = Mathf.Min(currentDisplayedReward + increment, targetForThisLevel);
                    int displayValue = Mathf.RoundToInt(currentDisplayedReward);
                    payoutTextWin.text = GameUtils.FormatCurrency(displayValue);
                }

                yield return null;
            }

            currentDisplayedReward = targetForThisLevel;
            int finalValue = Mathf.RoundToInt(currentDisplayedReward);
            payoutTextWin.text = GameUtils.FormatCurrency(finalValue);

            if (!isLastLevel)
            {
                if (GameEvents.EnableDebugLogs) Debug.Log($"🎯 Bump no threshold: {GameUtils.FormatCurrency(level.minRewardThreshold)}");
                yield return StartCoroutine(BumpRewardText(level.minRewardThreshold, false, originalPayoutTextScale));
            }
            else
            {
                if (GameEvents.EnableDebugLogs) Debug.Log($"🎯 Bump final no valor total: {GameUtils.FormatCurrency(totalReward)}");
                yield return StartCoroutine(BumpRewardText(totalReward, true, originalPayoutTextScale));
            }

            if (!isLastLevel)
            {
                ClearWinObjects();
                yield return new WaitForSeconds(winSequence.delayBetweenLevels);
            }
        }

        yield return new WaitForSeconds(1f);

        payoutTextWin.color = originalPayoutTextColor;
        UpdateTotalGainAndCredits(totalReward);
        ClearWinObjects();
        payoutTextWin.transform.localScale = originalPayoutTextScale;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        payoutTextWin.text = "";

        isInWinSequence = false;
        isPayout = false;

        if (GameEvents.EnableDebugLogs) Debug.Log("✅ Win Sequence finalizada");
    }

    private IEnumerator BumpRewardText(int thresholdValue, bool isFinal, Vector3 originalScale)
    {
        if (payoutTextWin == null) yield break;

        Transform textTransform = payoutTextWin.transform;
        Color originalColor = payoutTextWin.color;
        Color bumpColor = isFinal ? Color.green : new Color(1f, 0.5f, 0f);

        payoutTextWin.color = bumpColor;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (t < 0.5f)
            {
                float scale = Mathf.Lerp(1f, 1.3f, t * 2f);
                textTransform.localScale = originalScale * scale;
            }
            else
            {
                float scale = Mathf.Lerp(1.3f, 1f, (t - 0.5f) * 2f);
                textTransform.localScale = originalScale * scale;
            }

            yield return null;
        }

        textTransform.localScale = originalScale;

        if (!isFinal)
        {
            payoutTextWin.color = originalColor;
        }

        if (GameEvents.EnableDebugLogs) Debug.Log($"🎯 Threshold bump: {GameUtils.FormatCurrency(thresholdValue)}");
    }

    private void UpdateTotalGainAndCredits(int totalReward)
    {
        if (CanvasManager.Instance != null)
        {
            StartCoroutine(PayoutAnimationRoutine(totalReward));
        }

        if (creditsSpine)
        {
            creditsSpine.AnimationState.ClearTracks();
            creditsSpine.Skeleton.SetToSetupPose();
            creditsSpine.AnimationState.SetAnimation(0, "WinCelebration", false);
        }

        if (GameEvents.EnableDebugLogs) Debug.Log($"💰 Ganho total atualizado: {GameUtils.FormatCurrency(totalReward)}");
    }

    private void ClearWinObjects()
    {
        foreach (var obj in activeWinObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        activeWinObjects.Clear();
    }

    public void StopWinSequence()
    {
        if (winSequenceCoroutine != null)
        {
            StopCoroutine(winSequenceCoroutine);
            winSequenceCoroutine = null;
        }

        ClearWinObjects();
        isInWinSequence = false;
        isPayout = false;
    }

    public bool IsAnimating()
    {
        return isPayout || isInWinSequence;
    }

    #endregion

    private IEnumerator PayoutAnimationRoutine(int payout, bool isBonusFinalPayment = false)
    {
        if (isBonusFinalPayment)
        {
            if (GameEvents.EnableDebugLogs) Debug.Log("💰 Aguardando término completo das animações do bônus...");
            yield return new WaitUntil(() => !BonusManager.Instance.isAnimating && !BonusManager.Instance.isAnimatingBonus);
            yield return new WaitForSeconds(0.5f);
            onBonus = false;
        }

        yield return new WaitUntil(() => WG_SlotMachine.Instance.AreAllReelsCompleted());

        if (payout == 0)
        {
            if (GameEvents.EnableDebugLogs) Debug.Log("[PayoutAnimation] Payout = 0, pulando animação");
            GameEvents.CompletePayout();
            currentAnimation = null;
            isPayout = false;
            yield break;
        }

        if (GameEvents.EnableDebugLogs) Debug.Log($"🎯 Iniciando animação de payout: {GameUtils.FormatCurrency(payout)}");
        payoutText.text = GameUtils.FormatCurrency(payout);
        payoutText.color = textColor;
        ResetToInitialState();

        yield return StartCoroutine(BumpAnimation());
        yield return new WaitForSeconds(delayBetweenAnimations);
        yield return StartCoroutine(JumpToCreditsAnimation(payout));
        yield return StartCoroutine(ScaleToZeroAnimation(payout));

        ResetToInitialState();
        currentAnimation = null;

        if (!isInWinSequence)
        {
            yield return new WaitForSeconds(1);
            isPayout = false;
        }
    }

    public List<WG_SlotMachineIcon> icons = new List<WG_SlotMachineIcon>();

    private void CheckSymbols(WG_LineRows line, int symbolId, int payout)
    {
        foreach (var reel in line.m_reels)
        {
            var icon = reel.m_icons[0];
            if (icon.m_spriteNumber == symbolId && icon.isActive)
            {
                icons.Add(icon);
            }
        }
    }

    private IEnumerator BumpAnimation()
    {
        canvasGroup.alpha = 1f;
        payRect.localScale = Vector3.zero;
        hologramObj.SetActive(true);

        float elapsed = 0f;
        while (elapsed < bumpDuration * 0.6f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (bumpDuration * 0.6f);
            float scale = EaseOutBack(t) * 1.2f;
            payRect.localScale = initialScale * scale;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < bumpDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (bumpDuration * 0.4f);
            float scale = Mathf.Lerp(1.2f, 1f, EaseOutCubic(t));
            payRect.localScale = initialScale * scale;
            yield return null;
        }

        payRect.localScale = initialScale;

        if (onShake)
            StartCoroutine(Shake(payRect.transform));
    }

    private IEnumerator JumpToCreditsAnimation(int payout)
    {
        WG_SlotPaymentSystem paymentSystem = FindObjectOfType<WG_SlotPaymentSystem>();
        icons.Clear();
        foreach (var item in WG_SlotMachine.Instance.lineRows)
        {
            CheckSymbols(item, paymentSystem.symbolIdCurrent, payout);
        }

        if (targetPosition == null)
        {
            Debug.LogError("❌ Target position não configurado!");
            yield break;
        }

        Vector2 startPos = payRect.anchoredPosition;
        Vector2 endPos = targetPosition.anchoredPosition;
        Vector2 midPoint = (startPos + endPos) * 0.5f;
        Vector2 controlPoint = midPoint + (Vector2.up * jumpHeight);

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            Vector2 position = CalculateBezierPoint(startPos, controlPoint, endPos, EaseOutCubic(t));
            payRect.anchoredPosition = position;
            float scale = Mathf.Lerp(1f, 0.7f, EaseInCubic(t));
            payRect.localScale = initialScale * scale;
            yield return null;
        }

        payRect.anchoredPosition = endPos;
        payRect.localScale = initialScale * 0.7f;
    }

    private IEnumerator ScaleToZeroAnimation(int payout)
    {
        Vector3 startScale = payRect.localScale;
        float elapsed = 0f;

        while (elapsed < scaleToZeroDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleToZeroDuration;
            float scale = Mathf.Lerp(0.7f, 0f, EaseInBack(t));
            payRect.localScale = initialScale * scale;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, EaseInCubic(t));
            yield return null;
        }

        hologramObj.SetActive(false);

        GameEvents.AddGain(payout);
        GameEvents.Payout(payout, 0);

        if (creditsSpine)
        {
            creditsSpine.AnimationState.ClearTracks();
            creditsSpine.Skeleton.SetToSetupPose();
            creditsSpine.AnimationState.SetAnimation(0, "WinCelebration", false);
        }

        if (onShake)
        {
            GameEvents.RequestShake(GameEvents.ShakeTarget.GainText);
        }

        GameEvents.CompletePayout();

        if (onShake)
        {
            GameEvents.RequestShake(GameEvents.ShakeTarget.CreditsText);
        }

        payRect.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        hologramObj.SetActive(false);
    }

    IEnumerator Shake(Transform obj)
    {
        Vector3 originalPos = obj.localPosition;
        float shakeTime = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < shakeTime)
        {
            elapsedTime += Time.deltaTime;
            float xShake = Random.Range(-shakeIntensity, shakeIntensity);
            float yShake = Random.Range(-shakeIntensity, shakeIntensity);
            obj.localPosition = originalPos + new Vector3(xShake, yShake, 0);
            yield return null;
        }
        obj.localPosition = originalPos;
    }

    private Vector2 CalculateBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return uu * p0 + 2 * u * t * p1 + tt * p2;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t) => t * t * t;

    private float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    private void ResetToInitialState()
    {
        if (payRect == null) return;
        payRect.anchoredPosition = initialPosition;
        payRect.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        hologramObj.SetActive(false);
    }

    [ContextMenu("Reset Animation")]
    public void ResetAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        ResetToInitialState();
        if (GameEvents.EnableDebugLogs) Debug.Log("🔄 Animação resetada manualmente");
    }

    public void TestAnimation()
    {
        StartPayoutAnimation(100, 1);
    }
}

[System.Serializable]
public class WinLevelConfig
{
    public string winName;
    public GameObject winObject;
    public int minRewardThreshold;
    public float duration;
    public AudioClip soundEffect;

    [HideInInspector] public int originalThreshold;
    [HideInInspector] public bool hasOriginalThreshold;
}

[System.Serializable]
public class WinSequence
{
    public List<WinLevelConfig> winLevels = new List<WinLevelConfig>();
    public float delayBetweenLevels = 0.5f;
    public bool interruptible = false;
}
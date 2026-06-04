// GameEvents.cs
using System;
using UnityEngine;

/// <summary>
/// Sistema centralizado de eventos do jogo.
/// Permite que módulos se comuniquem sem dependência direta entre eles.
/// </summary>
public static class GameEvents
{
    // ============ Configuração de Debug ============

    /// <summary>
    /// Habilita logs de debug para todos os eventos.
    /// Útil para rastrear o fluxo de eventos durante o desenvolvimento.
    /// </summary>
    public static bool EnableDebugLogs { get; set; } = true;

    /// <summary>
    /// Prefixo usado nos logs de debug para fácil identificação.
    /// </summary>
    private const string LOG_PREFIX = "<color=#00FFAA>[GameEvents]</color>";

    // ============ Spinner/Slot Events ============
    /// <summary>Disparado quando o jogo inicia</summary>
    public static event Action OnStartRequested;
    public static void RequestStart()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} Start Game Requested");
        OnStartRequested?.Invoke();
    }

    /// <summary>Disparado quando o jogo inicia e termina ação</summary>
    public static event Action OnStartEndRequested;
    public static void RequestStartEnd()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} Start End Game Requested");
        OnStartEndRequested?.Invoke();
    }

    /// <summary>Disparado quando um spin é solicitado (antes da validação de créditos)</summary>
    public static event Action OnSpinRequested;
    public static void RequestSpin()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 📢 Spin Requested");
        OnSpinRequested?.Invoke();
    }

    /// <summary>Disparado quando o spin é validado e aprovado</summary>
    public static event Action OnSpinApproved;
    public static void ApproveSpin()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} ✅ Spin Approved");
        OnSpinApproved?.Invoke();
    }

    /// <summary>Disparado quando o spin é recusado (ex: saldo insuficiente)</summary>
    public static event Action<string> OnSpinDenied;
    public static void DenySpin(string reason)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} ❌ Spin Denied: {reason}");
        OnSpinDenied?.Invoke(reason);
    }

    /// <summary>Disparado quando o spin é completado</summary>
    public static event Action OnSpinCompleted;
    public static void CompleteSpin()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🎯 Spin Completed");
        OnSpinCompleted?.Invoke();
    }

    // ============ Bet Events ============

    /// <summary>Disparado quando a aposta é alterada</summary>
    public static event Action<int> OnBetChanged;
    public static void ChangeBet(int newBetInCents)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 💰 Bet Changed: {GameUtils.FormatCurrency(newBetInCents)} ({newBetInCents} cents)");
        OnBetChanged?.Invoke(newBetInCents);
    }

    // ============ Credit/Balance Events ============

    /// <summary>Disparado quando o saldo é atualizado</summary>
    public static event Action<int> OnBalanceUpdated;
    public static void UpdateBalance(int newBalance)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 💵 Balance Updated: {GameUtils.FormatCurrency(newBalance)} ({newBalance} cents)");
        OnBalanceUpdated?.Invoke(newBalance);
    }

    /// <summary>Disparado quando há um ganho (acumulado durante o spin)</summary>
    public static event Action<int> OnGain;
    public static void AddGain(int amount)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} ✨ Gain Added: {GameUtils.FormatCurrency(amount)} ({amount} cents)");
        OnGain?.Invoke(amount);
    }

    // ============ Payout/Win Events ============

    /// <summary>Disparado quando um pagamento ocorre (ganho individual por símbolo/linha)</summary>
    public static event Action<int, int> OnPayout;
    public static void Payout(int amount, int symbolId)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🎰 Payout: {GameUtils.FormatCurrency(amount)} for Symbol ID {symbolId}");
        OnPayout?.Invoke(amount, symbolId);
    }

    /// <summary>Disparado quando o processo de pagamento é finalizado</summary>
    public static event Action OnPayoutCompleted;
    public static void CompletePayout()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🏁 Payout Completed");
        OnPayoutCompleted?.Invoke();
    }

    // ============ Bonus Events ============

    /// <summary>Disparado quando um bônus é iniciado</summary>
    public static event Action OnBonusStarted;
    public static void StartBonus()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🎁 Bonus Started");
        OnBonusStarted?.Invoke();
    }

    /// <summary>Disparado quando um bônus termina</summary>
    public static event Action<int> OnBonusEnded;
    public static void EndBonus(int totalWin)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🏆 Bonus Ended - Total Win: {GameUtils.FormatCurrency(totalWin)}");
        OnBonusEnded?.Invoke(totalWin);
    }

    // ============ Auto-Spin Events ============

    /// <summary>Disparado quando auto-spin inicia</summary>
    public static event Action<int> OnAutoSpinRequest;
    public static void RequesttAutoSpin(int spins)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🔄 Auto-Spin Request: {spins} spins");
        OnAutoSpinRequest?.Invoke(spins);
    }

    /// <summary>Disparado quando auto-spin inicia</summary>
    public static event Action<int> OnAutoSpinStarted;
    public static void StartAutoSpin(int spins)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🔄 Auto-Spin Started: {spins} spins");
        OnAutoSpinStarted?.Invoke(spins);
    }

    /// <summary>Disparado quando auto-spin para</summary>
    public static event Action OnAutoSpinStopped;
    public static void StopAutoSpin()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} ⏹️ Auto-Spin Stopped");
        OnAutoSpinStopped?.Invoke();
    }

    /// <summary>Disparado a cada spin completado durante auto-spin</summary>
    public static event Action OnAutoSpinStep;
    public static void AutoSpinStep()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 👣 Auto-Spin Step");
        OnAutoSpinStep?.Invoke();
    }

    // ============ Turbo/Game Speed Events ============

    /// <summary>Disparado quando modo turbo é alterado</summary>
    public static event Action<bool> OnTurboChanged;
    public static void ChangeTurbo(bool isEnabled)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} ⚡ Turbo Mode: {(isEnabled ? "ON" : "OFF")}");
        OnTurboChanged?.Invoke(isEnabled);
    }

    // ============ UI/Feedback Events ============
    /// <summary>
    /// Tipos de alvo para efeito de shake
    /// </summary>
    public enum ShakeTarget
    {
        GainText,
        CreditsText,
        PlayButton,
        BetButton,
        All
    }

    /// <summary>Disparado quando um efeito de shake deve ser aplicado</summary>
    public static event Action<ShakeTarget> OnShakeRequested;
    public static void RequestShake(ShakeTarget target)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 📳 Shake requested for: {target}");
        OnShakeRequested?.Invoke(target);
    }
    /// <summary>Disparado quando um erro/mensagem deve ser mostrada</summary>
    public static event Action<string, string> OnMessageShow;
    public static void ShowMessage(string title, string message)
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 💬 Message: [{title}] {message}");
        OnMessageShow?.Invoke(title, message);
    }

    /// <summary>Disparado quando algum módulo precisa aguardar conclusão de outro</summary>
    public static event Action OnWaitForCompletion;
    public static void WaitForCompletion()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} ⏳ Waiting for completion");
        OnWaitForCompletion?.Invoke();
    }

    /// <summary>Disparado quando o sistema está pronto para próximo spin</summary>
    public static event Action OnReadyForNext;
    public static void ReadyForNext()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} ✅ Ready for next spin");
        OnReadyForNext?.Invoke();
    }


    // ============ Replay/State Events ============

    /// <summary>Disparado quando o replay é iniciado - o sistema deve salvar seu estado</summary>
    public static event System.Action OnReplayStarting;
    public static void ReplayStarting() => OnReplayStarting?.Invoke();

    /// <summary>Disparado quando o replay termina - o sistema deve restaurar seu estado</summary>
    public static event System.Action OnReplayEnding;
    public static void ReplayEnding() => OnReplayEnding?.Invoke();

    // GameEvents.cs - Adicionar estes eventos

    // ============ Replay Events ============
    public static void TriggerReplayStarting()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🎬 Replay Starting");
        OnReplayStarting?.Invoke();
    }

    public static void TriggerReplayEnding()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🏁 Replay Ending");
        OnReplayEnding?.Invoke();
    }

    /// <summary>Evento para salvar valores de créditos/aposta/ganho</summary>
    public static event System.Action<int, int, int> OnSavePlayerState; // (credits, bet, gain)
    public static void SavePlayerState(int credits, int bet, int gain) => OnSavePlayerState?.Invoke(credits, bet, gain);

    /// <summary>Evento para restaurar valores</summary>
    public static event System.Action<int, int, int> OnRestorePlayerState; // (credits, bet, gain)
    public static void RestorePlayerState(int credits, int bet, int gain) => OnRestorePlayerState?.Invoke(credits, bet, gain);

    // ============ Utility Methods ============

    /// <summary>
    /// Limpa todos os eventos (útil para reset entre cenas ou testes).
    /// Use com cuidado - isso remove todas as inscrições.
    /// </summary>
    public static void ClearAllEvents()
    {
        if (EnableDebugLogs) Debug.Log($"{LOG_PREFIX} 🧹 Clearing all events");

        OnSpinRequested = null;
        OnSpinApproved = null;
        OnSpinDenied = null;
        OnSpinCompleted = null;
        OnBetChanged = null;
        OnBalanceUpdated = null;
        OnGain = null;
        OnPayout = null;
        OnPayoutCompleted = null;
        OnBonusStarted = null;
        OnBonusEnded = null;
        OnAutoSpinStarted = null;
        OnAutoSpinStopped = null;
        OnAutoSpinStep = null;
        OnTurboChanged = null;
        OnMessageShow = null;
        OnWaitForCompletion = null;
        OnReadyForNext = null;
    }
}
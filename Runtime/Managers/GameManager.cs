using SlotMachineMath;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WG_Casino.SlotMachine;

namespace WG_Casino
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Game Settings")]
        [SerializeField] bool started;
        public bool Started
        {
            get => started;
            set => started = value;
        }

        [SerializeField] bool playing;
        public bool Playing
        {
            get => playing;
            set => playing = value;
        }

        [SerializeField] bool autoPlay;
        public bool AutoPlay
        {
            get => autoPlay;
            set => autoPlay = value;
        }

        [SerializeField] bool clickPlay;
        public bool ClickPlay
        {
            get => clickPlay;
            set => clickPlay = value;
        }

        [SerializeField] int freeSpin;
        public int FreeSpin
        {
            get => freeSpin;
            set => freeSpin = value;
        }

        [SerializeField] bool turbo;
        public bool Turbo
        {
            get => turbo;
            set => turbo = value;
        }

        [SerializeField] int credits = 0;
        public int Credits
        {
            get => credits;
            set => credits = value;
        }

        [SerializeField] int gain = 0;
        public int Gain
        {
            get => gain;
            set => gain = value;
        }

        [SerializeField] int autoSpins = 0;
        public int AutoSpins
        {
            get => autoSpins;
            set => autoSpins = value;
        }

        [SerializeField] int bet = 100; // Em centavos (100 = R$1.00)
        [SerializeField] int wildIconId;
        public int Bet
        {
            get => bet;
            set => bet = value;
        }

        [SerializeField] private List<int> availableBets = new List<int> { 100, 200, 500, 1000, 2000 };
        private int currentBetIndex = 0;

        public int GetBetIndex()
        {
            return availableBets[currentBetIndex];
        }

        [SerializeField] byte gameID;
        public byte GameID
        {
            get => gameID;
            set => gameID = value;
        }

        [SerializeField] byte wildIConID;
        public byte WildIConID
        {
            get => wildIConID;
            set => wildIConID = value;
        }

        [SerializeField] byte bigWinValue;
        public byte BigWinValue
        {
            get => bigWinValue;
            set => bigWinValue = value;
        }

        [SerializeField] byte megaWinValue;
        public byte MegaWinValue
        {
            get => megaWinValue;
            set => megaWinValue = value;
        }

        [SerializeField] byte superWinValue;
        public byte SuperWinValue
        {
            get => superWinValue;
            set => superWinValue = value;
        }

        [Header("Game")]
        Coroutine showGainsCoroutine;
        public bool onReplay;

        // Estado interno para gerenciamento
        private bool isInBonus = false;
        private int pendingGain = 0;

        void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
#if !UNITY_EDITOR
            GameEvents.EnableDebugLogs = false;
#endif
            StartCoroutine(StartBalance());
        }

        IEnumerator StartBalance()
        {
            yield return new WaitUntil(() => credits > 0);
            GameEvents.UpdateBalance(credits);
            GameEvents.ChangeBet(bet);
            GameEvents.AddGain(gain);

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Estado inicial enviado via eventos - Saldo: {GameUtils.FormatCurrency(credits)}, Bet: {GameUtils.FormatCurrency(bet)}");
        }

        private void OnEnable()
        {
            // ============ Eventos que AFETAM o estado do jogo ============

            // Inicio
            GameEvents.OnStartRequested += HandleStartRequest;
            GameEvents.OnStartEndRequested += HandleStartEndRequest;

            // 1. Spin - Gerencia validação e dedução de créditos
            GameEvents.OnSpinRequested += HandleSpinRequest;
            GameEvents.OnSpinApproved += HandleSpinApproved;
            GameEvents.OnSpinCompleted += HandleSpinCompleted;
            GameEvents.OnSpinDenied += HandleSpinDenied;

            // 2. Aposta - Mantém o valor atual da aposta
            GameEvents.OnBetChanged += HandleBetChanged;

            // 3. Saldo - Gerencia o crédito atual
            GameEvents.OnBalanceUpdated += HandleBalanceUpdated;

            // 4. Ganho - Acumula ganhos do spin atual
            GameEvents.OnGain += HandleGain;

            // 5. Payout - Atualiza saldo final com os ganhos
            GameEvents.OnPayout += HandlePayout;
            GameEvents.OnPayoutCompleted += HandlePayoutCompleted;

            // 6. Auto-Spin - Gerencia estado de auto-spin
            GameEvents.OnAutoSpinRequest += HandleAutoSpinRequest;
            GameEvents.OnAutoSpinStarted += HandleAutoSpinStarted;
            GameEvents.OnAutoSpinStopped += HandleAutoSpinStopped;
            GameEvents.OnAutoSpinStep += HandleAutoSpinStep;

            // 7. Turbo - Gerencia velocidade do jogo
            GameEvents.OnTurboChanged += HandleTurboChanged;

            // 8. Bonus - Gerencia estado de bônus
            GameEvents.OnBonusStarted += HandleBonusStarted;
            GameEvents.OnBonusEnded += HandleBonusEnded;

            // 9. ReadyForNext - Gatilho para próximo spin automático
            GameEvents.OnReadyForNext += HandleReadyForNext;

            // 10. Message - Para exibir mensagens (encaminha para UI)
            GameEvents.OnMessageShow += HandleMessageShow;

            GameEvents.OnSavePlayerState += HandleSavePlayerState;
            GameEvents.OnRestorePlayerState += HandleRestorePlayerState;

            if (GameEvents.EnableDebugLogs) Debug.Log("[GameManager] Event handlers registered");
        }

        private void OnDisable()
        {
            GameEvents.OnSpinRequested -= HandleSpinRequest;
            GameEvents.OnSpinApproved -= HandleSpinApproved;
            GameEvents.OnSpinCompleted -= HandleSpinCompleted;
            GameEvents.OnSpinDenied -= HandleSpinDenied;
            GameEvents.OnBetChanged -= HandleBetChanged;
            GameEvents.OnBalanceUpdated -= HandleBalanceUpdated;
            GameEvents.OnGain -= HandleGain;
            GameEvents.OnPayout -= HandlePayout;
            GameEvents.OnAutoSpinRequest -= HandleAutoSpinRequest;
            GameEvents.OnPayoutCompleted -= HandlePayoutCompleted;
            GameEvents.OnAutoSpinStarted -= HandleAutoSpinStarted;
            GameEvents.OnAutoSpinStopped -= HandleAutoSpinStopped;
            GameEvents.OnAutoSpinStep -= HandleAutoSpinStep;
            GameEvents.OnTurboChanged -= HandleTurboChanged;
            GameEvents.OnBonusStarted -= HandleBonusStarted;
            GameEvents.OnBonusEnded -= HandleBonusEnded;
            GameEvents.OnReadyForNext -= HandleReadyForNext;
            GameEvents.OnMessageShow -= HandleMessageShow;

            GameEvents.OnSavePlayerState -= HandleSavePlayerState;
            GameEvents.OnRestorePlayerState -= HandleRestorePlayerState;

            if (GameEvents.EnableDebugLogs) Debug.Log("[GameManager] Event handlers unregistered");
        }

        #region Event Handlers
        private void HandleStartRequest()
        {
            CanvasManager.Instance.ActiveInitialScreen(true);
            CanvasManager.Instance.SetConnecting(true);
            CanvasManager.Instance.ShowLoading(true);
            CanvasManager.Instance.SetAutenticating(true);
        }

        private void HandleStartEndRequest()
        {
            CanvasManager.Instance.ActivateHomeButton(true);

            Credits = 10000;
            Bet = 500;

            CanvasManager.Instance.SetAutenticating(false);
            CanvasManager.Instance.SetConnecting(false);
            CanvasManager.Instance.ShowLoading(false);
            CanvasManager.Instance.UpdateBalanceText(Credits);
            CanvasManager.Instance.UpdateBetText(Bet);
        }
        private void HandleSpinRequest()
        {
            if (playing)
            {
                GameEvents.DenySpin("Already spinning");
                return;
            }

            if (!GameUtils.HasSufficientCredits(credits, bet))
            {
                GameEvents.DenySpin("Insufficient balance");
                GameEvents.ShowMessage("Insufficient Balance", "Please add more credits to continue playing.");
                return;
            }

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Spin request approved. Bet: {GameUtils.FormatCurrency(bet)}");
            GameEvents.ApproveSpin();
        }

        private void HandleSpinApproved()
        {
            playing = true;
            clickPlay = true;
            gain = 0;
            pendingGain = 0;

            // Deduz aposta do saldo
            credits -= bet;

            // Atualiza UI via evento
            GameEvents.UpdateBalance(credits);
            GameEvents.AddGain(0);

            // Desabilita botões da interface durante o spin
            if (CanvasManager.Instance != null)
            {
                CanvasManager.Instance.EnableDisableInterfaceButtons(false, false, false, false);
            }

            // ⭐ Executar o spin no SlotMathRunner
            if (SlotMathRunner.Instance != null)
            {
                //SlotMathRunner.Instance.ExecuteSpin();
            }

            // Inicia a corrotina de espera do spin
            if (showGainsCoroutine != null)
            {
                StopCoroutine(showGainsCoroutine);
            }
            showGainsCoroutine = StartCoroutine(PlayIE());

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Spin started. Bet: {GameUtils.FormatCurrency(bet)}. New balance: {GameUtils.FormatCurrency(credits)}");
        }

        private void HandleSpinDenied(string reason)
        {
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Spin denied: {reason}");
        }

        private void HandleSpinCompleted()
        {
            // O spin foi completado, mas a animação de payout pode ainda estar rodando
            // A reativação dos botões acontece no PlayIE ou no PayoutCompleted
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Spin completed. Total gain: {GameUtils.FormatCurrency(gain)}");

            if (gain == 0 && showGainsCoroutine != null)
            {
                if (GameEvents.EnableDebugLogs) Debug.Log("[GameManager] Sem ganho, finalizando spin imediatamente");
                StopCoroutine(showGainsCoroutine);
                showGainsCoroutine = null;
                FinalizeSpin();
            }
        }

        private void HandleBetChanged(int newBetInCents)
        {
            bet = newBetInCents;

            // Atualiza o índice da aposta atual
            int newIndex = availableBets.IndexOf(bet);
            if (newIndex >= 0)
            {
                currentBetIndex = newIndex;
            }

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Bet changed to: {GameUtils.FormatCurrency(bet)}");
        }

        private void HandleBalanceUpdated(int newBalance)
        {
            credits = newBalance;
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Balance updated to: {GameUtils.FormatCurrency(credits)}");
        }

        private void HandleGain(int amount)
        {
            gain += amount;
            pendingGain += amount;
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Gain accumulated: +{GameUtils.FormatCurrency(amount)} → Total gain: {GameUtils.FormatCurrency(gain)}");
        }

        private void HandlePayout(int amount, int symbolId)
        {
            // Adiciona ao saldo (já foi acumulado via OnGain, mas garantimos)
            credits += amount;
            GameEvents.UpdateBalance(credits);

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Payout processed: {GameUtils.FormatCurrency(amount)}. New balance: {GameUtils.FormatCurrency(credits)}");
        }

        private void HandlePayoutCompleted()
        {
            // Payout finalizado, agora podemos finalizar o spin
            if (showGainsCoroutine == null)
            {
                FinalizeSpin();
            }
        }

        private void HandleAutoSpinRequest(int spins)
        {
            autoSpins = spins;
        }

        private void HandleAutoSpinStarted(int spins)
        {
            autoPlay = true;
            //autoSpins = spins;
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Auto-spin started. Remaining: {autoSpins}");
            WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_play);
            GameEvents.RequestSpin();
            StartCoroutine(CanvasManager.Instance.CreateParticlePlay());
        }

        private void HandleAutoSpinStopped()
        {
            autoPlay = false;
            autoSpins = 0;

            // Atualiza UI do botão auto-spin
            if (CanvasManager.Instance != null && CanvasManager.Instance.autoPlayBtn != null)
            {
                CanvasManager.Instance.autoPlayBtn.isOn = false;
            }

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Auto-spin stopped");
        }

        private void HandleAutoSpinStep()
        {
            if (autoSpins > 0)
            {
                autoSpins--;
                if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Auto-spin step completed. Remaining: {autoSpins}");

                if (autoSpins <= 0)
                {
                    GameEvents.StopAutoSpin();
                }
            }
        }

        private void HandleTurboChanged(bool isEnabled)
        {
            turbo = isEnabled;

            // Sincroniza com a slot machine
            if (WG_SlotMachine.Instance != null)
            {
                WG_SlotMachine.Instance.onSpeedMode = turbo;
            }

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Turbo mode: {(turbo ? "ON" : "OFF")}");
        }

        private void HandleBonusStarted()
        {
            isInBonus = true;
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Bonus started");
        }

        private void HandleBonusEnded(int totalWin)
        {
            isInBonus = false;
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Bonus ended. Total win: {GameUtils.FormatCurrency(totalWin)}");

            // Se estava em auto-spin, continua após o bônus
            if (autoPlay && autoSpins > 0 && !playing)
            {
                GameEvents.ReadyForNext();
            }
        }

        private void HandleReadyForNext()
        {
            if (autoPlay && autoSpins > 0 && !playing && !isInBonus && !onReplay)
            {
                if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Auto-spin: Requesting next spin. {autoSpins} remaining");
                GameEvents.RequestSpin();
            }
        }

        private void HandleMessageShow(string title, string message)
        {
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Message received: [{title}] {message}");
            // O CanvasManager já deve ter mostrado a mensagem via seu próprio handler
        }

        /// <summary>
        /// Salva o estado atual do jogador (chamado pelo HistoryUIManager antes do replay)
        /// </summary>
        private void HandleSavePlayerState(int credits, int bet, int gain)
        {
            // Este handler é chamado quando o HistoryUIManager quer salvar o estado
            // Os valores já estão sendo salvos pelo HistoryUIManager, então aqui só logamos
            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Estado solicitado para salvar - Credits: {credits}, Bet: {bet}, Gain: {gain}");
        }

        /// <summary>
        /// Restaura o estado do jogador (chamado pelo HistoryUIManager após o replay)
        /// </summary>
        private void HandleRestorePlayerState(int credits, int bet, int gain)
        {
            // Restaurar os valores
            this.credits = credits;
            this.bet = bet;
            this.gain = gain;

            // Notificar UI sobre as mudanças
            GameEvents.UpdateBalance(credits);
            GameEvents.ChangeBet(bet);
            GameEvents.AddGain(gain);

            if (GameEvents.EnableDebugLogs) Debug.Log($"[GameManager] Estado restaurado - Credits: {GameUtils.FormatCurrency(credits)}, Bet: {GameUtils.FormatCurrency(bet)}, Gain: {GameUtils.FormatCurrency(gain)}");
        }

        #endregion

        #region Core Game Logic

        private IEnumerator PlayIE()
        {
            yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(0.1f);

            if (gameID == 0)
            {
                yield return new WaitForSeconds(0.2f);
                started = false;
                if (gameID == 1 || gameID == 2)
                {
                    yield break;
                }
            }

            yield return new WaitForSeconds(1f);

            // Aguarda todos os componentes finalizarem
            yield return new WaitUntil(() =>
                WG_SlotMachine.Instance != null &&
                WG_SlotMachine.Instance.AreAllReelsCompleted() &&
                BonusManager.Instance != null &&
                !BonusManager.Instance.isAnimating &&
                PayoutAnimation.Instance != null &&
                !PayoutAnimation.Instance.isPayout);

            FinalizeSpin();
        }

        private void FinalizeSpin()
        {
            clickPlay = false;
            playing = false;

            // Reabilita os botões da interface
            if (CanvasManager.Instance != null)
            {
                CanvasManager.Instance.EnableDisableInterfaceButtons();
            }

            // Dispara evento de spin completado
            GameEvents.CompleteSpin();

            showGainsCoroutine = null;
        }

        public void Play()
        {
            if (showGainsCoroutine != null)
            {
                StopCoroutine(showGainsCoroutine);
                showGainsCoroutine = null;
            }

            if (!clickPlay && !playing && !onReplay)
            {
                GameEvents.RequestSpin();
            }
            else if (playing && !clickPlay && WG_SlotMachine.Instance != null && !WG_SlotMachine.Instance.AreAllReelsCompleted())
            {
                // Força parada se estiver girando
                WG_SlotMachine.Instance.ForceStop();
            }
        }

        #endregion
    }
}
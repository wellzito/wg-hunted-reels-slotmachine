using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WG_Casino.Setup;
using WG_Casino.SlotMachine;
using WG_Casino.Systems;
using WG_Casino;

namespace SlotMachineMath
{
    public class SlotMathRunner : MonoBehaviour
    {
        public static SlotMathRunner Instance;

        [Header("Configurações")]
        [SerializeField] private float totalBet = 1f;
        [SerializeField] private bool autoSpin = false;
        [SerializeField] private int autoSpinCount = 10;
        [SerializeField] private float autoSpinDelay = 1f;

        [Header("Debug - Padrões Vencedores")]
        [SerializeField] private List<WinningPatternInfo> oldWinningPatterns = new List<WinningPatternInfo>();
        [SerializeField] private List<WinningPatternInfo> lastWinningPatterns = new List<WinningPatternInfo>();
        [SerializeField] private int lastSpinNumber = 0;
        [SerializeField] private float lastWinAmount = 0f;

        private SlotMathCalculator calculator;
        private float balance = 1000f;
        private int totalSpins = 0;
        private bool isAutoSpinning = false;
        private WG_SlotPaymentSystem paymentSystem;
        private int autoSpinCountCur = 0;

        public List<WinningPatternInfo> GetLastWinningPatterns() => lastWinningPatterns;

        bool isComplete = true;

        public List<WinningPatternInfo> SetLastWinningPatterns
        {
            set { lastWinningPatterns = value; }
        }

        private void Awake()
        {
            Instance = this;
            isComplete = true;
        }

        void Start()
        {
            InitializeCalculator();
            //Debug.Log("🎰 SlotMathCalculator Inicializado!");
            //Debug.Log($"💰 Saldo: {balance:F2} | Bet: {totalBet:F2}");

            GameEvents.OnBalanceUpdated += HandleBalanceUpdated;
            GameEvents.OnBetChanged += HandleBetChanged;

            GameEvents.OnSpinRequested += HandleSpinRequest;
            GameEvents.OnSpinCompleted += HandleSpinCompleted;

            // Auto-Spin - Gerencia estado de auto-spin
            GameEvents.OnAutoSpinRequest += HandleAutoSpinRequest;
            GameEvents.OnAutoSpinStopped += HandleAutoSpinStopped;
        }

        private void OnDestroy()
        {
            GameEvents.OnBalanceUpdated -= HandleBalanceUpdated;
            GameEvents.OnBetChanged -= HandleBetChanged;

            GameEvents.OnSpinRequested -= HandleSpinRequest;
            GameEvents.OnSpinCompleted -= HandleSpinCompleted;

            // Auto-Spin - Gerencia estado de auto-spin
            GameEvents.OnAutoSpinRequest -= HandleAutoSpinRequest;
            GameEvents.OnAutoSpinStopped -= HandleAutoSpinStopped;
        }

        private void HandleBalanceUpdated(int balanceInCents)
        {
            // CONVERTE centavos para REAIS (float)
            balance = balanceInCents / 100f;
            if (GameEvents.EnableDebugLogs) Debug.Log($"[SlotMathRunner] Balance atualizado: R$ {balance:F2}");
        }

        private void HandleBetChanged(int betInCents)
        {
            // CONVERTE centavos para REAIS (float)
            totalBet = betInCents / 100f;
            if (calculator != null)
            {
                calculator.SetTotalBet(totalBet);
            }
          if(GameEvents.EnableDebugLogs)  Debug.Log($"[SlotMathRunner] Bet atualizado: R$ {totalBet:F2}");
        }

        private void HandleSpinCompleted()
        {
            isComplete = true;
        }

        private void HandleSpinRequest()
        {
            isComplete = false;
        }

        private void HandleAutoSpinStopped()
        {
            StopAutoSpin();
        }

        private void HandleAutoSpinRequest(int spine)
        {
            autoSpinCount = spine;
            StartAutoSpin();
        }

        private void InitializeCalculator()
        {
            // Busca o WG_SlotPaymentSystem
            paymentSystem = FindObjectOfType<WG_SlotPaymentSystem>();

            if (paymentSystem == null)
            {
                Debug.LogError("[SlotMathRunner] WG_SlotPaymentSystem não encontrado na cena!");
                calculator = new SlotMathCalculator(totalBet, null);
                return;
            }

            // Busca o paymentConfig e seus padrões
            if (paymentSystem.paymentConfig == null)
            {
                Debug.LogError("[SlotMathRunner] paymentConfig não atribuído no WG_SlotPaymentSystem!");
                calculator = new SlotMathCalculator(totalBet, null);
                return;
            }

            // Carrega os padrões do paymentConfig
            var patterns = paymentSystem.paymentConfig.paymentPatterns;

            if (patterns == null || patterns.Count == 0)
            {
                Debug.LogWarning("[SlotMathRunner] Nenhum padrão encontrado no paymentConfig!");
                calculator = new SlotMathCalculator(totalBet, null);
            }
            else
            {
                //Debug.Log($"[SlotMathRunner] Carregados {patterns.Count} padrões do paymentConfig");
                calculator = new SlotMathCalculator(totalBet, patterns);
            }
        }

        // Método público para recarregar padrões em tempo real
        public void ReloadPatterns()
        {
            if (paymentSystem != null && paymentSystem.paymentConfig != null)
            {
                var patterns = paymentSystem.paymentConfig.paymentPatterns;
                if (calculator != null && patterns != null)
                {
                    calculator.LoadPatternsFromList(patterns);
                    //Debug.Log($"[SlotMathRunner] Padrões recarregados! Total: {patterns.Count}");
                }
            }
            else
            {
                InitializeCalculator();
            }
        }

        public void ReturnOldWin()
        {
            lastWinningPatterns.Clear();
            foreach (var pattern in oldWinningPatterns)
            {
                var copy = new WinningPatternInfo
                {
                    patternId = pattern.patternId,
                    patternName = pattern.patternName,
                    symbolId = pattern.symbolId,
                    symbolName = pattern.symbolName,
                    payout = pattern.payout,
                    positions = new List<PatternPosition>()
                };

                foreach (var pos in pattern.positions)
                {
                    copy.positions.Add(new PatternPosition(pos.row, pos.column));
                }
                lastWinningPatterns.Add(copy);
            }
        }

        public void ManualSpin()
        {
            if (balance < totalBet)
            {
                Debug.LogWarning("❌ Saldo insuficiente!");
                return;
            }
            PerformSpin();
        }

        void StartAutoSpin()
        {
            isAutoSpinning = true;
            Debug.Log($"🔄 Auto Spin iniciado - {autoSpinCount} giros");
            autoSpinCountCur = 0;
            StartCoroutine(AutoSpinRoutine());
        }

        void StopAutoSpin()
        {
            isAutoSpinning = false;
            if (GameEvents.EnableDebugLogs) Debug.Log("🛑 Auto Spin interrompido");
        }

        IEnumerator AutoSpinRoutine()
        {
            yield return new WaitUntil(() => isComplete);
            for (int i = 0; i < autoSpinCount && isAutoSpinning; i++)
            {
                if (balance < totalBet)
                {
                    Debug.LogWarning("❌ Saldo insuficiente! Parando auto spin.");
                    break;
                }

                if (isComplete)
                {
                    autoSpinCountCur++;
                    //PerformSpin();
                    ManualSpin();
                    GameEvents.AutoSpinStep();
                    GameEvents.StartAutoSpin(autoSpinCount);
                }
                yield return new WaitForSeconds(autoSpinDelay);
            }
            isAutoSpinning = false;
            GameEvents.StopAutoSpin();
            if (GameEvents.EnableDebugLogs) Debug.Log("✅ Auto Spin finalizado");
        }

        void PerformSpin()
        {
            balance -= totalBet;
            totalSpins++;
            /*
            Debug.Log($"\n═══════════════════════════════════════");
            Debug.Log($"🎲 SPIN #{totalSpins} - Bet: {totalBet:F2} - Saldo atual: {balance:F2}");
            Debug.Log($"═══════════════════════════════════════");
            */
            PlayResponse response = calculator.GeneratePlayResponse((int)(balance * 100));

            UpdateDisplayList(response);
            calculator.DebugWinningPositions();
            SlotMathCalculator.LogPlayResponse(response, totalBet);

            float winAmount = response.rewards / 100f;
            balance += winAmount;

            if (GameEvents.EnableDebugLogs) Debug.Log($"💰 Novo Saldo: {balance:F2} (Ganhou: {winAmount:F2})\n");

            SlotClient.Instance.PlayResponse(response);
        }

        void UpdateDisplayList(PlayResponse response)
        {
            lastWinningPatterns.Clear();
            oldWinningPatterns.Clear();

            foreach (var pattern in calculator.winningPositionsByPattern)
            {
                var copy = new WinningPatternInfo
                {
                    patternId = pattern.patternId,
                    patternName = pattern.patternName,
                    symbolId = pattern.symbolId,
                    symbolName = pattern.symbolName,
                    payout = pattern.payout,
                    positions = new List<PatternPosition>()
                };

                foreach (var pos in pattern.positions)
                {
                    copy.positions.Add(new PatternPosition(pos.row, pos.column));
                }
                lastWinningPatterns.Add(copy);
            }

            oldWinningPatterns.AddRange(lastWinningPatterns);
            lastSpinNumber = totalSpins;
            lastWinAmount = response.rewards / 100f;
        }

        void DebugWinningPatterns()
        {
            if (lastWinningPatterns.Count == 0)
            {
                if (GameEvents.EnableDebugLogs) Debug.Log("📊 Nenhum padrão vencedor registrado ainda. Faça alguns spins primeiro!");
                return;
            }

            if (GameEvents.EnableDebugLogs)
            {
                Debug.Log("\n═══════════════════════════════════════");
                Debug.Log($"📊 ÚLTIMOS PADRÕES VENCEDORES (Spin #{lastSpinNumber}):");
                Debug.Log($"🏆 Total Ganho: {lastWinAmount:F2}");
                Debug.Log("═══════════════════════════════════════");
            }
            for (int i = 0; i < lastWinningPatterns.Count; i++)
            {
                var pattern = lastWinningPatterns[i];
                string positionsStr = string.Join(", ", pattern.positions.Select(p => $"({p.row},{p.column})"));

                if (GameEvents.EnableDebugLogs)
                {
                    Debug.Log($"🎯 PADRÃO {pattern.patternId}: {pattern.patternName}");
                    Debug.Log($"   Símbolo: {pattern.symbolName} (ID: {pattern.symbolId})");
                    Debug.Log($"   Pagamento: {pattern.payout:F2}");
                    Debug.Log($"   Posições: {positionsStr}");
                }
                Debug.Log("");
            }
            if (GameEvents.EnableDebugLogs) Debug.Log("═══════════════════════════════════════\n");
        }

        void Update()
        {
            //totalBet = GameManager.Instance.Bet / 100;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ManualSpin();
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                if (isAutoSpinning)
                    StopAutoSpin();
                else
                    StartAutoSpin();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                DebugWinningPatterns();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ReloadPatterns();
                if (GameEvents.EnableDebugLogs) Debug.Log("🔄 Padrões recarregados manualmente!");
            }
        }

        [ContextMenu("Reload Patterns")]
        void ReloadPatternsMenu()
        {
            ReloadPatterns();
        }
    }
}
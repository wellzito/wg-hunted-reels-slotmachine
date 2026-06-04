using SlotMachineMath;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WG_Casino.SlotMachine;

namespace WG_Casino
{
    public class SlotClient : MonoBehaviour
    {
        public static SlotClient Instance;
        public PlayResponse playResponse;
        public BonusResponse bonusResponse;

        // Sistema de Histórico
        public List<SerializableLastPlay> GameHistory = new List<SerializableLastPlay>();
        public event Action<SerializableLastPlay> OnNewHistoryAdded;
        public event Action OnHistoryUpdated;
        public int match;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        { 
            StartCoroutine(StartCanvas());
        }

        IEnumerator StartCanvas()
        {
            GameEvents.RequestStart();
            yield return new WaitForSeconds(1f);
            GameEvents.RequestStartEnd();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                PlayResponse(playResponse);
            }
        }

        public void PlayResponse(PlayResponse msg)
        {
            playResponse = null;
            //  isProcessingPlayResponse = false;
            WG_SlotMachine.Instance.BetPlayBtn();
            playResponse = msg;
            /*Debug.Log($"[SERVER] PlayResponse received - Rewards: {msg.rewards}");
            Debug.Log($"[SERVER] Reel structure: {msg.reel}");*/

            if (msg.rewards != 0) PayoutAnimation.Instance.isPayout = true;

            // VERIFICAR SE É UMA NOVA JOGADA OU CONTINUAÇÃO DE BÔNUS
            bool isBonusContinuation = false;

            if (!GameManager.Instance.onReplay)
            {
                if (GameHistory.Count > 0)
                {
                    var lastPlay = GameHistory[0];
                    // Se o último item tem bônus E o bônus não terminou, é continuação
                    if (lastPlay.playItem.hasBonusSpins && lastPlay.playItem.bonusSpins.Count > 0)
                    {
                        var lastBonusSpin = lastPlay.playItem.bonusSpins[lastPlay.playItem.bonusSpins.Count - 1];
                        if (!lastBonusSpin.bonusOver && msg.gameState == GameState.Bonus)
                        {
                            isBonusContinuation = true;
                            // ATUALIZAR o item existente com o novo spin
                            UpdateExistingBonusWithPlayResponse(msg, lastPlay);
                        }
                    }
                }

                // Se NÃO for continuação de bônus, criar NOVO item
                if (!isBonusContinuation)
                {
                    var historyEntry = CreateHistoryFromPlayResponse(msg, msg.playerBalanceWithoutRewards + msg.rewards, GameManager.Instance.Bet);
                    AddPlayToHistory(historyEntry);
                    //Debug.Log($"[HISTORY] New play added to history: MatchID {historyEntry.matchid}");
                }
            }

            string msdDebug = JsonUtility.ToJson(msg);
            //Debug.Log(msdDebug);

            string currentColor = "#FFFFFF";
            string aposta = "#FF0000";
            string ganho = "#00FF00";
            string newBalanceColor = "#FFFF00";

            string debugMessage =
    $"<color={currentColor}>PlayResponse Balance: Saldo Atual = {GameUtils.FormatCurrency(msg.playerBalanceWithoutRewards)}</color> \n" +
    //$"<color={currentColor}>{("BalanceResponse")} = {Math.Abs(GameManager.Instance.Credits):C2}</color>\n" +
    $"<color={aposta}>{("Aposta")} = {Math.Abs(GameManager.Instance.Bet):C2}</color> => " +
    $"<color={ganho}>{("Ganho")} = {GameUtils.FormatCurrency(playResponse.rewards)}</color> => " +
    $"<color={newBalanceColor}>Novo Saldo = {GameUtils.FormatCurrency(msg.playerBalanceWithoutRewards + msg.rewards)}</color>";

#if UNITY_EDITOR
            Debug.Log(debugMessage);
#endif

            // Log detalhado do reel
            if (msg.reel?.rows != null)
            {
                //Debug.Log($"[SERVER] Reel Rows Count: {msg.reel.rows.Length} | Lines: {WG_SlotMachine.Instance.lineRows.Count}");

                // CONVERSÃO: De linhas para colunas/reels
                Row[] convertedReels = ConvertRowsToReels(msg.reel.rows);


                for (int lineIndex = 0; lineIndex < WG_SlotMachine.Instance.lineRows.Count && lineIndex < msg.reel.rows.Length; lineIndex++)
                {
                    var lineRow = WG_SlotMachine.Instance.lineRows[lineIndex];
                    var rowData = msg.reel.rows[lineIndex];

                    //Debug.Log($"[CLIENT] Line {lineIndex} recebendo row: [{string.Join(", ", rowData.elements)}]");

                    WG_SlotMachine.Instance.SetPaymentPlayResponseRow(rowData, lineRow);
                }
            }
            else
            {
                Debug.Log("[SERVER] Reel or rows is null!");
            }

            // Atualizar o saldo do jogador
            //CanvasManager.Instance.UpdateBalanceText(msg.playerBalanceWithoutRewards);

            //GameManager.Instance.Credits = msg.playerBalanceWithoutRewards + msg.rewards;// / 100.0;
            GameEvents.UpdateBalance(GameManager.Instance.Credits);
            if (msg.rewards > 0)
            {
                GameEvents.AddGain(msg.rewards);
            }
        }

        private void UpdateExistingBonusWithPlayResponse(PlayResponse response, SerializableLastPlay existingPlay)
        {
            // Atualizar saldo final
            existingPlay.afterBet = response.playerBalanceWithoutRewards + response.rewards;

            // Atualizar recompensa total do item principal
            existingPlay.playItem.rewards = response.rewards;

            // Adicionar este spin à lista de bonusSpins
            var bonusSpinData = CreateBonusSpinDataFromResponse(response);
            existingPlay.playItem.bonusSpins.Add(bonusSpinData);

            Debug.Log($"[HISTORY] Added bonus spin to existing play. Total spins: {existingPlay.playItem.bonusSpins.Count}");

            OnHistoryUpdated?.Invoke();

            if (existingPlay.playItem.bonusSpins.Count > 0)
            {
                if (existingPlay.playItem.bonusSpins[0].bonusMultipliers == null || existingPlay.playItem.bonusSpins[0].bonusMultipliers.Count == 0)
                {
                    existingPlay.playItem.bonusSpins.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Cria uma entrada de histórico a partir de uma PlayResponse
        /// </summary>
        private SerializableLastPlay CreateHistoryFromPlayResponse(PlayResponse response, int currentBalance, int betValue)
        {
            match++;

            // CRIA UMA CÓPIA PROFUNDA dos padrões vencedores
            List<WinningPatternInfo> copiedPatterns = new List<WinningPatternInfo>();
            var originalPatterns = SlotMathRunner.Instance.GetLastWinningPatterns();

            foreach (var pattern in originalPatterns)
            {
                var copiedPattern = new WinningPatternInfo
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
                    copiedPattern.positions.Add(new PatternPosition(pos.row, pos.column));
                }

                copiedPatterns.Add(copiedPattern);
            }

            var historyEntry = new SerializableLastPlay
            {
                matchid = response.matchId > 0 ? response.matchId : match,
                userid = 1,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                beforeBet = response.playerBalanceWithoutRewards,
                afterBet = response.playerBalanceWithoutRewards + response.rewards,
                playItem = new SerializableLastPlayItem
                {
                    betIndex = (byte)GameManager.Instance.GetBetIndex(),
                    betValue = betValue,
                    rewards = response.rewards,
                    gameState = (byte)response.gameState,
                    reel = response.reel,
                    hasWays = response.waysSummary != null && response.waysSummary.total > 0,
                    ways = response.waysSummary ?? new WaysSummaryDto(),
                    hasDefaultEvent = false,
                    hasBonusSpins = response.gameState == GameState.Bonus,
                    bonusSpins = response.gameState == GameState.Bonus ?
                        new List<BonusSpinData> { CreateBonusSpinDataFromResponse(response) } :
                        new List<BonusSpinData>(),
                    lastWinningPatterns = copiedPatterns
                }
            };
            if (historyEntry.playItem.bonusSpins.Count > 0)
            {
                if (historyEntry.playItem.bonusSpins[0].bonusMultipliers == null || historyEntry.playItem.bonusSpins[0].bonusMultipliers.Count == 0)
                {
                    historyEntry.playItem.bonusSpins.RemoveAt(0);
                }
            }

            return historyEntry;
        }

        /// <summary>
        /// Adiciona uma nova jogada ao histórico em tempo real
        /// </summary>
        public void AddPlayToHistory(SerializableLastPlay play)
        {
            if (play != null)
            {
                // Inserir no início da lista (mais recente primeiro)
                GameHistory.Insert(0, play);

                // Aplicar limite de 50 itens
                LimitHistorySize();

                /*Debug.Log($"[HISTORY] Added play to history: MatchID {play.matchid}, " +
                         $"Bet: {play.playItem.betValue}, Reward: {play.playItem.rewards}");*/

                // Notificar listeners
                OnNewHistoryAdded?.Invoke(play);
                OnHistoryUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Limita o histórico a 50 itens, removendo os mais antigos
        /// </summary>
        private void LimitHistorySize()
        {
            const int MAX_HISTORY = 50;

            if (GameHistory.Count > MAX_HISTORY)
            {
                int itemsToRemove = GameHistory.Count - MAX_HISTORY;
                GameHistory.RemoveRange(MAX_HISTORY, itemsToRemove);
                Debug.Log($"[HISTORY] Removed {itemsToRemove} old items. Current count: {GameHistory.Count}");
            }
        }

        /// <summary>
        /// Cria BonusSpinData a partir de PlayResponse (para bônus)
        /// </summary>
        private BonusSpinData CreateBonusSpinDataFromResponse(PlayResponse response)
        {
            return new BonusSpinData
            {
                rewards = response.rewards,
                freespinsRemaining = 0, // Será preenchido pelo servidor se necessário
                bonusMultipliers = new List<int>(),
                bonusGrid = ConvertReelDtoToGrid(response.reel),
                bonusTentacles = new List<BonusTentacleDto>(),
                bonusOver = false
            };
        }

        /// <summary>
        /// Converte ReelDto para grid (List<List<byte>>)
        /// </summary>
        private List<List<byte>> ConvertReelDtoToGrid(ReelDto reel)
        {
            var grid = new List<List<byte>>();
            if (reel?.rows != null)
            {
                foreach (var row in reel.rows)
                {
                    if (row?.elements != null)
                    {
                        grid.Add(new List<byte>(row.elements));
                    }
                }
            }
            return grid;
        }

        private Row[] ConvertRowsToReels(Row[] rows)
        {
            if (rows == null || rows.Length == 0)
                return null;

            // Determinar o número de reels (colunas) baseado no primeiro elemento
            int numReels = rows[0]?.elements?.Length ?? 0;

            if (numReels == 0)
                return null;

            Row[] reels = new Row[numReels];

            for (int reelIndex = 0; reelIndex < numReels; reelIndex++)
            {
                // Cada reel terá um número de elementos igual ao número de linhas
                byte[] reelElements = new byte[rows.Length];

                for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    //Debug.Log($"[ConvertRowsToReels] Processing Reel Index: {reelIndex} with {rows[rowIndex]} rows");

                    // Pega o elemento da coluna 'reelIndex' na linha 'rowIndex'
                    if (rows[rowIndex]?.elements != null && reelIndex < rows[rowIndex].elements.Length)
                    {
                        reelElements[rowIndex] = rows[rowIndex].elements[reelIndex];
                    }
                    else
                    {
                        reelElements[rowIndex] = 0; // Valor padrão se não existir
                    }
                }

                reels[reelIndex] = new Row { elements = reelElements };
            }

            return reels;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WG_Casino.Setup;

namespace SlotMachineMath
{
    [System.Serializable]
    public class WinningPatternInfo
    {
        public int patternId;
        public string patternName;
        public int symbolId;
        public string symbolName;
        public float payout;
        public int matchCount;
        public int regularCount;
        public List<PatternPosition> positions;

        public WinningPatternInfo()
        {
            positions = new List<PatternPosition>();
        }
    }

    [System.Serializable]
    public class PatternPosition
    {
        public int row;
        public int column;

        public PatternPosition(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

        public override string ToString() => $"({row},{column})";
    }

    /// <summary>
    /// Configuração do grid para o slot machine
    /// </summary>
    [System.Serializable]
    public class GridConfig
    {
        public int rows = 3;
        public int columns = 5;

        public GridConfig(int rows = 3, int columns = 5)
        {
            this.rows = rows;
            this.columns = columns;
        }
    }

    /// <summary>
    /// Modo de operação do SlotMathCalculator
    /// </summary>
    public enum SlotMathMode
    {
        Internal,   // Gera os números internamente (modo automático)
        External    // Recebe dados externos via PlayResponse (modo API)
    }

    public class SlotMathCalculator
    {
        private List<PaymentPattern> paymentPatterns;
        private Dictionary<int, SymbolData> symbolDict;
        private List<SymbolData> symbolList;
        private float lineBet;
        private System.Random rng;
        private int totalWeight;
        private int currentMatchId = 1000;
        private int winRate = 50;
        private GridConfig gridConfig;
        private SlotMathMode mode = SlotMathMode.Internal;

        public List<WinningPatternInfo> winningPositionsByPattern = new List<WinningPatternInfo>();

        private const int WILD_SYMBOL_ID = 7;
        private const int MAX_WILD_PER_PATTERN = 2;

        // ============ CONSTRUTORES ============

        /// <summary>
        /// Construtor para modo INTERNO (geração automática)
        /// </summary>
        public SlotMathCalculator(float totalBet = 1f, List<PaymentPattern> patterns = null, GridConfig gridConfig = null)
        {
            this.mode = SlotMathMode.Internal;
            this.gridConfig = gridConfig ?? new GridConfig(3, 5);
            this.paymentPatterns = patterns ?? new List<PaymentPattern>();

            InitializeSymbols();
            SetTotalBet(totalBet);
            this.rng = new System.Random(Guid.NewGuid().GetHashCode());
            CalculateTotalWeight();
            winningPositionsByPattern.Clear();
            ValidateAndSortPatterns();
        }

        /// <summary>
        /// Construtor para modo EXTERNO (API) - recebe PlayResponse pré-definido
        /// </summary>
        public SlotMathCalculator(GridConfig gridConfig = null)
        {
            this.mode = SlotMathMode.External;
            this.gridConfig = gridConfig ?? new GridConfig(3, 5);
            InitializeSymbols();
            winningPositionsByPattern.Clear();
        }

        // ============ CONFIGURAÇÃO ============

        public void SetMode(SlotMathMode newMode)
        {
            this.mode = newMode;
        }

        public SlotMathMode GetMode() => mode;

        public void SetGridConfig(int rows, int columns)
        {
            this.gridConfig = new GridConfig(rows, columns);
        }

        public GridConfig GetGridConfig() => gridConfig;

        private void ValidateAndSortPatterns()
        {
            if (paymentPatterns == null) return;

            for (int i = 0; i < paymentPatterns.Count; i++)
            {
                var pattern = paymentPatterns[i];
                if (pattern.patternId == 0)
                    pattern.patternId = i + 1;
                if (string.IsNullOrEmpty(pattern.patternName))
                    pattern.patternName = $"Pattern {i + 1}";
            }
        }

        public void LoadPatternsFromList(List<PaymentPattern> patterns)
        {
            paymentPatterns = patterns ?? new List<PaymentPattern>();
            ValidateAndSortPatterns();
            Debug.Log($"[Math] Recarregados {paymentPatterns.Count} padrões");
        }

        public void SetTotalBet(float totalBet)
        {
            this.lineBet = totalBet / 10f;
        }

        private void CalculateTotalWeight()
        {
            totalWeight = 0;
            foreach (var symbol in symbolList)
            {
                totalWeight += symbol.weight;
            }
        }

        private void InitializeSymbols()
        {
            symbolDict = new Dictionary<int, SymbolData>();
            symbolList = new List<SymbolData>();

            symbolList.Add(new SymbolData(1, "Bruxa", "🧙", 150f, 25f, 5f, 40));
            symbolList.Add(new SymbolData(2, "Abóbora", "🎃", 100f, 15f, 4f, 45));
            symbolList.Add(new SymbolData(3, "Caveira", "💀", 60f, 10f, 3f, 50));
            symbolList.Add(new SymbolData(4, "Morcego", "🦇", 30f, 8f, 2f, 55));
            symbolList.Add(new SymbolData(5, "Aranha", "🕷️", 20f, 6f, 1.5f, 60));
            symbolList.Add(new SymbolData(6, "Poção", "🧪", 15f, 4f, 1f, 65));
            symbolList.Add(new SymbolData(7, "Wild", "⭐", 0f, 0f, 0f, 8));

            foreach (var s in symbolList)
            {
                symbolDict[s.id] = s;
            }
        }

        // ============ GERAÇÃO DE GRID (MODO INTERNO) ============

        private int GenerateRandomSymbol()
        {
            int roll = rng.Next(0, totalWeight);
            int cumulative = 0;

            foreach (var symbol in symbolList)
            {
                cumulative += symbol.weight;
                if (roll < cumulative)
                    return symbol.id;
            }
            return 1;
        }

        private int[,] GenerateEmptyGrid()
        {
            return new int[gridConfig.rows, gridConfig.columns];
        }

        private int[,] GenerateRandomGrid()
        {
            int[,] grid = GenerateEmptyGrid();
            for (int row = 0; row < gridConfig.rows; row++)
            {
                for (int col = 0; col < gridConfig.columns; col++)
                {
                    grid[row, col] = GenerateRandomSymbol();
                }
            }
            return grid;
        }

        private int[,] GenerateWinningGrid()
        {
            int[,] grid = GenerateRandomGrid();

            if (paymentPatterns == null || paymentPatterns.Count == 0)
                return grid;

            var validPatterns = paymentPatterns.Where(p => p.selectedSlots != null && p.selectedSlots.Count > 0).ToList();

            if (validPatterns.Count == 0)
                return grid;

            var winningPattern = validPatterns[rng.Next(0, validPatterns.Count)];
            int mainSymbol = rng.Next(1, 7);

            int wildCount = 0;
            if (winningPattern.selectedSlots.Count >= 5)
            {
                wildCount = rng.Next(0, MAX_WILD_PER_PATTERN + 1);
                if (wildCount > MAX_WILD_PER_PATTERN)
                    wildCount = MAX_WILD_PER_PATTERN;
                if (wildCount > winningPattern.selectedSlots.Count - 3)
                    wildCount = Math.Min(wildCount, winningPattern.selectedSlots.Count - 3);
            }

            int regularNeeded = winningPattern.selectedSlots.Count - wildCount;
            var shuffledSlots = winningPattern.selectedSlots.OrderBy(x => rng.Next()).ToList();

            int normalPlaced = 0;
            int wildPlaced = 0;

            foreach (var slot in shuffledSlots)
            {
                if (slot.rowIndex >= 0 && slot.rowIndex < gridConfig.rows &&
                    slot.reelIndex >= 0 && slot.reelIndex < gridConfig.columns)
                {
                    if (wildPlaced < wildCount)
                    {
                        grid[slot.rowIndex, slot.reelIndex] = WILD_SYMBOL_ID;
                        wildPlaced++;
                    }
                    else if (normalPlaced < regularNeeded)
                    {
                        grid[slot.rowIndex, slot.reelIndex] = mainSymbol;
                        normalPlaced++;
                    }
                }
            }

            return grid;
        }

        // ============ AVALIAÇÃO DE GRID (COMUM PARA AMBOS OS MODOS) ============

        private int CountMatchingSymbolsInPattern(int[,] grid, PaymentPattern pattern, out int mainSymbolId, out List<Vector2Int> positions, out int wildCount)
        {
            mainSymbolId = -1;
            positions = new List<Vector2Int>();
            wildCount = 0;

            if (pattern.selectedSlots == null || pattern.selectedSlots.Count == 0)
                return 0;

            int regularCount = 0;
            int firstRegularSymbol = -1;

            foreach (var slot in pattern.selectedSlots)
            {
                if (slot.rowIndex < 0 || slot.rowIndex >= gridConfig.rows ||
                    slot.reelIndex < 0 || slot.reelIndex >= gridConfig.columns)
                    continue;

                int currentSymbol = grid[slot.rowIndex, slot.reelIndex];
                positions.Add(new Vector2Int(slot.rowIndex, slot.reelIndex));

                if (currentSymbol == WILD_SYMBOL_ID)
                {
                    wildCount++;
                }
                else
                {
                    regularCount++;
                    if (firstRegularSymbol == -1)
                        firstRegularSymbol = currentSymbol;
                    else if (firstRegularSymbol != currentSymbol)
                    {
                        return 0;
                    }
                }
            }

            if (regularCount < 3) return 0;
            if (wildCount > MAX_WILD_PER_PATTERN) return 0;

            mainSymbolId = firstRegularSymbol;
            return regularCount + wildCount;
        }

        private float CalculatePayout(int symbolId, int matchCount)
        {
            var symbol = GetSymbol(symbolId);
            if (symbol == null) return 0;
            if (symbolId == WILD_SYMBOL_ID) return 0;

            float multiplier = 0;
            switch (matchCount)
            {
                case 5: multiplier = symbol.payout5; break;
                case 4: multiplier = symbol.payout4; break;
                case 3: multiplier = symbol.payout3; break;
                default: return 0;
            }

            return multiplier * lineBet;
        }

        private SpinResult EvaluateSpin(int[,] grid)
        {
            var result = new SpinResult();
            result.grid = grid;
            result.totalWin = 0;
            result.lineWins = new List<PaylineWinResult>();
            winningPositionsByPattern.Clear();

            if (paymentPatterns == null || paymentPatterns.Count == 0)
            {
                Debug.Log("[Math] Sem padrões para avaliar");
                return result;
            }

            // Log do grid
            for (int row = 0; row < gridConfig.rows; row++)
            {
                string line = "";
                for (int col = 0; col < gridConfig.columns; col++)
                {
                    int val = grid[row, col];
                    line += (val == WILD_SYMBOL_ID ? "⭐" : val.ToString()) + " ";
                }
                Debug.Log(line);
            }

            foreach (var pattern in paymentPatterns)
            {
                if (pattern.selectedSlots == null || pattern.selectedSlots.Count == 0)
                    continue;

                int matchCount = CountMatchingSymbolsInPattern(grid, pattern, out int mainSymbolId, out List<Vector2Int> matchedPositions, out int wildCount);

                if (matchCount >= 3 && mainSymbolId != -1 && mainSymbolId != WILD_SYMBOL_ID)
                {
                    var symbol = GetSymbol(mainSymbolId);
                    if (symbol != null)
                    {
                        float payoutAmount = CalculatePayout(mainSymbolId, matchCount);

                        if (payoutAmount > 0)
                        {
                            var lineWin = new PaylineWinResult();
                            lineWin.paylineId = pattern.patternId;
                            lineWin.paylineName = pattern.patternName;
                            lineWin.symbolId = mainSymbolId;
                            lineWin.symbolName = symbol.name;
                            lineWin.consecutiveCount = matchCount;
                            lineWin.payout = payoutAmount;
                            lineWin.positions = matchedPositions;

                            result.lineWins.Add(lineWin);
                            result.totalWin += payoutAmount;

                            var winningInfo = new WinningPatternInfo();
                            winningInfo.patternId = pattern.patternId;
                            winningInfo.patternName = pattern.patternName;
                            winningInfo.symbolId = mainSymbolId;
                            winningInfo.symbolName = symbol.name;
                            winningInfo.payout = payoutAmount;
                            winningInfo.matchCount = matchCount;
                            winningInfo.regularCount = matchCount - wildCount;

                            foreach (var pos in matchedPositions)
                            {
                                winningInfo.positions.Add(new PatternPosition(pos.x, pos.y));
                            }

                            winningPositionsByPattern.Add(winningInfo);

                            Debug.Log($"✅ PADRÃO VENCEDOR: {pattern.patternName}");
                            Debug.Log($"   Símbolo: {symbol.name} (ID: {mainSymbolId})");
                            Debug.Log($"   Normais: {matchCount - wildCount}, Wilds: {wildCount}");
                            Debug.Log($"   Pagamento: {payoutAmount:F2}");
                        }
                    }
                }
            }

            Debug.Log($"[Math] Total ganho: {result.totalWin:F2}");
            return result;
        }

        private SymbolData GetSymbol(int id)
        {
            return symbolDict.ContainsKey(id) ? symbolDict[id] : null;
        }

        public void DebugWinningPositions()
        {
            Debug.Log($"═══════════════════════════════════════");
            Debug.Log($"📊 PADRÕES VENCEDORES ({winningPositionsByPattern.Count}):");

            foreach (var win in winningPositionsByPattern)
            {
                Debug.Log($"🎯 {win.patternName}: {win.symbolName} - Pagamento: {win.payout:F2}");
            }
        }

        // ============ MÉTODO PRINCIPAL - GERA PLAYRESPONSE (MODO INTERNO) ============

        public PlayResponse GeneratePlayResponse(int currentBalance)
        {
            if (mode != SlotMathMode.Internal)
            {
                Debug.LogError("[SlotMathCalculator] Modo não é INTERNAL. Use ProcessExternalPlayResponse para dados externos.");
                return null;
            }

            currentMatchId++;

            int shouldWin = rng.Next(0, 100);
            int[,] grid;

            bool hasValidPatterns = paymentPatterns != null && paymentPatterns.Count > 0 &&
                                    paymentPatterns.Any(p => p.selectedSlots != null && p.selectedSlots.Count > 0);

            if (shouldWin < winRate && hasValidPatterns)
            {
                grid = GenerateWinningGrid();
                Debug.Log($"🎲 FORÇANDO VITÓRIA (winRate: {winRate}%)");
            }
            else
            {
                grid = GenerateRandomGrid();
                Debug.Log($"🎲 GIRO ALEATÓRIO");
            }

            var winResult = EvaluateSpin(grid);
            return CreatePlayResponseFromGrid(winResult, currentBalance, grid);
        }

        // ============ MÉTODO PARA MODO EXTERNO (API) ============

        /// <summary>
        /// Processa um PlayResponse recebido externamente (modo API)
        /// </summary>
        public PlayResponse ProcessExternalPlayResponse(PlayResponse externalResponse, int currentBalance)
        {
            if (mode != SlotMathMode.External)
            {
                Debug.LogWarning("[SlotMathCalculator] Modo não é EXTERNAL. Mudando temporariamente para EXTERNAL.");
                mode = SlotMathMode.External;
            }

            if (externalResponse == null || externalResponse.reel == null || externalResponse.reel.rows == null)
            {
                Debug.LogError("[SlotMathCalculator] PlayResponse externo inválido!");
                return null;
            }

            // Converter o ReelDto recebido para grid interno
            int[,] grid = ConvertReelDtoToGrid(externalResponse.reel);

            // Atualizar configuração do grid baseado nos dados recebidos
            gridConfig.rows = externalResponse.reel.rows.Length;
            if (externalResponse.reel.rows.Length > 0)
            {
                gridConfig.columns = externalResponse.reel.rows[0].elements.Length;
            }

            // Avaliar o grid recebido usando as regras atuais
            var winResult = EvaluateSpin(grid);

            // Criar PlayResponse baseado no grid avaliado
            return CreatePlayResponseFromGrid(winResult, currentBalance, grid);
        }

        /// <summary>
        /// Converte um ReelDto recebido externamente para grid interno
        /// </summary>
        private int[,] ConvertReelDtoToGrid(ReelDto reel)
        {
            if (reel == null || reel.rows == null || reel.rows.Length == 0)
                return null;

            int rows = reel.rows.Length;
            int cols = reel.rows[0]?.elements?.Length ?? 0;

            int[,] grid = new int[rows, cols];

            for (int row = 0; row < rows; row++)
            {
                if (reel.rows[row] != null && reel.rows[row].elements != null)
                {
                    for (int col = 0; col < cols && col < reel.rows[row].elements.Length; col++)
                    {
                        grid[row, col] = reel.rows[row].elements[col];
                    }
                }
            }

            return grid;
        }

        /// <summary>
        /// Cria um PlayResponse a partir de um grid avaliado
        /// </summary>
        private PlayResponse CreatePlayResponseFromGrid(SpinResult winResult, int currentBalance, int[,] grid)
        {
            ReelDto reel = new ReelDto();
            reel.rows = new Row[gridConfig.rows];

            for (int row = 0; row < gridConfig.rows; row++)
            {
                reel.rows[row] = new Row();
                reel.rows[row].elements = new byte[gridConfig.columns];
                for (int col = 0; col < gridConfig.columns; col++)
                {
                    reel.rows[row].elements[col] = (byte)grid[row, col];
                }
            }

            // Log do resultado
            if (winResult.lineWins.Count > 0)
            {
                Debug.Log($"✅ GANHOU! Total: {winResult.totalWin:F2}");
                foreach (var win in winResult.lineWins)
                {
                    Debug.Log($"   → {win.paylineName}: {win.consecutiveCount}x {win.symbolName} = {win.payout:F2}");
                }
            }
            else
            {
                Debug.Log($"❌ Nenhum padrão vencedor encontrado");
            }

            WaysSummaryDto waysSummary = new WaysSummaryDto();
            waysSummary.total = (int)(winResult.totalWin * 100);

            if (winResult.lineWins.Count > 0)
            {
                var perSymbol = new List<WaysSymbolSummaryDto>();
                foreach (var win in winResult.lineWins)
                {
                    perSymbol.Add(new WaysSymbolSummaryDto
                    {
                        symbolId = win.symbolId,
                        ways3 = win.consecutiveCount == 3 ? 1 : 0,
                        ways4 = win.consecutiveCount == 4 ? 1 : 0,
                        ways5 = win.consecutiveCount == 5 ? 1 : 0,
                        payout = (int)(win.payout * 100)
                    });
                }
                waysSummary.perSymbol = perSymbol.ToArray();
            }

            PlayResponse response = new PlayResponse
            {
                rewards = (int)(winResult.totalWin * 100),
                reel = reel,
                gameState = GameState.Default,
                waysSummary = waysSummary,
                playerBalanceWithoutRewards = currentBalance,
                matchId = currentMatchId
            };

            return response;
        }

        // ============ MÉTODO PARA VALIDAR GRID EXTERNO SEM GERAR NOVO ============

        /// <summary>
        /// Apenas valida um grid externo e retorna os padrões vencedores
        /// </summary>
        public List<WinningPatternInfo> ValidateExternalGrid(int[,] grid)
        {
            if (grid == null) return new List<WinningPatternInfo>();

            // Atualizar configuração do grid
            gridConfig.rows = grid.GetLength(0);
            gridConfig.columns = grid.GetLength(1);

            var result = EvaluateSpin(grid);
            return winningPositionsByPattern;
        }

        /// <summary>
        /// Apenas valida um ReelDto externo e retorna os padrões vencedores
        /// </summary>
        public List<WinningPatternInfo> ValidateExternalReel(ReelDto reel)
        {
            int[,] grid = ConvertReelDtoToGrid(reel);
            return ValidateExternalGrid(grid);
        }

        private string GetSymbolChar(int symbolId)
        {
            switch (symbolId)
            {
                case 1: return "🧙";
                case 2: return "🎃";
                case 3: return "💀";
                case 4: return "🦇";
                case 5: return "🕷️";
                case 6: return "🧪";
                case 7: return "⭐";
                default: return "❓";
            }
        }

        public static void LogPlayResponse(PlayResponse response, float totalBet)
        {
            if (response.reel?.rows != null)
            {
                Debug.Log("┌─────┬─────┬─────┬─────┬─────┐");
                for (int row = 0; row < response.reel.rows.Length; row++)
                {
                    string line = "│";
                    for (int col = 0; col < response.reel.rows[row].elements.Length; col++)
                    {
                        int symbolId = response.reel.rows[row].elements[col];
                        string symbol = GetSymbolCharStatic(symbolId);
                        line += $" {symbol} │";
                    }
                    Debug.Log(line);
                }
                Debug.Log("└─────┴─────┴─────┴─────┴─────┘");
            }

            if (response.rewards > 0)
                Debug.Log($"💰 WIN: {response.rewards / 100f:F2}");
            else
                Debug.Log($"😔 No win");
        }

        private static string GetSymbolCharStatic(int symbolId)
        {
            switch (symbolId)
            {
                case 1: return "🧙";
                case 2: return "🎃";
                case 3: return "💀";
                case 4: return "🦇";
                case 5: return "🕷️";
                case 6: return "🧪";
                case 7: return "⭐";
                default: return "❓";
            }
        }
    }

    public class PaylineWinResult
    {
        public int paylineId;
        public string paylineName;
        public int symbolId;
        public string symbolName;
        public int consecutiveCount;
        public float payout;
        public List<Vector2Int> positions;
        public PaylineWinResult() { positions = new List<Vector2Int>(); }
    }

    public class SpinResult
    {
        public int[,] grid;
        public float totalWin;
        public List<PaylineWinResult> lineWins;
        public SpinResult() { lineWins = new List<PaylineWinResult>(); }
    }

    public class SymbolData
    {
        public int id;
        public string name;
        public string symbolChar;
        public float payout5;
        public float payout4;
        public float payout3;
        public int weight;

        public SymbolData(int id, string name, string symbolChar, float p5, float p4, float p3, int weight)
        {
            this.id = id;
            this.name = name;
            this.symbolChar = symbolChar;
            this.payout5 = p5;
            this.payout4 = p4;
            this.payout3 = p3;
            this.weight = weight;
        }
    }
}
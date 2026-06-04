using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using System;
using System.Text.RegularExpressions;
using WG_Casino.SlotMachine;

namespace WG_Casino
{
    public class HistoryItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI matchIDText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private GameObject bonusIndicator;
        [SerializeField] private Button replayButton;

        private int historyIndex;

        public void Initialize(SerializableLastPlay historyItem, int index)
        {
            this.historyIndex = index;

            // Atualizar textos
            if (matchIDText != null)
                matchIDText.text = $"ID: {historyItem.matchid}";

            if (dateText != null)
            {
                if (IsValidDateString(historyItem.date))
                {
                    var d = ParseStandardDate(historyItem.date);
                    dateText.text = FormatForDisplay(d);
                }
                else
                {
                    dateText.text = historyItem.date;
                }
            }

            if (betText != null)
                betText.text = $"{GameUtils.FormatCurrency(historyItem.playItem.betValue)}";

            if (rewardText != null)
                rewardText.text = $"{GameUtils.FormatCurrency(historyItem.playItem.rewards)}";

            if (balanceText != null)
                balanceText.text = $"{GameUtils.FormatCurrency(historyItem.afterBet)}";

            // Mostrar indicador de bônus
            if (bonusIndicator != null)
                bonusIndicator.SetActive(historyItem.playItem.hasBonusSpins);

            // Configurar botão
            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);
        }
        public static bool IsValidDateString(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                Debug.LogWarning("String de data vazia ou nula");
                return false;
            }

            // Remove caracteres inválidos para análise
            string cleanString = dateString.Trim();

            // Verifica se contém caracteres não imprimíveis ou inválidos
            foreach (char c in cleanString)
            {
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                {
                    Debug.LogWarning($"String contém caracteres de controle inválidos: {(int)c}");
                    return false;
                }
            }

            // Verifica comprimento mínimo (pelo menos "2025-01-01")
            if (cleanString.Length < 10)
            {
                Debug.LogWarning($"String muito curta para ser uma data: '{cleanString}' ({cleanString.Length} chars)");
                return false;
            }

            // Verifica padrões básicos de datas
            bool hasValidPattern =
                Regex.IsMatch(cleanString, @"^\d{4}[-/]\d{2}[-/]\d{2}") || // yyyy-MM-dd ou yyyy/MM/dd
                Regex.IsMatch(cleanString, @"^\d{2}[-/]\d{2}[-/]\d{4}") || // dd-MM-yyyy ou dd/MM/yyyy
                (cleanString.Contains("T") && cleanString.IndexOf("T") > 0); // Formato ISO

            if (!hasValidPattern)
            {
                Debug.LogWarning($"String não corresponde a padrões de data conhecidos: '{cleanString}'");
                return false;
            }

            // Verifica se tem apenas caracteres válidos para datas
            string validDateChars = "0123456789-/: .TZt+";
            foreach (char c in cleanString)
            {
                if (!validDateChars.Contains(c.ToString()) && !char.IsLetter(c))
                {
                    Debug.LogWarning($"Caractere inválido encontrado na data: '{c}' ({(int)c})");
                    return false;
                }
            }

            return true;
        }
        public static DateTime ParseStandardDate(string dateString)
        {
            //Debug.Log("Parsing date string: " + dateString);

            if (dateString.Contains("T") && dateString.EndsWith("Z"))
            {
                // Formato ISO 8601 com UTC
                return DateTime.Parse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }
            else
            {
                // Outros formatos
                return DateTime.Parse(dateString, CultureInfo.InvariantCulture);
            }
        }

        // Para exibir ao usuário
        public static string FormatForDisplay(DateTime date, string cultureCode = "pt-BR")
        {
            CultureInfo culture = new CultureInfo(cultureCode);
            return date.ToString(culture);
        }

        private void OnReplayClicked()
        {
            HistoryUIManager.Instance.ExecuteHistoryReplay(historyIndex);
        }

        void OnDestroy()
        {
            // Limpar eventos do botão
            if (replayButton != null)
                replayButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (WG_SlotMachine.Instance.AreAllReelsCompleted() && !BonusManager.Instance.isAnimating && !PayoutAnimation.Instance.isPayout && !CanvasManager.Instance.isProcessingPlayResponse)
            {
                if (SlotClient.Instance.bonusResponse != null)
                {
                    if (!SlotClient.Instance.bonusResponse.bonusOver)
                    {
                        if (SlotClient.Instance.bonusResponse.bonusMultipliers.Count != 0)
                            replayButton.interactable = false;
                        else
                            replayButton.interactable = true;
                    }
                    else
                    {
                        replayButton.interactable = true;
                    }
                }
                else
                {
                    replayButton.interactable = true;
                }
            }
            else
            {
                replayButton.interactable = false;
            }
        }
    }
}
// PaymentPattern.cs - Versão SIMPLIFICADA (sem absolutas)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewPaymentPattern", menuName = "Slot Machine/Payment Pattern")]
public class PaymentPattern : ScriptableObject
{
    [System.Serializable]
    public class SlotPosition
    {
        public int reelIndex;  // 0 a reelCount-1 (coluna)
        public int rowIndex;   // 0 a visibleRows-1 (linha)
        public Sprite slotSprite;

        public SlotPosition(int reel, int row)
        {
            reelIndex = reel;
            rowIndex = row;
            slotSprite = null;
        }
    }

    [Header("Configuração")]
    public int reelCount = 5;
    public int visibleRows = 3;  // Número de linhas no grid (3, 4 ou 5)
    public float payoutValue = 10.0f;
    public bool isBonusPattern = false;

    [Header("Math Integration")]
    public int patternId;
    public string patternName;
    public int requiredSymbolId = -1;
    public int minMatchCount = 3;

    [Header("Slots Selecionados")]
    [SerializeField] private List<SlotPosition> _selectedSlots = new List<SlotPosition>();

    public List<SlotPosition> selectedSlots
    {
        get
        {
            _selectedSlots.Sort((a, b) => a.reelIndex.CompareTo(b.reelIndex));
            return _selectedSlots;
        }
        set
        {
            _selectedSlots = value;
            _selectedSlots.Sort((a, b) => a.reelIndex.CompareTo(b.reelIndex));
        }
    }

    // Obtém as posições para o grid
    public List<Vector2Int> GetPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        foreach (var slot in selectedSlots)
        {
            positions.Add(new Vector2Int(slot.rowIndex, slot.reelIndex));
        }
        return positions;
    }

    // Verifica se o padrão corresponde ao grid
    public bool MatchesGrid(int[,] grid, out int matchedSymbolId)
    {
        matchedSymbolId = -1;

        if (selectedSlots.Count < minMatchCount) return false;

        int referenceValue = -1;
        foreach (var slot in selectedSlots)
        {
            // Validação dos limites
            if (slot.reelIndex >= grid.GetLength(1) || slot.rowIndex >= grid.GetLength(0))
                return false;

            int currentValue = grid[slot.rowIndex, slot.reelIndex];
            if (currentValue != 0 && currentValue != 9) // 9 é wild
            {
                referenceValue = currentValue;
                break;
            }
        }

        if (referenceValue == -1) return false;

        if (requiredSymbolId != -1 && referenceValue != requiredSymbolId)
            return false;

        foreach (var slot in selectedSlots)
        {
            int currentValue = grid[slot.rowIndex, slot.reelIndex];
            if (currentValue != referenceValue && currentValue != 9)
                return false;
        }

        matchedSymbolId = referenceValue;
        return true;
    }

    public void AddSlot(int reelIndex, int rowIndex)
    {
        if (!IsSlotSelected(reelIndex, rowIndex))
        {
            _selectedSlots.Add(new SlotPosition(reelIndex, rowIndex));
            _selectedSlots.Sort((a, b) => a.reelIndex.CompareTo(b.reelIndex));
        }
    }

    public void RemoveSlot(int reelIndex, int rowIndex)
    {
        _selectedSlots.RemoveAll(s => s.reelIndex == reelIndex && s.rowIndex == rowIndex);
        _selectedSlots.Sort((a, b) => a.reelIndex.CompareTo(b.reelIndex));
    }

    public void SetSlotSprite(int reelIndex, int rowIndex, Sprite sprite)
    {
        var slot = _selectedSlots.Find(s => s.reelIndex == reelIndex && s.rowIndex == rowIndex);
        if (slot != null) slot.slotSprite = sprite;
    }

    public Sprite GetSlotSprite(int reelIndex, int rowIndex)
    {
        var slot = _selectedSlots.Find(s => s.reelIndex == reelIndex && s.rowIndex == rowIndex);
        return slot?.slotSprite;
    }

    public bool IsSlotSelected(int reelIndex, int rowIndex)
    {
        return _selectedSlots.Exists(s => s.reelIndex == reelIndex && s.rowIndex == rowIndex);
    }

    public void DebugPrintPattern()
    {
        return;
        Debug.Log($"=== Padrão: {patternName} (ID: {patternId}) ===");
        Debug.Log($"Grid: {visibleRows} rows x {reelCount} reels");
        Debug.Log($"Posições selecionadas: {selectedSlots.Count}");
        foreach (var slot in selectedSlots)
        {
            Debug.Log($"  Reel {slot.reelIndex}, Row {slot.rowIndex}");
        }
        Debug.Log($"Payout: {payoutValue}, Required Symbol: {requiredSymbolId}");
    }
}
using UnityEngine;
using System.Collections.Generic;
using WG_Casino.SlotMachine;

[System.Serializable]
public class RowsInfos
{
    public int index;
    public bool onRow;

    public RowsInfos(int idx, bool on)
    {
        index = idx;
        onRow = on;
    }
}

[System.Serializable]
public class ReelsInfos
{
    public List<RowsInfos> slotsInfos = new List<RowsInfos>();
}

public class SlotPattern : ScriptableObject
{
    public string patternName;
    public int reelCount;
    public int totalRows = 12; // Fixo em 12
    public int visibleRows = 3; // Configurável de 3 a 5
    public ReelAnimationType animationType = ReelAnimationType.IndependentRows;
    public bool onShakeSlot = false;

    public List<ReelsInfos> reelsInfos = new List<ReelsInfos>();

    public void GenerateReelsInfosFromGrid(bool[,] grid)
    {
        reelsInfos.Clear();

        for (int reel = 0; reel < reelCount; reel++)
        {
            ReelsInfos reelInfo = new ReelsInfos();

            for (int row = 0; row < totalRows; row++)
            {
                reelInfo.slotsInfos.Add(new RowsInfos(row, grid[reel, row]));
            }

            reelsInfos.Add(reelInfo);
        }
    }

    public bool IsSlotActive(int reel, int row)
    {
        if (reel < 0 || reel >= reelsInfos.Count) return false;
        if (row < 0 || row >= reelsInfos[reel].slotsInfos.Count) return false;

        return reelsInfos[reel].slotsInfos[row].onRow;
    }

    // Método para obter a grade visual
    public bool[,] GetSlotGrid()
    {
        bool[,] grid = new bool[reelCount, totalRows];

        for (int reel = 0; reel < reelsInfos.Count; reel++)
        {
            for (int row = 0; row < reelsInfos[reel].slotsInfos.Count; row++)
            {
                grid[reel, row] = reelsInfos[reel].slotsInfos[row].onRow;
            }
        }

        return grid;
    }

    // Retorna o índice inicial da área visível
    public int GetVisibleStartRow()
    {
        return 0; // Sempre começa da linha 0
    }

    // Retorna o índice final da área visível
    public int GetVisibleEndRow()
    {
        return visibleRows - 1; // Termina em visibleRows - 1
    }

    [ContextMenu("Debug Pattern Structure")]
    public void DebugPatternStructure()
    {
        Debug.Log($"=== Debug Pattern: {patternName} ===");
        Debug.Log($"Reels: {reelCount}, Total Rows: {totalRows}, Visible Rows: {visibleRows}");
        Debug.Log($"Visible Area: Rows {GetVisibleStartRow()} to {GetVisibleEndRow()}");

        for (int reel = 0; reel < reelsInfos.Count; reel++)
        {
            string reelStatus = $"Reel {reel}: ";
            for (int row = 0; row < reelsInfos[reel].slotsInfos.Count; row++)
            {
                string status = reelsInfos[reel].slotsInfos[row].onRow ? "1" : "0";
                reelStatus += $"[{row}:{status}] ";
            }
            Debug.Log(reelStatus);
        }
    }
}
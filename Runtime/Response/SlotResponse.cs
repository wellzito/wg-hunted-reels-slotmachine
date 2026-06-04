using SlotMachineMath;
using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class SerializableLastPlay 
{
    public int matchid = 0;
    public int userid = 0;
    public string date = string.Empty;
    public int beforeBet = 0;
    public int afterBet = 0;
    public SerializableLastPlayItem playItem = new SerializableLastPlayItem();
}

[Serializable]
public class SerializableLastPlayItem 
{
    // Required fields - matching TypeScript types
    public byte betIndex = 0;
    public int betValue = 0;
    public int rewards = 0;
    public byte gameState = 0;
    public ReelDto reel = new ReelDto();

    // Optional fields with presence flags
    public bool hasWays = false;
    public WaysSummaryDto ways = new WaysSummaryDto();

    public bool hasDefaultEvent = false;
    public DefaultEventDto defaultEvent = new DefaultEventDto();

    public bool hasBonusSpins = false;
    public List<BonusSpinData> bonusSpins = new List<BonusSpinData>();

    public List<WinningPatternInfo> lastWinningPatterns = new List<WinningPatternInfo>();
}

[Serializable]
public class BonusSpinData 
{
    public int rewards = 0;
    public byte freespinsRemaining = 0;
    public List<int> bonusMultipliers = new List<int>();
    public List<List<byte>> bonusGrid = new List<List<byte>>();
    public List<BonusTentacleDto> bonusTentacles = new List<BonusTentacleDto>();
    public bool bonusOver = false;
}

[Serializable]
public class ReelDto 
{
    public Row[] rows = Array.Empty<Row>();
}

[Serializable]
public class Row
{
    public byte[] elements = Array.Empty<byte>();
}

[Serializable]
public class WaysSummaryDto
{
    public int total = 0;
    public WaysSymbolSummaryDto[] perSymbol = Array.Empty<WaysSymbolSummaryDto>();

}
[Serializable]
public class WaysSymbolSummaryDto
{
    public int symbolId = 0;
    public int ways3 = 0;
    public int ways4 = 0;
    public int ways5 = 0;
    public int payout = 0;
}
[Serializable]
public class DefaultEventDto
{
    public byte triggered = 0; // 0/1
    public byte selectedSymbol = 0;
    public byte dir = 0; // 0:U, 1:D, 2:L, 3:R
    public byte pattern = 0; // 1,2,3
    public CoordDto[] originalComboCells = Array.Empty<CoordDto>();
    public CoordDto[] hitCells = Array.Empty<CoordDto>();
    public CoordDto[] patternCells = Array.Empty<CoordDto>();
    public CoordDto[] cascadeCells = Array.Empty<CoordDto>();
    public ReelDto reelAfter = new ReelDto();
}
[Serializable]
public class CoordDto
{
    public byte r = 0;
    public byte c = 0;

}

[Serializable]
public class BonusTentacleDto
{
    public byte r = 0;
    public byte c = 0;
    public byte dir = 0; // 0:U, 1:D, 2:L, 3:R
    public double hiddenMult = 0;
}

//Play
[Serializable]
public class PlayResponse
{
    public int rewards = 0;
    public ReelDto reel = new ReelDto();
    public GameState gameState = GameState.Default;
    public WaysSummaryDto waysSummary = new WaysSummaryDto();
    public int playerBalanceWithoutRewards = 0;
    public int matchId = 0;
}

public enum GameState
{
    Default = 0,
    Bonus = 1,
    Event = 2
}
[Serializable]
public class BonusResponse
{
    public int rewards = 0;
    public GameState gameState = GameState.Bonus;
    public byte freespinsRemaining = 0;
    public bool bonusOver = false;
    public List<int> bonusMultipliers = new List<int>();
    public ReelDto bonusReel = new ReelDto();
    public List<BonusTentacleDto> bonusTentacles = new List<BonusTentacleDto>();
    public int playerBalanceWithoutRewards = 0;
    public int matchId = 0;
}
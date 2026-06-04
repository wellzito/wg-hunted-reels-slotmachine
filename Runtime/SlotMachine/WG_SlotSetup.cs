using UnityEngine;
using System.Collections.Generic;
using WG_Casino.SlotMachine;
using static WG_Casino.SlotMachine.WG_SlotMachine;
using TMPro;
using Unity.Mathematics;
using WG_Casino;
using WG_Casino.Systems;
using WG_Casino.Setup;

[CreateAssetMenu(fileName = "WG_SlotSetup", menuName = "WG Casino/Slot Setup")]
public class WG_SlotSetup : ScriptableObject
{
    [Header("Wizard Settings")]
    public SlotPattern slotPattern;
    public bool onUpdateWizard;

    [Header("Symbols | Icons")]
    public List<WG_SymbolSO> m_symbols = new List<WG_SymbolSO>();

    [Header("Symbols Special")]
    public List<SymbolsSpecial> symbols = new List<SymbolsSpecial>();
    public PaymentPattern curPaymentPattern { get; set; }

    [HideInInspector] public List<PaymentPattern> paymentPatterns = new List<PaymentPattern>();
    public enum SymbolSpecialIndex
    {
        bonus,
        wild,
        diamond,
        chest,
        roulette
    }

    [System.Serializable]
    public class SymbolsSpecial
    {
        public SymbolSpecialIndex special;
        public int indexSymbol;
    }
}

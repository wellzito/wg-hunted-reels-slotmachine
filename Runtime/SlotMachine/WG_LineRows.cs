using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WG_Casino.SlotMachine;

public class WG_LineRows : MonoBehaviour
{
    public GameObject reelColPrefab;

    [Header("Delays Settings Loop Animation")]
    public float delayStart;
    public float delayPreSpin;
    public float delayEndDuration;
    public float delayBetweenEndReels;

    [Header("Delays and Speeds Cascade Animation")]
    public float delaycascadeBetweenIcons;
    public float cascadeFallSpeedStart = 2500f;
    public float cascadeFallSpeedEnd = 1000f;

    [Header("Height Settings Cascade Animation")]
    public float cascadeResetHeightOffset = 300;

    public List<WG_SlotMachineReel> m_reels = new List<WG_SlotMachineReel>();
    RectMask2D mask2D;

    void Start()
    {
        // Encontra o índice desta linha na lista global
        int lineIndex = GetLineIndex();
        int totalReelsPerLine = m_reels.Count;

        // Calcula o delay base para esta linha considerando todas as linhas anteriores
        // (índice da linha * quantidade de reels por linha * delayStart)
        float lineBaseDelay = lineIndex * totalReelsPerLine * delayStart;

        var cascadeH = cascadeResetHeightOffset * (lineIndex + 1);

        // Aplica os delays progressivos para cada reel
        for (int i = 0; i < m_reels.Count; i++)
        {
            // Delay progressivo: lineBaseDelay + (delayStart * índice do reel)
            float progressiveDelay = lineBaseDelay + (delayStart * i);
            float progressiveDelayEnd = /*lineBaseDelay +*/ (delayBetweenEndReels * i);

            m_reels[i].m_initialDelay = progressiveDelay;
            m_reels[i].m_preSpinDuration = m_reels[i].m_preSpinDuration + delayPreSpin;
            m_reels[i].m_endSpinDuration = m_reels[i].m_endSpinDuration + delayEndDuration + progressiveDelayEnd;
            m_reels[i].m_cascadeDelayBetweenIcons = delaycascadeBetweenIcons;
            m_reels[i].m_cascadeResetHeightOffset = cascadeH;
            m_reels[i].m_cascadeFallSpeedEnd = cascadeFallSpeedEnd;
            m_reels[i].m_cascadeFallSpeedStart = cascadeFallSpeedStart;

            //Debug.Log($"Linha {lineIndex}, Reel {i}: Delay = {progressiveDelay}");
        }

        mask2D = GetComponent<RectMask2D>();

        if (WG_SlotMachine.Instance.gameSetup.slotPattern.animationType == ReelAnimationType.CascadeFall)
        {
            mask2D.enabled = false;
        }

        EnableMask(false);
    }

    private int GetLineIndex()
    {
        if (WG_SlotMachine.Instance != null && WG_SlotMachine.Instance.lineRows != null)
        {
            return WG_SlotMachine.Instance.lineRows.IndexOf(this);
        }

        // Fallback: tenta encontrar pelo nome ou retorna 0
        Debug.LogWarning("Não foi possível encontrar o índice da linha, usando 0 como padrão");
        return 0;
    }

    public void SetReels(int countReels)
    {
        if (m_reels.Count != 0)
        {
            for (int i = 0; i < m_reels.Count; i++)
            {
                DestroyImmediate(m_reels[i].gameObject);
            }
        }
        m_reels.Clear();

        for (int i = 0; i < countReels; i++)
        {
            WG_SlotMachineReel reelNewObj = Instantiate(reelColPrefab, this.transform).GetComponent<WG_SlotMachineReel>();
            m_reels.Add(reelNewObj);
        }
    }

    public void EnableMask(bool enable)
    {
        if (mask2D != null)
        {
            mask2D.enabled = enable;
        }
    }
}
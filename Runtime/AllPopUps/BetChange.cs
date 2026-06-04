using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WG_Casino.SlotMachine;
using WG_Casino;
using System.Collections.Generic;
using SlotMachineMath;

public class BetChange : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI betText;

    [SerializeField] Button startAutoSpinBtn;
    [SerializeField] Button closeBtn;

    [SerializeField] List<Button> betButtons = new List<Button>();
    [SerializeField] List<int> betVals = new List<int>();

    public bool onAutoSpin = false;
    // Start is called before the first frame update
    void Start()
    {
        startAutoSpinBtn.onClick.AddListener(() => PlayAutoSpin());
        closeBtn.onClick.AddListener(() => gameObject.SetActive(false));

        for (int i = 0; i < betButtons.Count; i++)
        {
            int index = i;
            betButtons[i].onClick.AddListener(() =>
            {
                var val = betVals[index];
                SetGameManagerAutoSpinsAmount(val, betButtons[index]);
            });
        }
    }

    public void SetBetTextTextValue(int text)
    {
        string formattedValue = GameUtils.FormatCurrency(GameManager.Instance.Bet);//.ToString("C2", GameManager.Instance.Culture);
        if (betText) betText.text = formattedValue;
    }

    void OnEnable()
    {
        ResetAutoSpinConfig();
    }
    void DeselectAllButtons()
    {
        ColorBlock colors = betButtons[0].colors;
        colors.normalColor = Color.white; // Cor padrão quando não está selecionado

        for (int i = 0; i < betButtons.Count; i++)
        {
            betButtons[i].colors = colors;
        }
    }
    public void ResetAutoSpinConfig()
    {
        GameManager.Instance.AutoSpins = 0;

        startAutoSpinBtn.interactable = false;
        for (int i = 0; i < betButtons.Count; i++)
        {
            betButtons[i].enabled = true;
        }

        DeselectAllButtons();
    }
    public void SetGameManagerAutoSpinsAmount(int amount, Button button)
    {
        if (!GameManager.Instance.Playing)
        {
            startAutoSpinBtn.interactable = true;

            SetBetTextTextValue(amount);

            GameEvents.ChangeBet(amount);

        }
        else
        {
            button.interactable = false;
            button.interactable = true;

        }
    }
    public void PlayAutoSpin()
    {
        var gm = GameManager.Instance;
        var cm = CanvasManager.Instance;
        var sm = WG_SlotMachine.Instance;

        if (gm.AutoSpins > 0 || cm.popUpManager.AutoSpinManager.onAutoSpin)
        {
            gm.AutoSpins = 0;
            cm.popUpManager.AutoSpinManager.onAutoSpin = false;
            cm.autoPlayBtn.isOn = false;
            Debug.Log("Auto Spin Cancel");
            sm.ForceStop();
            //ExitAutoSpin();
            return;
        }
        if (!gm.Playing && sm.AreAllReelsCompleted())
        {
            //if (gm.GameID == 0)
            //{
            if (GameUtils.HasSufficientCredits(gm.Credits, gm.Bet))
            {
                //gm.Credits -= gm.Bet;
            }
            else
            {
                cm.ShowNoBalanceMessage();
                return;
            }
            //}
            if (!gm.ClickPlay && sm.AreAllReelsCompleted() && !gm.Playing)
            {
                gm.Gain = 0;
                cm.UpdateGainText(gm.Gain);
                //WebClient.Instance.PlayRequest();
                SlotMathRunner.Instance.ManualSpin();
                //sm.BetPlayBtn();
                WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_play);
                gm.Play();
                cm.StartCreateParticlePlay();
            }

        }
        else if (gm.Playing && !gm.ClickPlay && !sm.AreAllReelsCompleted())
        {
            sm.ForceStop();
        }
        else
        {
            sm.ForceStop();

            Debug.Log("Aguarde o término da rodada atual.");
        }

        gameObject.SetActive(false);
    }
}

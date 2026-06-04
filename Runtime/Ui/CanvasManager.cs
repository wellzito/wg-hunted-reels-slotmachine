using SlotMachineMath;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WG_Casino;
using WG_Casino.SlotMachine;

public class CanvasManager : MonoBehaviour
{
    #region Template
    public static CanvasManager Instance;

    [Header("HeaderButtons")]
    [SerializeField] Button Home;
    [SerializeField] Button FullScreen;
    [SerializeField] Image FullScreenImage;
    [SerializeField] Sprite spriteWindowed, spriteFullScreen;
    [Header("PlayScreen")]
    [SerializeField] GameObject initialScreen;
    [SerializeField] Transform gameScreen;
    [SerializeField] GameObject playButtonObj;
    [SerializeField] Button playButton;
    [SerializeField] GameObject connecting;

    [Header("LoadingScreen")]
    [SerializeField] GameObject loading;
    [Header("MessagePanel")]
    [SerializeField] GameObject messagepanel;
    [SerializeField] TextMeshProUGUI messageText;
    [SerializeField] TextMeshProUGUI messageTitle;
    [SerializeField] Button closeButton;
    #endregion

    [Header("Main Interface")]
    [SerializeField] GameObject playParticlePrefab;
    [SerializeField] Transform playParticlePos;
    private GameObject playParticleCur;

    [SerializeField] Button playBtn;

    public Toggle autoPlayBtn;
    [SerializeField] Toggle turboBtn;
    [SerializeField] Button betBtn;
    [SerializeField] Button settingBtn;
    [SerializeField] List<Button> animTypeBtn = new List<Button>();

    [Header("Main Interface Images")]
    [SerializeField] Image playRotateImg;
    [SerializeField] Transform playRotateTransform;
    public bool isProcessingPlayResponse = false;

    [Header("Interface Texts")]
    [SerializeField] TextMeshProUGUI betText;
    [SerializeField] TextMeshProUGUI creditsText;
    [SerializeField] TextMeshProUGUI autoSpinsAmountText;
    [SerializeField] TextMeshProUGUI gainText;
    [SerializeField] TextMeshProUGUI autoSpinValue;

    [Header("Version")]
    [SerializeField] TextMeshProUGUI version;
    public TextMeshProUGUI GainText => gainText;
    public TextMeshProUGUI CreditsText => creditsText;


    [Header("Cameras")]
    [SerializeField] GameObject symbolCam;

    public GameObject SymbolCam
    {
        get
        {
            return symbolCam;
        }
        set
        {
            symbolCam = value;
        }
    }

    [Header("Animators")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool clockwise = true;

    bool onAutoSpin = false;

    [Header("PopUpManager")]
    public PopUpManager popUpManager;
    [SerializeField] Transform menuPopUpContent;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        version.text = $"Version : {Application.version}";

        if (playBtn) playBtn.onClick
           .AddListener(
               () =>
               {
                   if (GameManager.Instance.AutoSpins > 0 || popUpManager.AutoSpinManager.onAutoSpin)
                   {
                       GameManager.Instance.AutoSpins = 0;
                       popUpManager.AutoSpinManager.onAutoSpin = false;
                       autoPlayBtn.isOn = false;
                       Debug.Log("Auto Spin Cancel");
                       WG_SlotMachine.Instance.ForceStop();
                       return;
                   }
                   if (!GameManager.Instance.Playing && WG_SlotMachine.Instance.AreAllReelsCompleted())
                   {
                       if (GameUtils.HasSufficientCredits(GameManager.Instance.Credits, GameManager.Instance.Bet))
                       {
                           //GameManager.Instance.Credits -= GameManager.Instance.Bet;
                       }
                       else
                       {
                           ShowNoBalanceMessage();
                           return;
                       }
                       if (!GameManager.Instance.ClickPlay && WG_SlotMachine.Instance.AreAllReelsCompleted() && !GameManager.Instance.Playing)
                       {
                           GameManager.Instance.Gain = 0;
                           UpdateGainText(GameManager.Instance.Gain);
                           //WebClient.Instance.PlayRequest();
                           SlotMathRunner.Instance.ManualSpin();
                           //WG_SlotMachine.Instance.BetPlayBtn();
                           WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_play);
                           GameManager.Instance.Play();
                           StartCoroutine(CreateParticlePlay());
                       }

                   }
                   else if (GameManager.Instance.Playing && !GameManager.Instance.ClickPlay && !WG_SlotMachine.Instance.AreAllReelsCompleted())
                   {
                       WG_SlotMachine.Instance.ForceStop();
                   }
                   else
                   {
                       WG_SlotMachine.Instance.ForceStop();

                       Debug.Log("Aguarde o término da rodada atual.");
                   }
               });

        if (autoPlayBtn) autoPlayBtn.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                WG_AudioManager.Instance.Play("tapbutton");
                OpenAutoPlayMenu();
            }
            else
            {
                popUpManager.AutoSpinManager.onAutoSpin = false;
                GameManager.Instance.AutoSpins = 0;
            }
        });

        if (turboBtn) turboBtn.onValueChanged.AddListener((isOn) =>
        {
            GameManager.Instance.Turbo = isOn;
            WG_AudioManager.Instance.Play("tapbutton");
            WG_SlotMachine.Instance.onSpeedMode = GameManager.Instance.Turbo;
        });

        UpdateBalanceText(GameManager.Instance.Credits);
        UpdateGainText(GameManager.Instance.Gain);
        UpdateBetText(GameManager.Instance.Bet);

        #region Template
        if(closeButton) closeButton.onClick.AddListener(() =>
        {
            HideMessagePanel();
            WG_AudioManager.Instance.Play("tapbutton");
        });
        if(FullScreen) FullScreen.onClick.AddListener(() =>
        {
            SetFullScreen();
            WG_AudioManager.Instance.Play("tapbutton");
        });
        if(Home) Home.onClick.AddListener(() =>
        {
            GoToHome();
            WG_AudioManager.Instance.Play("tapbutton");
        });
        if(playButton) playButton.onClick.AddListener(() =>
        {
            playButton.interactable = false;
            Invoke("SetPlayButtonInteractable", 1f);
            ActiveInitialScreen(false);
            WG_AudioManager.Instance.PlayBgMusic();
        });

#if UNITY_EDITOR
        if(FullScreen) FullScreen.gameObject.SetActive(false);
#elif UNITY_WEBGL
        if(FullScreen) FullScreen.gameObject.SetActive(!WebGLPluginJS.IsIOSBrowser());
#endif
        #endregion

        if (settingBtn) settingBtn.onClick.AddListener(() =>
        {
            WG_AudioManager.Instance.Play("tapbutton");
            OpenConfig();
        });
    }

    private void OnEnable()
    {
        // Inscrever nos eventos do GameEvents
        GameEvents.OnBalanceUpdated += HandleBalanceUpdated;
        GameEvents.OnGain += HandleGainUpdated;
        GameEvents.OnBetChanged += HandleBetChanged;
        GameEvents.OnMessageShow += HandleMessageShow;
        GameEvents.OnAutoSpinStopped += HandleAutoSpinStopped;
        GameEvents.OnPayout += HandlePayoutEffect;
        GameEvents.OnPayoutCompleted += HandlePayoutCompleted;
        GameEvents.OnShakeRequested += HandleShakeRequest;
    }

    private void OnDisable()
    {
        GameEvents.OnBalanceUpdated -= HandleBalanceUpdated;
        GameEvents.OnGain -= HandleGainUpdated;
        GameEvents.OnBetChanged -= HandleBetChanged;
        GameEvents.OnMessageShow -= HandleMessageShow;
        GameEvents.OnAutoSpinStopped -= HandleAutoSpinStopped;
        GameEvents.OnPayout -= HandlePayoutEffect;
        GameEvents.OnPayoutCompleted -= HandlePayoutCompleted;
        GameEvents.OnShakeRequested -= HandleShakeRequest;
    }

    void Update()
    {
        if (FullScreen)
        {
            if (FullScreen.gameObject.activeSelf)
            {
                FullScreenImage.sprite = Screen.fullScreen ? spriteFullScreen : spriteWindowed;
            }
        }

        if (playRotateImg)
        {
            var b = CheckShouldRotate();
            playRotateImg.gameObject.SetActive(b);

            if (!WG_SlotMachine.Instance.AreAllReelsCompleted())
            {
                float direction = clockwise ? -1f : 1f;
                float rotationAmount = direction * rotationSpeed * Time.deltaTime;
                playRotateTransform.Rotate(0, 0, rotationAmount, Space.Self);

                if (animTypeBtn.Count != 0)
                {
                    for (int i = 0; i < animTypeBtn.Count; i++)
                    {
                        animTypeBtn[i].interactable = false;
                    }
                }
            }
            else
            {
                if (playRotateImg.transform.rotation != Quaternion.identity)
                {
                    playRotateTransform.rotation = Quaternion.identity;
                }

                if (animTypeBtn.Count != 0)
                {
                    for (int i = 0; i < animTypeBtn.Count; i++)
                    {
                        animTypeBtn[i].interactable = true;
                    }
                }
            }
        }

        if (GameManager.Instance.AutoSpins <= 0)
        {
            //ExitAutoSpin();
            popUpManager.AutoSpinManager.onAutoSpin = false;
            if (onAutoSpin)
            {
                onAutoSpin = false;
                if (betBtn) betBtn.interactable = true;
            }
        }

        if (popUpManager.AutoSpinManager && popUpManager.AutoSpinManager.onAutoSpin && GameManager.Instance.AutoSpins > 0 && GameUtils.HasSufficientCredits(GameManager.Instance.Credits, GameManager.Instance.Bet))
        {
            onAutoSpin = true;
            if (betBtn) betBtn.interactable = false;
        }
        if (GameManager.Instance.AutoSpins > 0)
        {
            if (!GameUtils.HasSufficientCredits(GameManager.Instance.Credits, GameManager.Instance.Bet))
            {
                Debug.Log("Saldo insuficiente para continuar o Auto Spin.");
                autoPlayBtn.isOn = false;
                popUpManager.AutoSpinManager.onAutoSpin = false;
                GameManager.Instance.AutoSpins = 0;
            }

            if (autoSpinsAmountText) autoSpinsAmountText.text = $"{GameManager.Instance.AutoSpins} x";


            if (autoSpinValue)
            {
                autoSpinValue.text = GameUtils.FormatCurrency((GameManager.Instance.Bet) * GameManager.Instance.AutoSpins);
            }
        }
        else
        {
            if (autoSpinsAmountText) autoSpinsAmountText.text = "";

            if (autoSpinValue)
            {
                autoSpinValue.text = "";
            }
        }
        if (autoPlayBtn)
        {
            if (GameManager.Instance.AutoSpins <= 0)
            {
                autoPlayBtn.isOn = false;
            }
            else
            {
                autoPlayBtn.isOn = true;
            }
        }
    }

    // Handlers
    private void HandleBalanceUpdated(int balanceInCents)
    {
        // Atualiza o texto do saldo na UI
        if (creditsText != null)
            creditsText.text = GameUtils.FormatCurrency(balanceInCents);

        Debug.Log($"[CanvasManager] Balance UI updated: {GameUtils.FormatCurrency(balanceInCents)}");
    }

    private void HandleGainUpdated(int gainInCents)
    {
        // Atualiza o texto do ganho na UI
        if (gainText != null)
            gainText.text = GameUtils.FormatCurrency(gainInCents);
    }

    private void HandleBetChanged(int betInCents)
    {
        if (betText != null)
            betText.text = GameUtils.FormatCurrency(betInCents);
    }

    private void HandleMessageShow(string title, string message)
    {
        ShowMessage(message, title);
    }

    private void HandleAutoSpinStopped()
    {
        if (autoPlayBtn != null)
            autoPlayBtn.isOn = false;
    }

    private void HandlePayoutEffect(int amount, int symbolId)
    {
        // Efeito visual de payout (shake, etc)
       // if (onShake && gainText != null)
          //  StartCoroutine(Shake(gainText.transform));
    }

    private void HandlePayoutCompleted()
    {
        // Reabilitar botões se necessário
        EnableDisableInterfaceButtons(true, true, true, true);
    }

    private void HandleShakeRequest(GameEvents.ShakeTarget target)
    {
        switch (target)
        {
            case GameEvents.ShakeTarget.GainText:
                if (gainText != null && gainText.gameObject.activeInHierarchy)
                    StartCoroutine(GameUtils.Shake(gainText.transform));
                break;

            case GameEvents.ShakeTarget.CreditsText:
                if (creditsText != null && creditsText.gameObject.activeInHierarchy)
                    StartCoroutine(GameUtils.Shake(creditsText.transform));
                break;

            case GameEvents.ShakeTarget.PlayButton:
                if (playBtn != null && playBtn.gameObject.activeInHierarchy)
                    StartCoroutine(GameUtils.Shake(playBtn.transform));
                break;

            case GameEvents.ShakeTarget.BetButton:
                if (betBtn != null && betBtn.gameObject.activeInHierarchy)
                    StartCoroutine(GameUtils.Shake(betBtn.transform));
                break;

            case GameEvents.ShakeTarget.All:
                HandleShakeRequest(GameEvents.ShakeTarget.GainText);
                HandleShakeRequest(GameEvents.ShakeTarget.CreditsText);
                HandleShakeRequest(GameEvents.ShakeTarget.PlayButton);
                HandleShakeRequest(GameEvents.ShakeTarget.BetButton);
                break;
        }
    }

    IEnumerator AutoSpinRoutine()
    {
        yield return new WaitUntil(() => WG_SlotMachine.Instance.AreAllReelsCompleted());
        yield return new WaitForSeconds(1);
        if (WG_SlotMachine.Instance.AreAllReelsCompleted() && !BonusManager.Instance.isAnimating && !PayoutAnimation.Instance.isPayout && !isProcessingPlayResponse)
        {
            isProcessingPlayResponse = true;
            popUpManager.AutoSpinManager.PlayAutoSpin();
        }
    }

    public void BuyBonusRequest()
    {
        if (GameManager.Instance.AutoSpins > 0)
        {
            //ExitAutoSpin();
            return;
        }
        if (!GameManager.Instance.Playing && WG_SlotMachine.Instance.AreAllReelsCompleted())
        {
            if (GameUtils.HasSufficientCredits(GameManager.Instance.Credits, GameManager.Instance.Bet))
            {
                //GameManager.Instance.Credits -= GameManager.Instance.Bet;
            }
            else
            {
                ShowNoBalanceMessage();
                return;
            }
            //}
            if (!GameManager.Instance.ClickPlay && WG_SlotMachine.Instance.AreAllReelsCompleted() && !GameManager.Instance.Playing)
            {
                //WebClient.Instance.PlayRequest();
                //WebClient.Instance.BuyBonusRequest();
                //WG_SlotMachine.Instance.BetPlayBtn();
                WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_play);
                GameManager.Instance.Play();
            }

        }
        else if (GameManager.Instance.Playing && !GameManager.Instance.ClickPlay && !WG_SlotMachine.Instance.AreAllReelsCompleted())
        {
            //if (Board.Instance.canStop)
            //{
            //    Board.Instance.skipRoll = true;
            //}
        }
        else
        {
            Debug.Log("Aguarde o término da rodada atual.");
        }
    }
    public void BuyEventRequest()
    {
        if (GameManager.Instance.AutoSpins > 0)
        {
            return;
        }
        if (!GameManager.Instance.Playing && WG_SlotMachine.Instance.AreAllReelsCompleted())
        {

            if (GameUtils.HasSufficientCredits(GameManager.Instance.Credits, GameManager.Instance.Bet))
            {
                //GameManager.Instance.Credits -= GameManager.Instance.Bet;
            }
            else
            {
                ShowNoBalanceMessage();
                return;
            }
            //}
            if (!GameManager.Instance.ClickPlay && WG_SlotMachine.Instance.AreAllReelsCompleted() && !GameManager.Instance.Playing)
            {
                WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_play);
                GameManager.Instance.Play();
            }

        }
        else if (GameManager.Instance.Playing && !GameManager.Instance.ClickPlay && !WG_SlotMachine.Instance.AreAllReelsCompleted())
        {
            //if (Board.Instance.canStop)
            //{
            //    Board.Instance.skipRoll = true;
            //}
        }
        else
        {
            Debug.Log("Aguarde o término da rodada atual.");
        }
    }
    IEnumerator CheckPlayResponse()
    {
        yield return new WaitForSeconds(5);
        if (isProcessingPlayResponse) isProcessingPlayResponse = false;
        //PayoutAnimation.Instance.isPayout = false;
    }

    public IEnumerator PlayBonus()
    {
        yield return new WaitUntil(() => !BonusManager.Instance.isAnimatingBonus);
        yield return new WaitUntil(() => WG_SlotMachine.Instance.AreAllReelsCompleted());
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => SlotClient.Instance.bonusResponse != null && SlotClient.Instance.bonusResponse.freespinsRemaining > 0);
        //WebClient.Instance.PlayRequest();
        if (GameManager.Instance.onReplay)
        {
            HistoryUIManager.Instance.PlayBonus();
        }
        else
        {
           // WebClient.Instance.PlayRequest();
        }
        //WG_SlotMachine.Instance.BetPlayBtn();
        WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_play);
        GameManager.Instance.Play();
        StartCoroutine(CreateParticlePlay());
    }

    private bool CheckShouldRotate()
    {
        // Se todos completaram, não gira
        if (WG_SlotMachine.Instance.AreAllReelsCompleted())
            return false;

        // Verifica se pelo menos um reel está spinning
        foreach (WG_LineRows line in WG_SlotMachine.Instance.lineRows)
        {
            foreach (WG_SlotMachineReel reel in line.m_reels)
            {
                if (reel.AnimationType == ReelAnimationType.IndependentRows)
                {
                    if (reel.m_spinning)
                        return true;
                    if (reel.m_stoppingEnd)
                        return true;
                }
                else
                {
                    if(!reel.AreReelsCascadeCompleted()) return true;
                }
            }
        }

        return false;
    }

    public void OpenConfig()
    {
        if (popUpManager)
        {
            popUpManager.OpenSettingsPanel(true);
        }
    }
    public void OpenAutoPlayMenu()
    {
        if (popUpManager)
        {
            popUpManager.OpenAutoPlayPanel(true);
        }
    }
    public void UpdateBalanceText(int value)
    {
        string formattedValue = GameUtils.FormatCurrency(value);
        if (value < 0)
        {
            formattedValue = "err";
        }
        if(creditsText) creditsText.text = formattedValue;
    }
    public void UpdateBetText(int index)
    {
        string formattedValue = GameUtils.FormatCurrency(GameManager.Instance.Bet);
        if(betText) betText.text = formattedValue;
    }
    public void UpdateGainText(int gain)
    {
        string formattedValue2 = GameUtils.FormatCurrency(gain);
        if (gainText) gainText.text = formattedValue2;
    }
    public void EnableDisableInterfaceButtons(bool plus = true, bool turbo = true, bool auto = true, bool bet = true)
    {
        //if(playBtn) playBtn.interactable = plus;
        if(turboBtn) turboBtn.interactable = turbo;
        if(autoPlayBtn) autoPlayBtn.interactable = auto;
        if(betBtn) betBtn.interactable = bet;
    }

    public void AnimationReelsType(int val)
    {
        var sm = WG_SlotMachine.Instance;
        sm.gameSetup.slotPattern.animationType = (ReelAnimationType)val;
        sm.ExternalStart();
        SlotClient.Instance.playResponse = null;
        var slw = new List<WinningPatternInfo>();
        slw.Clear();
        SlotMathRunner.Instance.SetLastWinningPatterns = slw;
        sm.ClearAnimations();
        sm.PaymentSystem.StopCurrentPatternAnimation();
    }

    #region Template
    public void SetFullScreen()
    {
#if UNITY_WEBGL
        WebGLPluginJS.FullScreen();
#else
        Screen.fullScreen = !Screen.fullScreen;
#endif
    }

    public void GoToHome()
    {
        /*if ((WebClient.Instance.urlCallback != null) && (WebClient.Instance.urlCallback != string.Empty))
        {
            WebGLPluginJS.OpenURL(WebClient.Instance.urlCallback);
        }*/
    }

    public void ActivateHomeButton(bool active)
    {
        if(Home) Home.gameObject.SetActive(active);
    }

    public void HideMessagePanel()
    {
        messagepanel.SetActive(false);
    }
    public void ShowMessage(string message, string title)
    {
        WG_AudioManager.Instance.Play("message");
        messageText.text = message;
        messageTitle.text = title;
        messagepanel.SetActive(true);
    }
    public void ShowNoBalanceMessage()
    {
        ShowMessage(LanguageManager.instance.TryTranslate("no_balance", "Saldo insuficiente. Por favor, insira mais créditos para continuar jogando."),
                    LanguageManager.instance.TryTranslate("no_balance_title", "Saldo insuficiente"));
    }
    public void ShowLoading(bool v)
    {
        if(loading) loading.SetActive(v);
    }
    public void SetAutenticating(bool active)
    {
        if(connecting) connecting.GetComponent<TextMeshProUGUI>().text = active
            ?
            LanguageManager.instance.TryTranslate("autenticating", "Autenticando...")
            :
            LanguageManager.instance.TryTranslate("connecting", "Conectando...");
    }

    public void SetConnecting(bool active)
    {
        if(connecting) connecting.SetActive(active);
        if(playButtonObj) playButtonObj.SetActive(!active);
    }

    public void ActiveInitialScreen(bool active)
    {
        if(initialScreen) initialScreen.SetActive(active);
        if(gameScreen) gameScreen.gameObject.SetActive(!active);
    }
    void SetPlayButtonInteractable()
    {
        playButton.interactable = true;
    }

    public void CancelAutoSpin()
    {
        popUpManager.AutoSpinManager.onAutoSpin = false;
        GameManager.Instance.AutoSpins = 0;
        autoPlayBtn.isOn = false;
    }

    public void StartCreateParticlePlay()
    {
        StartCoroutine(CreateParticlePlay());
    }

    public IEnumerator CreateParticlePlay()
    {
        yield return new WaitForSeconds(0.2f);
        if (playParticleCur) Destroy(playParticleCur);
        playParticleCur = Instantiate(playParticlePrefab, playParticlePos.transform);
        //playParticleCur.transform.localPosition = Vector3.zero;
        playParticleCur.SetActive(true);
    }
    #endregion
}

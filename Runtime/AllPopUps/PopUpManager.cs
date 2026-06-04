using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WG_Casino;
public class PopUpManager : MonoBehaviour
{
    public static PopUpManager instance;

    [SerializeField] AutoSpinManager _autoSpinManager;
    [SerializeField] GameObject gameHistoryPrefab;
    [SerializeField] Transform gameHistoryTransform;
    [SerializeField] Image[] paymentIcons;
    [SerializeField] Sprite[] paymentIconsSprites;
    [SerializeField] TextMeshProUGUI[] paymentIconsText;
    [SerializeField] TextMeshProUGUI[] paymentIconsText2;
    [SerializeField] GameObject[] ptGO;
    [SerializeField] GameObject[] engGO;
    [SerializeField] GameObject[] espGO;

    [SerializeField] public TMP_Dropdown idiomaConfigDrop;
    [SerializeField] public Sprite[] idiomaImages;

    void Awake()
    {
        instance = this;
    }
    public AutoSpinManager AutoSpinManager => _autoSpinManager;

    [SerializeField] GameObject settingsPanel;
    public GameObject SettingsPanel
    {
        get => settingsPanel;
        set
        {
            settingsPanel = value;
        }
    }
    [SerializeField] GameObject autoSpinPanel;

    [SerializeField] Toggle turboSpinTgg;

    void Start()
    {
        turboSpinTgg.onValueChanged.AddListener((v) =>
        {
            if (v)
            {
                GameManager.Instance.Turbo = true;
            }
            else
            {
                GameManager.Instance.Turbo = false;
            }
            //CanvasManager.Instance.ChangeAutoSpinBtnColor(v);
        });

        //if (LanguageManager.instance.GetLanguage() == 0)
        //{
        //    for (int i = 0; i < ptGO.Length; i++)
        //    {
        //        ptGO[i].gameObject.SetActive(true);
        //        engGO[i].gameObject.SetActive(false);
        //        espGO[i].gameObject.SetActive(false);
        //    }
        //}
        //else if (LanguageManager.instance.GetLanguage() == 1)
        //{
        //    for (int i = 0; i < ptGO.Length; i++)
        //    {
        //        ptGO[i].gameObject.SetActive(false);
        //        engGO[i].gameObject.SetActive(true);
        //        espGO[i].gameObject.SetActive(false);

        //    }
        //}
        //else if (LanguageManager.instance.GetLanguage() == 2)
        //{
        //    for (int i = 0; i < ptGO.Length; i++)
        //    {
        //        ptGO[i].gameObject.SetActive(false);
        //        engGO[i].gameObject.SetActive(false);
        //        espGO[i].gameObject.SetActive(true);

        //    }
        //}

        //UpdatePaymentTable();

        idiomaConfigDrop.onValueChanged.AddListener(x => OnChangeIdiomaConfigDropdown(x));
    }

    void OnChangeIdiomaConfigDropdown(int x)
    {
        LanguageManager.instance.ChangeLanguage(x);
        WG_AudioManager.Instance.Play("tapbutton");
    }
    public void OpenSettingsPanel(bool v)
    {
        settingsPanel.SetActive(v);
        turboSpinTgg.SetIsOnWithoutNotify(GameManager.Instance.Turbo);
        CanvasManager.Instance.SymbolCam.SetActive(!v);
    }
    public void OpenAutoPlayPanel(bool v)
    {
        autoSpinPanel.SetActive(v);
    }

    public void RestartShowGainsAnimation()
    {
        //if (!GameManager.Instance.Playing)
        //{
        //    if (GameManager.Instance.showGainsCoroutine != null)
        //    {
        //        StopCoroutine(GameManager.Instance.showGainsCoroutine);
        //        GameManager.Instance.showGainsCoroutine = null;
        //    }
        //    GameManager.Instance.showGainsCoroutine = StartCoroutine(GameManager.Instance.ShowGainsInLoop());
        //}
        //CanvasManager.Instance.IconCamera.SetActive(true);
    }
    //public void CreateGameHistory(string gameType, string bet, string gain)
    //{
    //    // Verifica se já existem 20 filhos
    //    if (gameHistoryTransform.childCount >= 20)
    //    {
    //        // Destroi o último filho (o mais antigo)
    //        Destroy(gameHistoryTransform.GetChild(gameHistoryTransform.childCount - 1).gameObject);
    //    }

    //    // Instancia o novo histórico de jogo como o primeiro filho
    //    GameObject newHistory = Instantiate(gameHistoryPrefab, gameHistoryTransform);
    //    newHistory.transform.SetSiblingIndex(0);

    //    GameHistoryInfo info = newHistory.GetComponent<GameHistoryInfo>();
    //    info.SetValues(gameType, bet, gain);
    //}
    //public void UpdatePaymentTable()
    //{
    //    byte[] index = new byte[] { 0, 3, 4, 6, 7, 8, 9, 10, 11, 13, 14, 15 };
    //    for (int i = 0; i < paymentIcons.Length; i++)
    //    {
    //        paymentIcons[i].sprite = paymentIconsSprites[i];
    //    }
    //    for (int i = 0; i < index.Length; i++)
    //    {
    //        paymentIconsText[i].text = "x3: " +
    //            GameManager.Instance.databaseReelElementDto.elementArray[index[i]].data[0].combValue;
    //        paymentIconsText2[i].text = "x4: " +
    //            GameManager.Instance.databaseReelElementDto.elementArray[index[i]].data[1].combValue;
    //    }
    //}
}

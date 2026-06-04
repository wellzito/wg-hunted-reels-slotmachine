using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WG_Casino.SlotMachine;
using WG_Casino;

public class AutoSpinManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI creditText;
    [SerializeField] TextMeshProUGUI betText;
    [SerializeField] TextMeshProUGUI gainText;

    [SerializeField] Button startAutoSpinBtn;
    [SerializeField] Button closeBtn;
    [SerializeField] Button freeSpin10;
    [SerializeField] Button freeSpin25;
    [SerializeField] Button freeSpin50;
    [SerializeField] Button freeSpin100;
    [SerializeField] Button freeSpin1000;
    public bool onAutoSpin = false;
    // Start is called before the first frame update
    void Start()
    {
        startAutoSpinBtn.onClick.AddListener(() => PlayAutoSpin());
        closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
        freeSpin10.onClick.AddListener(() => SetGameManagerAutoSpinsAmount(10, freeSpin10));
        freeSpin25.onClick.AddListener(() => SetGameManagerAutoSpinsAmount(25, freeSpin25));
        freeSpin50.onClick.AddListener(() => SetGameManagerAutoSpinsAmount(50, freeSpin50));
        freeSpin100.onClick.AddListener(() => SetGameManagerAutoSpinsAmount(100, freeSpin100));
        freeSpin1000.onClick.AddListener(() => SetGameManagerAutoSpinsAmount(1000, freeSpin1000));

    }

    public void SetCreditTextValue(string text)
    {
        creditText.text = text;
    }
    public void SetBetTextTextValue(string text)
    {
        betText.text = text;
    }
    public void SetGainTextValue(string text)
    {
        gainText.text = text;
    }
    void OnEnable()
    {
        ResetAutoSpinConfig();
    }
    void DeselectAllButtons()
    {
        ColorBlock colors = freeSpin10.colors;
        colors.normalColor = Color.white; // Cor padrão quando não está selecionado
        freeSpin10.colors = colors;
        freeSpin25.colors = colors;
        freeSpin50.colors = colors;
        freeSpin100.colors = colors;
        freeSpin1000.colors = colors;
    }
    public void ResetAutoSpinConfig()
    {
        GameManager.Instance.AutoSpins = 0;

        startAutoSpinBtn.interactable = false;
        freeSpin10.interactable = true;
        freeSpin25.interactable = true;
        freeSpin50.interactable = true;
        freeSpin100.interactable = true;
        freeSpin1000.interactable = true;

        DeselectAllButtons();
    }
    public void SetGameManagerAutoSpinsAmount(int amount, Button button)
    {
        if (!GameManager.Instance.Playing)
        {
            startAutoSpinBtn.interactable = true;
            GameManager.Instance.AutoSpins = amount;
        }
        else
        {
            button.interactable = false;
            button.interactable = true;

        }
    }
    public void PlayAutoSpin()
    {
        GameEvents.RequesttAutoSpin(GameManager.Instance.AutoSpins);
        gameObject.SetActive(false);
    }
}

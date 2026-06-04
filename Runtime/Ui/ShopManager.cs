using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ShopManager : MonoBehaviour
{
    public GameObject panelShop;
    public GameObject panelConfirm;

    public Button shopBtn;
    public Button closeShopBtn;
    public Button minigameBtn;
    public Button vaultBtn;

    public Button confirmBtn;
    public Button cancelBtn;

    public Sprite selectSprite;
    public Sprite unselectSprite;

    enum BuysEvents
    {
        Minigame,
        Vault
    }

    private BuysEvents buysEvents;

    void Start()
    {
        shopBtn.onClick.AddListener(OpenShop);
        closeShopBtn.onClick.AddListener(CloseShop);
        minigameBtn.onClick.AddListener(OpenMinigame);
        vaultBtn.onClick.AddListener(OpenVault);
        confirmBtn.onClick.AddListener(ConfirmBuy);
        cancelBtn.onClick.AddListener(CancelBuy);
    }

    public void OpenShop()
    {
        panelShop.SetActive(true);
        minigameBtn.image.sprite = unselectSprite;
        vaultBtn.image.sprite = unselectSprite;
        CloseConfirm();
    }

    public void CloseShop()
    {
        panelShop.SetActive(false);
        CloseConfirm();
    }

    public void OpenConfirm()
    {
        panelConfirm.SetActive(true);
    }

    public void CloseConfirm()
    {
        panelConfirm.SetActive(false);
    }
    public void OpenMinigame()
    {
        buysEvents = BuysEvents.Minigame;
        minigameBtn.image.sprite = selectSprite;
        vaultBtn.image.sprite = unselectSprite;
        OpenConfirm();
    }
    public void OpenVault()
    {
        buysEvents = BuysEvents.Vault;
        minigameBtn.image.sprite = unselectSprite;
        vaultBtn.image.sprite = selectSprite;
        OpenConfirm();
    }

    public void ConfirmBuy()
    {
        if(buysEvents == BuysEvents.Minigame)
            CanvasManager.Instance.BuyBonusRequest();
        else if (buysEvents == BuysEvents.Vault)
            CanvasManager.Instance.BuyEventRequest();
        CloseConfirm();
        CloseShop();
    }

    public void CancelBuy()
    {
        CloseConfirm();
    }
}

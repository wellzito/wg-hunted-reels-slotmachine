using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoTranslate : MonoBehaviour
{
    public TextMeshProUGUI value;
    public Text value2;
    public string key;
    public Translate translateData;

    public string Key
    {
        get { return key; }
        set { key = value; ForceTranslate(); }
    }

    private void Awake()
    {
        value = GetComponent<TextMeshProUGUI>();
        value2 = GetComponent<Text>();
    }
    private void Start()
    {
        if (value != null)
            value.text = LanguageManager.instance?.TryTranslate(key, value.text);
        if (value2 != null)
            value2.text = LanguageManager.instance?.TryTranslate(key, value2.text);
        StartCoroutine(Rebuild());
    }
    private void OnValidate()
    {
        value = value ?? GetComponent<TextMeshProUGUI>();
        value2 = value2 ?? GetComponent<Text>();

        // Busca automaticamente o translateData se não estiver definido
        if (translateData == null && LanguageManager.instance != null)
        {
            translateData = LanguageManager.instance.translate;
        }
    }
    private void Reset()
    {
        value = GetComponent<TextMeshProUGUI>();
        value2 = GetComponent<Text>();
    }
    internal void ForceTranslate()
    {
        if (value != null)
            value.text = LanguageManager.instance.TryTranslate(key, value.text);
        else if (value2 != null)
            value2.text = LanguageManager.instance.TryTranslate(key, value2.text);
        StartCoroutine(Rebuild());
    }
    private void OnEnable()
    {
        if (LanguageManager.instance != null) LanguageManager.instance.RegisterComponent(this);

    }
    IEnumerator Rebuild()
    {
        var r = TryGetComponent<ContentSizeFitter>(out ContentSizeFitter csf);
        //Debug.Log(name + " Rebuild " + r);
        if (r)
        {
            yield return new WaitForEndOfFrame();
            csf.enabled = false;
            yield return new WaitForEndOfFrame();
            csf.enabled = true;
        }
    }
    private void OnDisable()
    {
        LanguageManager.instance?.UnregisterComponent(this);
    }
}
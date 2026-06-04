
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager instance;
    public Translate translate;
    [SerializeField] int currentLanguageId = 0;
    List<AutoTranslate> autoTranslates = new List<AutoTranslate>();

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
        //ChangeLanguage(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        ChangeLanguage(currentLanguageId);
        translate.Language = PlayerPrefs.GetInt("currentLanguage");
        instance = this;
    }
#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
        {
            ChangeLanguage(translate.Language += 1);
            StartCoroutine(Reload());
        }
    }
#endif
    IEnumerator Reload()
    {
        string scene = SceneManager.GetActiveScene().name;
        SceneManager.UnloadSceneAsync(scene);
        SceneManager.LoadScene("LoadScene");
        yield break;
    }
    public int GetLanguage()
    {
        return translate.Language;
    }
    public void RegisterComponent(AutoTranslate comp)
    {
        if (!autoTranslates.Contains(comp))
        {
            autoTranslates.Add(comp);
        }
    }
    public void UnregisterComponent(AutoTranslate comp)
    {
        if (autoTranslates.Contains(comp))
        {
            autoTranslates.Remove(comp);
        }
    }

    public void ChangeLanguage(int id)
    {
        translate.Language = id;
        PlayerPrefs.SetInt("currentLanguage", id);
        foreach (var item in autoTranslates)
        {
            item.ForceTranslate();
        }
    }
    public void ChangeLanguage(string culture)
    {
        int id = translate.GetLanguage(culture);
        translate.Language = id;
        foreach (var item in autoTranslates)
        {
            item.ForceTranslate();
        }
    }

    public string TryTranslate(string key, string defaultValue)
    {
        return translate.TryGetTranslate(key, defaultValue).Replace("\\n", "\n");
    }
}

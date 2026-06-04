
using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "translateData", menuName = "ScriptableObjects/translateData", order = 1)]
public class Translate : ScriptableObject
{
    int language;
    public List<Language> translates = new List<Language>();

    public int Language
    {
        get => language; set
        {
            language = value;
            if (value >= translates.Count)
                language = 0;
        }
    }
    public int GetLanguage(string culture)
    {
        for (int i = 0; i < translates.Count; i++)
        {
            if (translates[i].name.CompareTo(culture) == 0)
                return i;
        }
        return 0;
    }
    public string TryGetTranslate(string key, string defaultValue)
    {
        if (translates.Count <= Language)
        {
            return defaultValue;
        }
#if UNITY_EDITOR
        foreach (var item in translates)
        {
            item.CreateIfNotExist(key, defaultValue);
        }
        return translates[Language].TryGetTranslate(key, defaultValue);
#endif
        return translates[Language].TryGetTranslate(key, defaultValue);
    }
}
[Serializable]
public class Language
{
    public string name;
    public List<TranslateKeyValue> keyValues = new List<TranslateKeyValue>();

    internal void CreateIfNotExist(string key, string defaultValue)
    {
        var t = keyValues.Find(x => x.Key == key);
        if (t == null)
        {
            keyValues.Add(new TranslateKeyValue { Key = key, Value = defaultValue });
        }
    }

    internal string TryGetTranslate(string key, string defaultValue)
    {
        var t = keyValues.Find(x => x.Key == key);
        if (t == null)
        {
            return defaultValue;
        }
        return t.Value;
    }
}
[Serializable]
public class TranslateKeyValue
{
    public string Key;
    public string Value;
}
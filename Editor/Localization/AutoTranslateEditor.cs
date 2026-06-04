using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
[CustomEditor(typeof(AutoTranslate))]
public class AutoTranslateEditor : Editor
{
    private SerializedProperty keyProperty;
    private SerializedProperty translateDataProperty;
    private SerializedProperty valueProperty;
    private SerializedProperty value2Property;

    private int selectedLanguage = 0;
    private string newText = "";
    private bool editMode = false;
    private bool keyNotFound = false;

    private void OnEnable()
    {
        keyProperty = serializedObject.FindProperty("key");
        translateDataProperty = serializedObject.FindProperty("translateData");
        valueProperty = serializedObject.FindProperty("value");
        value2Property = serializedObject.FindProperty("value2");

        AutoTranslate at = (AutoTranslate)target;
        if (at.translateData == null)
        {
            FindTranslateData(at);
            serializedObject.Update();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        AutoTranslate at = (AutoTranslate)target;

        if (at.translateData == null)
        {
            FindTranslateData(at);
        }

        EditorGUILayout.PropertyField(keyProperty);
        EditorGUILayout.PropertyField(translateDataProperty);
        EditorGUILayout.PropertyField(valueProperty);
        EditorGUILayout.PropertyField(value2Property);

        if (at.translateData == null)
        {
            EditorGUILayout.HelpBox("Translate Data not found! Please assign a Translate ScriptableObject.", MessageType.Error);
            if (GUILayout.Button("Try to Find Translate Data"))
            {
                FindTranslateData(at);
            }
        }
        else if (!string.IsNullOrEmpty(at.key))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Translation Preview", EditorStyles.boldLabel);

            // Verifica se a key existe em todas as linguas
            keyNotFound = !KeyExistsInAllLanguages(at.translateData, at.key);

            if (keyNotFound)
            {
                EditorGUILayout.HelpBox($"Key '{at.key}' not found in all languages!", MessageType.Warning);

                // Pega o texto padrão (do componente de texto)
                string defaultText = "";
                if (at.value != null) defaultText = at.value.text;
                else if (at.value2 != null) defaultText = at.value2.text;

                if (GUILayout.Button($"Create New Key '{at.key}' in All Languages"))
                {
                    CreateKeyInAllLanguages(at.translateData, at.key, defaultText);
                    keyNotFound = false;
                    EditorUtility.SetDirty(at.translateData);
                    AssetDatabase.SaveAssets();
                }
            }
            else
            {
                // Seletor de idioma
                selectedLanguage = EditorGUILayout.Popup("Language", selectedLanguage, GetLanguageNames(at.translateData));

                // Mostra o texto atual
                string currentText = GetCurrentText(at, selectedLanguage);
                EditorGUILayout.LabelField("Current Text:", currentText);

                // Botão para editar
                if (GUILayout.Button(editMode ? "Cancel" : "Edit Text"))
                {
                    editMode = !editMode;
                    newText = currentText;
                    GUI.FocusControl(null);
                }

                // Campo de edição
                if (editMode)
                {
                    newText = EditorGUILayout.TextArea(newText, GUILayout.MinHeight(60));

                    if (GUILayout.Button("Save"))
                    {
                        SaveText(at, selectedLanguage, newText);
                        editMode = false;
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void FindTranslateData(AutoTranslate at)
    {
        if (LanguageManager.instance != null)
        {
            at.translateData = LanguageManager.instance.translate;
            EditorUtility.SetDirty(at);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Translate");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            at.translateData = AssetDatabase.LoadAssetAtPath<Translate>(path);
            EditorUtility.SetDirty(at);
        }
    }

    private bool KeyExistsInAllLanguages(Translate translateData, string key)
    {
        foreach (var language in translateData.translates)
        {
            if (language.keyValues.Find(x => x.Key == key) == null)
                return false;
        }
        return translateData.translates.Count > 0;
    }

    private void CreateKeyInAllLanguages(Translate translateData, string key, string defaultValue)
    {
        foreach (var language in translateData.translates)
        {
            if (language.keyValues.Find(x => x.Key == key) == null)
            {
                language.keyValues.Add(new TranslateKeyValue { Key = key, Value = defaultValue });
            }
        }
    }

    private string[] GetLanguageNames(Translate translateData)
    {
        string[] names = new string[translateData.translates.Count];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = translateData.translates[i].name;
        }
        return names;
    }

    private string GetCurrentText(AutoTranslate at, int languageIndex)
    {
        if (at.translateData.translates.Count > languageIndex)
        {
            var language = at.translateData.translates[languageIndex];
            var translation = language.keyValues.Find(x => x.Key == at.key);
            return translation != null ? translation.Value : "No translation found";
        }
        return "Invalid language index";
    }

    private void SaveText(AutoTranslate at, int languageIndex, string text)
    {
        if (at.translateData.translates.Count > languageIndex)
        {
            var language = at.translateData.translates[languageIndex];
            var translation = language.keyValues.Find(x => x.Key == at.key);

            if (translation == null)
            {
                language.keyValues.Add(new TranslateKeyValue { Key = at.key, Value = text });
            }
            else
            {
                translation.Value = text;
            }

            EditorUtility.SetDirty(at.translateData);
           // at.ForceTranslate();
        }
    }
}
#endif
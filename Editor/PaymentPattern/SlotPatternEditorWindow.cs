using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SlotPatternEditorWindow : EditorWindow
{
    private PaymentPattern currentPattern;
    private bool onStart;
    private int reelCount = 5;
    private int visibleRows = 3;
    private Vector2 scrollPosition;
    private Vector2Int selectedSlot = new Vector2Int(-1, -1);

    private Texture2D selectedTex;
    private Texture2D unselectedTex;
    private Texture2D editingTex;

    [MenuItem("Window/Slot Machine Pattern Editor")]
    public static void ShowWindow()
    {
        SlotPatternEditorWindow window = GetWindow<SlotPatternEditorWindow>("Editor de Padrões");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        selectedTex = CreateColorTexture(Color.green);
        unselectedTex = CreateColorTexture(Color.gray);
        editingTex = CreateColorTexture(new Color(0.5f, 0.5f, 1f));
    }

    private Texture2D CreateColorTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void OnGUI()
    {
        DrawHeader();
        if (currentPattern == null) return;

        DrawConfiguration();
        DrawSlotGrid();
        DrawSpriteEditingSection();
        DrawSaveButton();
    }

    private void DrawHeader()
    {
        GUILayout.Label("Editor de Padrões de Pagamento", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        currentPattern = (PaymentPattern)EditorGUILayout.ObjectField(
            "Padrão Atual",
            currentPattern,
            typeof(PaymentPattern),
            false);

        if (GUILayout.Button("Criar Novo", GUILayout.Width(100)))
        {
            CreateNewPattern();
        }
        EditorGUILayout.EndHorizontal();

        if (currentPattern != null && !onStart)
        {
            LoadPatternSettings();
            onStart = true;
        }
    }

    private void LoadPatternSettings()
    {
        reelCount = currentPattern.reelCount;
        visibleRows = currentPattern.visibleRows;
    }

    private void CreateNewPattern()
    {
        currentPattern = CreateInstance<PaymentPattern>();
        currentPattern.reelCount = reelCount;
        currentPattern.visibleRows = visibleRows;
        currentPattern.patternName = "New Pattern";
        currentPattern.patternId = System.Guid.NewGuid().GetHashCode();

        string path = EditorUtility.SaveFilePanelInProject(
            "Salvar Padrão",
            "NewPaymentPattern",
            "asset",
            "Digite um nome para o padrão");

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(currentPattern, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoadPatternSettings();
            EditorUtility.SetDirty(currentPattern);
        }
    }

    private void DrawConfiguration()
    {
        GUILayout.Space(10);
        GUILayout.Label("Configuração", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        reelCount = EditorGUILayout.IntSlider("Número de Reels", reelCount, 3, 5);
        visibleRows = EditorGUILayout.IntSlider("Linhas Visíveis", visibleRows, 3, 5);

        if (EditorGUI.EndChangeCheck() && currentPattern != null)
        {
            currentPattern.reelCount = reelCount;
            currentPattern.visibleRows = visibleRows;
            EditorUtility.SetDirty(currentPattern);
        }

        EditorGUILayout.HelpBox($"Grid: {visibleRows} rows x {reelCount} reels", MessageType.Info);
    }

    private void DrawSlotGrid()
    {
        GUILayout.Space(10);
        GUILayout.Label($"Grid de Seleção ({visibleRows} x {reelCount})", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Cabeçalho
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(60);
        for (int reel = 0; reel < reelCount; reel++)
        {
            GUIStyle reelStyle = new GUIStyle(EditorStyles.boldLabel);
            reelStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label($"Reel {reel}", reelStyle, GUILayout.Width(50));
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Linhas 0 a visibleRows-1
        for (int row = 0; row < visibleRows; row++)
        {
            EditorGUILayout.BeginHorizontal();

            string rowLabel = row == 0 ? "Topo (L0)" : (row == 1 ? "Meio (L1)" : (row == 2 ? "Baixo (L2)" : $"Linha {row}"));
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            if (row == 1) labelStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label(rowLabel, labelStyle, GUILayout.Width(60));

            for (int reel = 0; reel < reelCount; reel++)
            {
                bool isSelected = currentPattern.IsSlotSelected(reel, row);
                bool isEditing = selectedSlot.x == reel && selectedSlot.y == row;

                GUIStyle style = new GUIStyle(GUI.skin.button)
                {
                    fixedWidth = 50,
                    fixedHeight = 50,
                    normal = { background = isEditing ? editingTex : (isSelected ? selectedTex : unselectedTex) }
                };

                Rect buttonRect = GUILayoutUtility.GetRect(50, 50, style, GUILayout.Width(50), GUILayout.Height(50));

                if (GUI.Button(buttonRect, "", style))
                {
                    HandleSlotClick(reel, row, isSelected);
                }

                // Desenhar sprite se selecionado
                if (isSelected)
                {
                    DrawSlotSprite(buttonRect, reel, row);
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        // Legenda
        EditorGUILayout.BeginHorizontal();
        DrawLegendItem(selectedTex, "Selecionado");
        DrawLegendItem(unselectedTex, "Não Selecionado");
        DrawLegendItem(editingTex, "Editando");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Clique esquerdo: Selecionar/Deselecionar slot\n" +
            "Clique direito: Editar sprite do slot\n" +
            $"Total de slots selecionados: {currentPattern.selectedSlots.Count}",
            MessageType.Info);
    }

    private void DrawLegendItem(Texture2D tex, string label)
    {
        GUILayout.BeginHorizontal(GUILayout.Width(130));
        GUILayout.Box(tex, GUILayout.Width(20), GUILayout.Height(20));
        GUILayout.Label(label, GUILayout.Width(100));
        GUILayout.EndHorizontal();
    }

    private void HandleSlotClick(int reel, int row, bool isSelected)
    {
        if (Event.current.button == 0) // Left click
        {
            if (isSelected)
            {
                currentPattern.RemoveSlot(reel, row);
                if (selectedSlot.x == reel && selectedSlot.y == row)
                    selectedSlot = new Vector2Int(-1, -1);
            }
            else
            {
                currentPattern.AddSlot(reel, row);
                selectedSlot = new Vector2Int(reel, row);
            }
            EditorUtility.SetDirty(currentPattern);
        }
        else if (Event.current.button == 1 && isSelected) // Right click
        {
            selectedSlot = new Vector2Int(reel, row);
        }
    }

    private void DrawSlotSprite(Rect buttonRect, int reel, int row)
    {
        var slot = currentPattern.selectedSlots.Find(s => s.reelIndex == reel && s.rowIndex == row);
        if (slot != null && slot.slotSprite != null)
        {
            Sprite sprite = slot.slotSprite;
            Rect texCoords = new Rect(
                sprite.rect.x / sprite.texture.width,
                sprite.rect.y / sprite.texture.height,
                sprite.rect.width / sprite.texture.width,
                sprite.rect.height / sprite.texture.height);

            Rect spriteRect = new Rect(
                buttonRect.x + 5,
                buttonRect.y + 5,
                buttonRect.width - 10,
                buttonRect.height - 10);

            GUI.DrawTextureWithTexCoords(spriteRect, sprite.texture, texCoords);
        }
    }

    private void DrawSpriteEditingSection()
    {
        if (selectedSlot.x < 0 || selectedSlot.y < 0) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor de Sprite", EditorStyles.boldLabel);

        var slot = currentPattern.selectedSlots.Find(s =>
            s.reelIndex == selectedSlot.x &&
            s.rowIndex == selectedSlot.y);

        if (slot != null)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Editando: Reel {selectedSlot.x}, Row {selectedSlot.y}");

            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(
                "Sprite do Slot",
                slot.slotSprite,
                typeof(Sprite),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                slot.slotSprite = newSprite;
                EditorUtility.SetDirty(currentPattern);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Limpar Sprite"))
            {
                slot.slotSprite = null;
                EditorUtility.SetDirty(currentPattern);
            }

            if (GUILayout.Button("Fechar Editor"))
            {
                selectedSlot = new Vector2Int(-1, -1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawSaveButton()
    {
        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Salvar Padrão", GUILayout.Height(35)))
        {
            EditorUtility.SetDirty(currentPattern);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Padrão '{currentPattern.name}' salvo!");
        }

        if (GUILayout.Button("Salvar e Fechar", GUILayout.Height(35)))
        {
            EditorUtility.SetDirty(currentPattern);
            AssetDatabase.SaveAssets();
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }

    public void SetPattern(PaymentPattern pattern)
    {
        currentPattern = pattern;
        if (pattern != null)
        {
            LoadPatternSettings();
        }
        onStart = false;
        Repaint();
    }
}
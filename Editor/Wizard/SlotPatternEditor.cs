using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using WG_Casino.SlotMachine;

[CustomEditor(typeof(SlotPattern))]
public class SlotPatternEditor : Editor
{
    private SlotPattern pattern;
    private bool[,] slotGrid;
    private Vector2 scrollPosition;

    private Texture2D activeTex;
    private Texture2D inactiveTex;
    private Texture2D externalActiveTex;
    private Texture2D externalInactiveTex;

    private void OnEnable()
    {
        pattern = (SlotPattern)target;

        activeTex = CreateColorTexture(Color.green);
        inactiveTex = CreateColorTexture(Color.gray);
        externalActiveTex = CreateColorTexture(new Color(0f, 0.5f, 0f)); // Verde escuro
        externalInactiveTex = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f)); // Cinza escuro

        // Inicializa a grade a partir dos dados do pattern
        InitializeGridFromPattern();
    }

    private Texture2D CreateColorTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void InitializeGridFromPattern()
    {
        if (pattern.reelsInfos == null || pattern.reelsInfos.Count == 0)
        {
            // Se não há dados, cria uma grade padrão
            slotGrid = new bool[pattern.reelCount, pattern.totalRows];
            for (int reel = 0; reel < pattern.reelCount; reel++)
            {
                for (int row = 0; row < pattern.totalRows; row++)
                {
                    slotGrid[reel, row] = true;
                }
            }
        }
        else
        {
            // Carrega a grade a partir dos dados existentes
            slotGrid = new bool[pattern.reelCount, pattern.totalRows];
            for (int reel = 0; reel < pattern.reelsInfos.Count; reel++)
            {
                for (int row = 0; row < pattern.reelsInfos[reel].slotsInfos.Count; row++)
                {
                    if (reel < pattern.reelCount && row < pattern.totalRows)
                    {
                        slotGrid[reel, row] = pattern.reelsInfos[reel].slotsInfos[row].onRow;
                    }
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();
        DrawConfiguration();
        DrawAnimationSettings();
        DrawGameGrid();
        DrawRemainingSlots();
        DrawControlButtons();

        // Draw default inspector para propriedades não customizadas
        EditorGUILayout.Space();
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Slot Pattern Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Editing: {pattern.patternName}", EditorStyles.miniBoldLabel);
        GUILayout.Space(10);
    }

    private void DrawConfiguration()
    {
        EditorGUILayout.LabelField("Configurações da Grade", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        int newReelCount = EditorGUILayout.IntSlider("Número de Fileiras", pattern.reelCount, 3, 8);
        int newVisibleRows = EditorGUILayout.IntSlider("Grid do Jogo (Linhas Visíveis)", pattern.visibleRows, 3, 12);

        if (EditorGUI.EndChangeCheck())
        {
            // Atualiza as dimensões
            pattern.reelCount = newReelCount;
            pattern.visibleRows = newVisibleRows;

            // Redimensiona a grade mantendo os valores existentes quando possível
            bool[,] newGrid = new bool[newReelCount, pattern.totalRows];

            for (int reel = 0; reel < newReelCount; reel++)
            {
                for (int row = 0; row < pattern.totalRows; row++)
                {
                    if (reel < slotGrid.GetLength(0))
                    {
                        newGrid[reel, row] = slotGrid[reel, row];
                    }
                    else
                    {
                        newGrid[reel, row] = true; // Padrão para novas células
                    }
                }
            }

            slotGrid = newGrid;
            SaveChanges();
        }

        GUILayout.Label($"Total de Linhas: {pattern.totalRows} (fixo)", EditorStyles.label);
        GUILayout.Label($"Grid do jogo: {pattern.visibleRows} linhas (L0 até L{pattern.visibleRows - 1})", EditorStyles.helpBox);
        GUILayout.Label($"Slots sobrando: {pattern.totalRows - pattern.visibleRows} linhas (L{pattern.visibleRows} até L{pattern.totalRows - 1})", EditorStyles.helpBox);

        GUILayout.Space(10);
    }

    private void DrawAnimationSettings()
    {
        EditorGUILayout.LabelField("Configurações de Animação", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        pattern.animationType = (ReelAnimationType)EditorGUILayout.EnumPopup("Tipo de Animação", pattern.animationType);
        pattern.onShakeSlot = EditorGUILayout.Toggle("Shake no Slot", pattern.onShakeSlot);

        if (EditorGUI.EndChangeCheck())
        {
            SaveChanges();
        }

        // Descrição do tipo de animação selecionado
        string description = GetAnimationTypeDescription(pattern.animationType);
        EditorGUILayout.HelpBox(description, MessageType.Info);

        GUILayout.Space(10);
    }
    private string GetAnimationTypeDescription(ReelAnimationType type)
    {
        switch (type)
        {
            case ReelAnimationType.IndependentRows:
                return "Efeito de linhas indepentes";
            /*case ReelAnimationType.Bounce:*/
                return "Movimento de vai-e-vem";
            case ReelAnimationType.CascadeFall:
                return "Efeito cascata entre os reels";
            /*case ReelAnimationType.Random:
                return "Comportamento aleatório";*/
            default:
                return "Tipo de animação não definido";
        }
    }
    private void DrawGameGrid()
    {
        EditorGUILayout.LabelField($"Grid do Jogo ({pattern.visibleRows}x{pattern.reelCount}) - Clique para Ativar/Desativar", EditorStyles.boldLabel);

        // Header com índices das fileiras
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(60));
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            GUILayout.Label($"F{reel}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(20));
        }
        GUILayout.EndHorizontal();

        // CORREÇÃO: Renderiza as linhas do grid do jogo de CIMA PARA BAIXO (L0 até L[visibleRows-1])
        for (int row = 0; row < pattern.visibleRows; row++) // ← Mudou para ordem crescente
        {
            GUILayout.BeginHorizontal();

            // Rótulo da linha
            GUILayout.Label($"L{row}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(50));

            for (int reel = 0; reel < pattern.reelCount; reel++)
            {
                bool isActive = slotGrid[reel, row];

                GUIStyle style = new GUIStyle(GUI.skin.button)
                {
                    normal = { background = isActive ? activeTex : inactiveTex },
                    fixedWidth = 50,
                    fixedHeight = 50
                };

                if (GUILayout.Button("", style))
                {
                    // Toggle do estado do slot
                    slotGrid[reel, row] = !slotGrid[reel, row];
                    SaveChanges();
                }

                // Desenha o índice dentro do botão
                Rect lastRect = GUILayoutUtility.GetLastRect();
                GUI.Label(lastRect, $"{reel},{row}", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndHorizontal();
        }

        // Legenda do grid do jogo
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Verde = Ativo, Cinza = Inativo", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void DrawRemainingSlots()
    {
        EditorGUILayout.LabelField($"Slots Sobrando (L{pattern.visibleRows} até L{pattern.totalRows - 1}) - Clique para Ativar/Desativar", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        // Header com índices das fileiras
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(60));
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            GUILayout.Label($"F{reel}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(20));
        }
        GUILayout.EndHorizontal();

        // CORREÇÃO: Renderiza as linhas sobrando de CIMA PARA BAIXO (L[visibleRows] até L[totalRows-1])
        for (int row = pattern.visibleRows; row < pattern.totalRows; row++) // ← Mudou para ordem crescente
        {
            GUILayout.BeginHorizontal();

            // Rótulo da linha
            GUILayout.Label($"L{row}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(50));

            for (int reel = 0; reel < pattern.reelCount; reel++)
            {
                bool isActive = slotGrid[reel, row];

                GUIStyle style = new GUIStyle(GUI.skin.button)
                {
                    normal = { background = isActive ? externalActiveTex : externalInactiveTex },
                    fixedWidth = 50,
                    fixedHeight = 50
                };

                if (GUILayout.Button("", style))
                {
                    // Toggle do estado do slot
                    slotGrid[reel, row] = !slotGrid[reel, row];
                    SaveChanges();
                }

                // Desenha o índice dentro do botão
                Rect lastRect = GUILayoutUtility.GetLastRect();
                GUI.Label(lastRect, $"{reel},{row}", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // Legenda dos slots sobrando
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Verde escuro = Ativo, Cinza escuro = Inativo", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void DrawControlButtons()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Ativar Todos"))
        {
            SetAllSlots(true);
        }

        if (GUILayout.Button("Desativar Todos"))
        {
            SetAllSlots(false);
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Ativar Apenas Grid do Jogo"))
        {
            SetGameGridOnly();
        }

        if (GUILayout.Button("Ativar Apenas Slots Sobrando"))
        {
            SetRemainingSlotsOnly();
        }

        if (GUILayout.Button("Inverter Todos"))
        {
            InvertAllSlots();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Botão para abrir no wizard
        if (GUILayout.Button("Abrir no Wizard", GUILayout.Height(30)))
        {
            SlotPatternWizard.ShowWindowForPattern(pattern);
        }

        // Botão para criar cópia
        if (GUILayout.Button("Criar Cópia", GUILayout.Height(25)))
        {
            CreateCopy();
        }
    }

    private void SetAllSlots(bool active)
    {
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            for (int row = 0; row < pattern.totalRows; row++)
            {
                slotGrid[reel, row] = active;
            }
        }
        SaveChanges();
    }

    private void SetGameGridOnly()
    {
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            for (int row = 0; row < pattern.totalRows; row++)
            {
                // Ativa apenas no grid do jogo, desativa os slots sobrando
                slotGrid[reel, row] = (row < pattern.visibleRows);
            }
        }
        SaveChanges();
    }

    private void SetRemainingSlotsOnly()
    {
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            for (int row = 0; row < pattern.totalRows; row++)
            {
                // Ativa apenas nos slots sobrando, desativa o grid do jogo
                slotGrid[reel, row] = (row >= pattern.visibleRows);
            }
        }
        SaveChanges();
    }

    private void InvertAllSlots()
    {
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            for (int row = 0; row < pattern.totalRows; row++)
            {
                slotGrid[reel, row] = !slotGrid[reel, row];
            }
        }
        SaveChanges();
    }

    private void SaveChanges()
    {
        pattern.GenerateReelsInfosFromGrid(slotGrid);
        EditorUtility.SetDirty(pattern);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Repaint();
    }

    private void CreateCopy()
    {
        // Cria uma cópia do pattern atual
        SlotPattern copy = ScriptableObject.CreateInstance<SlotPattern>();
        copy.patternName = pattern.patternName + " - Cópia";
        copy.reelCount = pattern.reelCount;
        copy.totalRows = pattern.totalRows;
        copy.visibleRows = pattern.visibleRows;
        copy.GenerateReelsInfosFromGrid(slotGrid);

        string path = AssetDatabase.GetAssetPath(pattern);
        string copyPath = Path.Combine(Path.GetDirectoryName(path), copy.patternName + ".asset");
        copyPath = AssetDatabase.GenerateUniqueAssetPath(copyPath);

        AssetDatabase.CreateAsset(copy, copyPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = copy;

        Debug.Log($"Cópia criada: {copyPath}");
    }
}
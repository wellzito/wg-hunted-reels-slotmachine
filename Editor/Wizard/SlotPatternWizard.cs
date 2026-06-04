using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using WG_Casino.SlotMachine;

public class SlotPatternWizard : ScriptableWizard
{
    [Header("Configurações da Grade")]
    public int reelCount = 5;
    public int totalRows = 12; // Total fixo de 12 slots por fileira
    public int visibleRows = 5; // Grid do jogo (3-5 linhas visíveis)
    public ReelAnimationType animationType = ReelAnimationType.IndependentRows;
    public bool onShakeSlot = false;

    [Header("Nome do Padrão")]
    public string patternName = "NovoPadraoSlots";

    [Header("Local de Salvamento")]
    public string saveFolderPath = "Assets/SlotPatterns/";

    private bool[,] slotGrid; // [reel, row] - true = ativo, false = inativo
    private Vector2 scrollPosition;

    private Texture2D activeTex;
    private Texture2D inactiveTex;
    private Texture2D externalActiveTex;
    private Texture2D externalInactiveTex;

    // Variável para edição de pattern existente
    private SlotPattern patternToEdit;
    private bool isEditing = false;

    [MenuItem("Tools/Slot Machine/Criar Padrão de Slots")]
    public static void CreateWizard()
    {
        SlotPatternWizard wizard = DisplayWizard<SlotPatternWizard>("Criar Padrão de Slots", "Criar");
        wizard.isEditing = false;
        wizard.InitializeGrid();
    }

    public static void ShowWindowForPattern(SlotPattern pattern)
    {
        SlotPatternWizard wizard = DisplayWizard<SlotPatternWizard>("Editar Padrão de Slots", "Salvar");
        wizard.LoadPattern(pattern);
    }

    void OnEnable()
    {
        activeTex = CreateColorTexture(Color.green);
        inactiveTex = CreateColorTexture(Color.gray);
        externalActiveTex = CreateColorTexture(new Color(0f, 0.5f, 0f)); // Verde escuro
        externalInactiveTex = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f)); // Cinza escuro
    }

    private Texture2D CreateColorTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    void OnWizardUpdate()
    {
        helpString = isEditing ? "Editando padrão existente - Clique nos slots para ativar/desativar"
                              : "Criando novo padrão - Clique nos slots para ativar/desativar";

        if (string.IsNullOrEmpty(patternName))
        {
            errorString = "Digite um nome para o padrão";
            isValid = false;
            return;
        }

        if (!isEditing && string.IsNullOrEmpty(saveFolderPath))
        {
            errorString = "Selecione uma pasta para salvar";
            isValid = false;
            return;
        }

        if (slotGrid == null || slotGrid.GetLength(0) != reelCount || slotGrid.GetLength(1) != totalRows)
        {
            InitializeGrid();
        }

        errorString = "";
        isValid = true;
    }

    void OnWizardCreate()
    {
        if (isEditing && patternToEdit != null)
        {
            // Modo edição - atualiza o pattern existente
            UpdateExistingPattern();
        }
        else
        {
            // Modo criação - cria novo pattern
            CreateNewPattern();
        }
    }

    private void CreateNewPattern()
    {
        // Cria o ScriptableObject
        SlotPattern newPattern = ScriptableObject.CreateInstance<SlotPattern>();

        // Configura as propriedades básicas
        newPattern.patternName = patternName;
        newPattern.reelCount = reelCount;
        newPattern.totalRows = totalRows;
        newPattern.visibleRows = visibleRows;
        newPattern.animationType = animationType;
        newPattern.onShakeSlot = onShakeSlot;

        // Gera a estrutura de dados no formato especificado
        newPattern.GenerateReelsInfosFromGrid(slotGrid);

        // Garante que a pasta existe
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }

        // Salva o asset
        string path = Path.Combine(saveFolderPath, patternName + ".asset");
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(newPattern, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Seleciona o asset criado
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newPattern;

        Debug.Log($"Padrão de slots criado: {path}");

        WG_SlotMachineSetupWizard.OnPatternCreated(newPattern);
    }

    private void UpdateExistingPattern()
    {
        // Atualiza as propriedades básicas
        patternToEdit.patternName = patternName;
        patternToEdit.reelCount = reelCount;
        patternToEdit.totalRows = totalRows;
        patternToEdit.visibleRows = visibleRows;
        patternToEdit.animationType = animationType;
        patternToEdit.onShakeSlot = onShakeSlot;
        // Gera a estrutura de dados no formato especificado
        patternToEdit.GenerateReelsInfosFromGrid(slotGrid);

        // Marca como modificado e salva
        EditorUtility.SetDirty(patternToEdit);
        AssetDatabase.SaveAssets();

        Debug.Log($"Padrão de slots atualizado: {patternToEdit.name}");

        WG_SlotMachineSetupWizard.OnPatternCreated(patternToEdit);
    }

    public void LoadPattern(SlotPattern pattern)
    {
        patternToEdit = pattern;
        isEditing = true;

        patternName = pattern.patternName;
        reelCount = pattern.reelCount;
        totalRows = pattern.totalRows;
        visibleRows = pattern.visibleRows;
        animationType = pattern.animationType;
        onShakeSlot = pattern.onShakeSlot;
        // Carrega a grade do pattern existente
        slotGrid = pattern.GetSlotGrid();
    }

    void InitializeGrid()
    {
        slotGrid = new bool[reelCount, totalRows];

        // Por padrão, ativa todos os slots
        for (int reel = 0; reel < reelCount; reel++)
        {
            for (int row = 0; row < totalRows; row++)
            {
                slotGrid[reel, row] = true;
            }
        }
    }

    protected override bool DrawWizardGUI()
    {
        EditorGUI.BeginChangeCheck();

        // Configurações básicas
        EditorGUILayout.Space();
        patternName = EditorGUILayout.TextField("Nome do Padrão", patternName);

        // Seleção de pasta apenas no modo criação
        if (!isEditing)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local de Salvamento", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            saveFolderPath = EditorGUILayout.TextField("Pasta", saveFolderPath);
            if (GUILayout.Button("Selecionar", GUILayout.Width(80)))
            {
                string selectedPath = EditorUtility.SaveFolderPanel("Selecionar Pasta para Salvar", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Converte para path relativo do projeto
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        saveFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        saveFolderPath = selectedPath;
                    }
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Selecione a pasta onde o padrão será salvo", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Editando padrão existente", MessageType.Info);
        }

        // Configurações da grade
        EditorGUILayout.Space();
        int newReelCount = EditorGUILayout.IntSlider("Número de Fileiras", reelCount, 3, 8);
        visibleRows = EditorGUILayout.IntSlider("Grid do Jogo (Linhas Visíveis)", visibleRows, 3, 12);

        // Tipo de animação
        EditorGUILayout.Space();
        animationType = (ReelAnimationType)EditorGUILayout.EnumPopup("Tipo de Animação", animationType);
        EditorGUILayout.HelpBox(GetAnimationTypeDescription(animationType), MessageType.Info);

        onShakeSlot = EditorGUILayout.Toggle("Shake no Slot", onShakeSlot);

        // Se as dimensões mudaram, reinicializa a grade
        if (newReelCount != reelCount)
        {
            reelCount = newReelCount;
            InitializeGrid();
        }

        GUILayout.Label($"Total de Linhas: {totalRows} (fixo)", EditorStyles.label);
        GUILayout.Label($"Grid do jogo: {visibleRows} linhas (L0 até L{visibleRows - 1})", EditorStyles.helpBox);
        GUILayout.Label($"Slots sobrando: {totalRows - visibleRows} linhas (L{visibleRows} até L{totalRows - 1})", EditorStyles.helpBox);

        // Grade do Jogo (Visível)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Grid do Jogo ({visibleRows}x{reelCount}) - Clique para Ativar/Desativar", EditorStyles.boldLabel);

        DrawGameGrid();

        // Slots Sobrando
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Slots Sobrando (L{visibleRows} até L{totalRows - 1}) - Clique para Ativar/Desativar", EditorStyles.boldLabel);

        DrawRemainingSlots();

        // Botões de controle
        EditorGUILayout.Space();
        DrawControlButtons();

        return EditorGUI.EndChangeCheck();
    }

    private string GetAnimationTypeDescription(ReelAnimationType type)
    {
        switch (type)
        {
            case ReelAnimationType.IndependentRows:
                return "Efeito de linhas indepentes";
                /* case ReelAnimationType.StopAndGo:
                     return "Para e reinicia periodicamente";
                 case ReelAnimationType.Bounce:*/
                return "Movimento de vai-e-vem";
            case ReelAnimationType.CascadeFall:
                return "Efeito cascata entre os reels";
           /* case ReelAnimationType.Random:
                return "Comportamento aleatório";*/
            default:
                return "Tipo de animação não definido";
        }
    }

    void DrawGameGrid()
    {
        // Header com índices das fileiras
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(60));
        for (int reel = 0; reel < reelCount; reel++)
        {
            GUILayout.Label($"F{reel}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(20));
        }
        GUILayout.EndHorizontal();

        // CORREÇÃO: Renderiza as linhas do grid do jogo de CIMA PARA BAIXO (L0 até L[visibleRows-1])
        for (int row = 0; row < visibleRows; row++) // ← Mudou para ordem crescente
        {
            GUILayout.BeginHorizontal();

            // Rótulo da linha
            GUILayout.Label($"L{row}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(50));

            for (int reel = 0; reel < reelCount; reel++)
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
    }

    void DrawRemainingSlots()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        // Header com índices das fileiras
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(60));
        for (int reel = 0; reel < reelCount; reel++)
        {
            GUILayout.Label($"F{reel}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(20));
        }
        GUILayout.EndHorizontal();

        // CORREÇÃO: Renderiza as linhas sobrando de CIMA PARA BAIXO (L[visibleRows] até L[totalRows-1])
        for (int row = visibleRows; row < totalRows; row++) // ← Mudou para ordem crescente
        {
            GUILayout.BeginHorizontal();

            // Rótulo da linha
            GUILayout.Label($"L{row}", EditorStyles.boldLabel, GUILayout.Width(50), GUILayout.Height(50));

            for (int reel = 0; reel < reelCount; reel++)
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
    }

    void DrawControlButtons()
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
    }

    void SetAllSlots(bool active)
    {
        for (int reel = 0; reel < reelCount; reel++)
        {
            for (int row = 0; row < totalRows; row++)
            {
                slotGrid[reel, row] = active;
            }
        }
    }

    void SetGameGridOnly()
    {
        for (int reel = 0; reel < reelCount; reel++)
        {
            for (int row = 0; row < totalRows; row++)
            {
                // Ativa apenas no grid do jogo, desativa os slots sobrando
                slotGrid[reel, row] = (row < visibleRows);
            }
        }
    }

    void SetRemainingSlotsOnly()
    {
        for (int reel = 0; reel < reelCount; reel++)
        {
            for (int row = 0; row < totalRows; row++)
            {
                // Ativa apenas nos slots sobrando, desativa o grid do jogo
                slotGrid[reel, row] = (row >= visibleRows);
            }
        }
    }

    void InvertAllSlots()
    {
        for (int reel = 0; reel < reelCount; reel++)
        {
            for (int row = 0; row < totalRows; row++)
            {
                slotGrid[reel, row] = !slotGrid[reel, row];
            }
        }
    }
}
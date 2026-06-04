using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using WG_Casino.SlotMachine;
using WG_Casino.Systems;
using WG_Casino.Setup;
using WG_Casino;

public class WG_SlotMachineSetupWizard : EditorWindow
{
    private enum SetupStep
    {
        Welcome,
        FindOrCreateMachine,
        CreateSlotPattern,
        CreateGameSetup,
        Complete
    }

    private SetupStep currentStep = SetupStep.Welcome;
    private Vector2 scrollPosition;

    //Prefabs
    private GameObject canvasPrefab;
    private GameObject lineSlotsPrefab;
    private GameObject inputManagerPrefab;
    private GameObject audioManagerPrefab;
    private Transform maskTransform;

    // Step 1: Machine References
    private WG_SlotMachine slotMachine;
    private WG_SlotPaymentSystem paymentSystem;
    private bool createNewMachine = false;

    // Step 2: Slot Pattern
    private SlotPattern newSlotPattern;
    private string patternName = "MySlotPattern";
    private int reelCount = 5;
    private int visibleRows = 3;
    private ReelAnimationType animationType = ReelAnimationType.IndependentRows;
    private bool onShakeSlot = false;

    // Step 3: Game Setup
    private WG_SlotSetup gameSetup;
    private string setupName = "MyGameSetup";

    // Styles
    private GUIStyle headerStyle;
    private GUIStyle stepStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;

    [MenuItem("Tools/Slot Machine/Setup Wizard")]
    public static void ShowWindow()
    {
        WG_SlotMachineSetupWizard window = GetWindow<WG_SlotMachineSetupWizard>("Slot Machine Setup");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }

    private void OnEnable()
    {
        FindExistingComponents();
    }

    private void OnGUI()
    {
        InitializeStyles();

        // CORREÇÃO: Usar um único BeginVertical principal
        EditorGUILayout.BeginVertical();
        // Header
        DrawHeader();

        // Progress Steps
        DrawProgressSteps();

        // Content - CORREÇÃO: Garantir que BeginScrollView é sempre pareado com EndScrollView
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        DrawCurrentStep();
        EditorGUILayout.EndScrollView();

        // Navigation
        DrawNavigation();
        EditorGUILayout.EndVertical();
    }


    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.1f, 0.3f, 0.6f) }
        };

        stepStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 30,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        boxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(15, 15, 15, 15),
            margin = new RectOffset(5, 5, 5, 5)
        };
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎰 WG Slot Machine Setup Wizard", headerStyle);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Complete o setup passo a passo para configurar sua máquina caça-níqueis", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);
    }

    private void DrawProgressSteps()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        string[] steps = {
            "1. Bem-vindo",
            "2. Máquina na Cena",
            "3. Padrão de Slots",
            "4. Configuração do Jogo",
            "5. Completo!"
        };

        for (int i = 0; i < steps.Length; i++)
        {
            var style = new GUIStyle(EditorStyles.label);
            string symbol;

            if (i < (int)currentStep)
            {
                symbol = "✓";
                style.normal.textColor = Color.green;
            }
            else if (i == (int)currentStep)
            {
                symbol = "▶";
                style.normal.textColor = Color.blue;
                style.fontStyle = FontStyle.Bold;
            }
            else
            {
                symbol = "○";
                style.normal.textColor = Color.gray;
            }

            EditorGUILayout.LabelField($"{symbol} {steps[i]}", style);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawCurrentStep()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        switch (currentStep)
        {
            case SetupStep.Welcome:
                DrawWelcomeStep();
                break;
            case SetupStep.FindOrCreateMachine:
                DrawMachineStep();
                break;
            case SetupStep.CreateSlotPattern:
                DrawPatternStep();
                break;
            case SetupStep.CreateGameSetup:
                DrawSetupStep();
                break;
            case SetupStep.Complete:
                DrawCompleteStep();
                break;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawNavigation()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = currentStep > SetupStep.Welcome;
        if (GUILayout.Button("← Anterior", buttonStyle, GUILayout.Width(100)))
        {
            currentStep--;
        }

        GUILayout.FlexibleSpace();

        // CORREÇÃO: No passo Complete, não mostrar botão "Próximo"
        if (currentStep < SetupStep.Complete)
        {
            GUI.enabled = IsStepValid(currentStep);
            string nextButtonText = "Próximo →";
            if (GUILayout.Button(nextButtonText, buttonStyle, GUILayout.Width(100)))
            {
                currentStep++;

                // CORREÇÃO: Se estamos indo para o passo Complete, executar o setup final
                if (currentStep == SetupStep.Complete)
                {
                    CompleteSetup();
                }
            }
        }
        else
        {
            GUI.enabled = true;
            if (GUILayout.Button("Reiniciar Wizard", buttonStyle, GUILayout.Width(120)))
            {
                ResetWizard();
            }
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
    }

    private bool IsStepValid(SetupStep step)
    {
        switch (step)
        {
            case SetupStep.FindOrCreateMachine:
                return slotMachine != null && paymentSystem != null;
            case SetupStep.CreateSlotPattern:
                return newSlotPattern != null;
            case SetupStep.CreateGameSetup:
                return gameSetup != null;
            case SetupStep.Complete:
                return true;
            default:
                return true;
        }
    }
    private void DrawWelcomeStep()
    {
        EditorGUILayout.LabelField("Bem-vindo ao Setup do Slot Machine!", stepStyle);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Este assistente irá guiá-lo através da configuração completa da sua máquina caça-níqueis.\n\n" +
            "Vamos configurar:\n" +
            "• A máquina principal na cena\n" +
            "• O padrão de slots visual\n" +
            "• A configuração do jogo\n" +
            "• A interface do usuário\n\n" +
            "Clique em 'Próximo' para começar!",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Configuração Rápida (Recomendado)", buttonStyle))
        {
            // Evita quebrar o layout GUI ao modificar a cena ou criar assets
            EditorApplication.delayCall += RunQuickSetup;
        }
    }
    private void DrawMachineStep()
    {
        EditorGUILayout.LabelField("Configurar Máquina na Cena", stepStyle);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Precisamos encontrar ou criar a máquina caça-níqueis na cena atual.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Encontrar componentes existentes
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Componentes Encontrados:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Slot Machine:", GUILayout.Width(100));
        EditorGUILayout.LabelField(slotMachine != null ? "✓ Encontrado" : "✗ Não encontrado");
        if (slotMachine != null && GUILayout.Button("Selecionar", GUILayout.Width(80)))
        {
            Selection.activeGameObject = slotMachine.gameObject;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Payment System:", GUILayout.Width(100));
        EditorGUILayout.LabelField(paymentSystem != null ? "✓ Encontrado" : "✗ Não encontrado");
        if (paymentSystem != null && GUILayout.Button("Selecionar", GUILayout.Width(80)))
        {
            Selection.activeGameObject = paymentSystem.gameObject;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Opções de criação
        EditorGUILayout.LabelField("Opções:", EditorStyles.boldLabel);

        createNewMachine = EditorGUILayout.Toggle("Criar nova máquina", createNewMachine);

        if (createNewMachine)
        {
            EditorGUILayout.HelpBox("Uma nova máquina será criada na cena.", MessageType.Warning);

            if (GUILayout.Button("Criar Nova Máquina", buttonStyle))
            {
                CreateNewSlotMachine();
            }
        }
        else
        {
            if (GUILayout.Button("Procurar Novamente", buttonStyle))
            {
                FindExistingComponents();
            }
        }

        EditorGUILayout.Space(10);

        if (slotMachine == null || paymentSystem == null)
        {
            EditorGUILayout.HelpBox("É necessário ter ambos os componentes na cena para continuar.", MessageType.Warning);
        }
    }

    private void DrawPatternStep()
    {
        EditorGUILayout.LabelField("Criar Padrão de Slots", stepStyle);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Agora vamos criar um padrão visual para os slots. Isso define como os reels e linhas serão organizados.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // CORREÇÃO: Botão para abrir o SlotPatternWizard
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Criar/Editar Padrão:", EditorStyles.boldLabel);

        if (GUILayout.Button("Abrir Editor de Padrão de Slots", buttonStyle))
        {
            OpenSlotPatternWizard();
        }

        EditorGUILayout.HelpBox(
            "Clique no botão acima para abrir o editor visual de padrões de slots. " +
            "Após criar ou editar um padrão, volte aqui para continuar.",
            MessageType.Info
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Pattern criado
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Padrão Atual:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Pattern:", GUILayout.Width(80));
        EditorGUILayout.LabelField(newSlotPattern != null ? newSlotPattern.patternName : "Nenhum criado");
        if (newSlotPattern != null && GUILayout.Button("Selecionar", GUILayout.Width(80)))
        {
            Selection.activeObject = newSlotPattern;
        }
        EditorGUILayout.EndHorizontal();

        if (newSlotPattern != null)
        {
            EditorGUILayout.LabelField($"Fileiras: {newSlotPattern.reelCount}, Linhas Visíveis: {newSlotPattern.visibleRows}");
            EditorGUILayout.LabelField($"Animação: {newSlotPattern.animationType}");

            // Botão para editar o pattern existente
            if (GUILayout.Button("Editar Este Padrão", buttonStyle))
            {
                SlotPatternWizard.ShowWindowForPattern(newSlotPattern);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (newSlotPattern == null)
        {
            EditorGUILayout.HelpBox("É necessário criar um padrão de slots para continuar.", MessageType.Warning);
        }
    }


    private void DrawSetupStep()
    {
        EditorGUILayout.LabelField("Configuração do Jogo", stepStyle);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Agora vamos criar a configuração principal do jogo que conecta tudo. " +
            "Você também pode configurar os símbolos aqui.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // CORREÇÃO: Criar Game Setup
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Configuração do Jogo:", EditorStyles.boldLabel);

        setupName = EditorGUILayout.TextField("Nome da Configuração", setupName);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Criar Game Setup", buttonStyle))
        {
            CreateGameSetup();
        }

        if (gameSetup != null && GUILayout.Button("Abrir Painel de Símbolos", buttonStyle))
        {
            OpenSymbolsPanel();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Crie o Game Setup primeiro. Depois você pode abrir o painel de símbolos para configurar os símbolos do jogo.",
            MessageType.Info
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Setup criado e conexões
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Configurações Atuais:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Game Setup:", GUILayout.Width(100));
        EditorGUILayout.LabelField(gameSetup != null ? "✓ Criado" : "✗ Não criado");
        if (gameSetup != null && GUILayout.Button("Selecionar", GUILayout.Width(80)))
        {
            Selection.activeObject = gameSetup;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Slot Pattern:", GUILayout.Width(100));
        EditorGUILayout.LabelField(newSlotPattern != null ? "✓ Conectado" : "✗ Não conectado");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Slot Machine:", GUILayout.Width(100));
        EditorGUILayout.LabelField(slotMachine != null ? "✓ Conectado" : "✗ Não conectado");
        EditorGUILayout.EndHorizontal();

        // Mostrar símbolos configurados
        if (gameSetup != null && gameSetup.m_symbols != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Símbolos:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{gameSetup.m_symbols.Count} configurados");
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // CORREÇÃO: Conectar à máquina
        if (gameSetup != null && slotMachine != null)
        {
            if (slotMachine.gameSetup != gameSetup)
            {
                if (GUILayout.Button("Conectar à Máquina", buttonStyle))
                {
                    ConnectToMachine();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Game Setup já está conectado à Slot Machine.", MessageType.Info);
            }
        }

        // CORREÇÃO: Conectar pattern ao setup
        if (gameSetup != null && newSlotPattern != null && gameSetup.slotPattern != newSlotPattern)
        {
            if (GUILayout.Button("Conectar Pattern ao Setup", buttonStyle))
            {
                ConnectPatternToSetup();
            }
        }

        EditorGUILayout.Space(10);

        if (gameSetup == null)
        {
            EditorGUILayout.HelpBox("É necessário criar o Game Setup para continuar.", MessageType.Warning);
        }
    }

    private void DrawCompleteStep()
    {
        EditorGUILayout.LabelField("✅ Setup Completo! 🎉", stepStyle);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Parabéns! Sua máquina caça-níqueis está configurada e pronta para uso.\n\n" +
            "Aqui está um resumo do que foi criado:",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Resumo
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📋 Resumo da Configuração", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawSummaryItem("🎰 Slot Machine", slotMachine != null ? slotMachine.gameObject.name : "❌ Não encontrado");
        if (slotMachine != null)
            DrawSubInfo($"Line Rows: {slotMachine.lineRows?.Count ?? 0}");

        DrawSummaryItem("💰 Payment System", paymentSystem != null ? paymentSystem.gameObject.name : "❌ Não encontrado");

        DrawSummaryItem("🧩 Slot Pattern", newSlotPattern != null ? newSlotPattern.patternName : "❌ Nenhum criado");
        if (newSlotPattern != null)
            DrawSubInfo($"Fileiras: {newSlotPattern.reelCount} | Linhas Visíveis: {newSlotPattern.visibleRows}");

        DrawSummaryItem("⚙️ Game Setup", gameSetup != null ? gameSetup.name : "❌ Não criado");
        if (gameSetup != null)
            DrawSubInfo($"Símbolos configurados: {gameSetup.m_symbols?.Count ?? 0}");

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Botão para regenerar UI
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🖥️ Geração de UI", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        if (GUILayout.Button(new GUIContent("  Regenerar UI (Canvas e LineSlots)", EditorGUIUtility.IconContent("d_Refresh").image), buttonStyle))
        {
            GenerateUI();
        }

        if (GUILayout.Button(new GUIContent("  Limpar e Regenerar UI", EditorGUIUtility.IconContent("TreeEditor.Trash").image), buttonStyle))
        {
            ClearAndRegenerateUI();
        }

        EditorGUILayout.HelpBox(
            "Se a UI não foi gerada automaticamente ou precisa ser atualizada, " +
            "clique nos botões acima para regenerar o Canvas e LineSlots.",
            MessageType.Info
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Próximos passos:\n" +
            "1. Configure os símbolos no Game Setup (se ainda não fez)\n" +
            "2. Verifique se as Line Rows estão corretas na Slot Machine\n" +
            "3. Configure os pagamentos no Payment System\n" +
            "4. Teste seu jogo!",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("⚡ Ações Rápidas", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("  Slot Machine", EditorGUIUtility.IconContent("Prefab Icon").image), buttonStyle))
        {
            if (slotMachine != null)
            {
                Selection.activeGameObject = slotMachine.gameObject;
            }
        }

        if (GUILayout.Button(new GUIContent("  Game Setup", EditorGUIUtility.IconContent("ScriptableObject Icon").image), buttonStyle))
        {
            if (gameSetup != null)
            {
                Selection.activeObject = gameSetup;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("  Símbolos", EditorGUIUtility.IconContent("d_Favorite Icon").image), buttonStyle))
        {
            OpenSymbolsPanel();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("  Canvas", EditorGUIUtility.IconContent("CanvasRenderer Icon").image), buttonStyle))
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Selection.activeGameObject = canvas.gameObject;
            }
        }
        if (newSlotPattern != null)
        {
            //EditorGUILayout.LabelField($"Fileiras: {newSlotPattern.reelCount}, Linhas Visíveis: {newSlotPattern.visibleRows}");
            //EditorGUILayout.LabelField($"Animação: {newSlotPattern.animationType}");

            // Botão para editar o pattern existente
            if (GUILayout.Button(new GUIContent("  Editar Padrão", EditorGUIUtility.IconContent("d_EditCollider").image), buttonStyle))
            {
                SlotPatternWizard.ShowWindowForPattern(newSlotPattern);
            }
        }

        if (GUILayout.Button(new GUIContent("  Novo Setup", EditorGUIUtility.IconContent("CreateAddNew").image), buttonStyle))
        {
            ResetWizard();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSummaryItem(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140));
        GUILayout.Label(value, EditorStyles.label);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSubInfo(string text)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.Label("↳ " + text, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }


    private void OpenSlotPatternWizard()
    {
        SlotPatternWizard.CreateWizard();
    }

    private void OpenSymbolsPanel()
    {
        // Criar uma janela para gerenciar símbolos
        SymbolsManagerWindow.ShowWindow(gameSetup);
    }

    private void ConnectPatternToSetup()
    {
        if (gameSetup != null && newSlotPattern != null)
        {
            gameSetup.slotPattern = newSlotPattern;
            EditorUtility.SetDirty(gameSetup);
            AssetDatabase.SaveAssets();
            Debug.Log("Slot Pattern conectado ao Game Setup: " + newSlotPattern.patternName);
        }
    }

    private void ClearAndRegenerateUI()
    {
        // Encontrar e destruir Canvas existente
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null && existingCanvas.name == "Canvas")
        {
            DestroyImmediate(existingCanvas.gameObject);
        }

        // Gerar nova UI
        GenerateUI();
    }

    private void ResetWizard()
    {
        slotMachine = null;
        paymentSystem = null;
        newSlotPattern = null;
        gameSetup = null;
        currentStep = SetupStep.Welcome;

        FindExistingComponents(); // Recarregar componentes existentes
    }

    // Métodos de implementação
    private bool FindPrefabs()
    {
        // Procurar o Canvas.prefab
        string[] canvasGuids = AssetDatabase.FindAssets("Canvas t:Prefab", new[] { "Assets/_src/SlotMachine/Prefabs/UI" });
        if (canvasGuids.Length > 0)
        {
            string canvasPath = AssetDatabase.GUIDToAssetPath(canvasGuids[0]);
            canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPath);
        }

        // Procurar o LineSlots.prefab
        string[] lineSlotsGuids = AssetDatabase.FindAssets("LineSlots t:Prefab", new[] { "Assets/_src/SlotMachine/Prefabs/UI" });
        if (lineSlotsGuids.Length > 0)
        {
            string lineSlotsPath = AssetDatabase.GUIDToAssetPath(lineSlotsGuids[0]);
            lineSlotsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lineSlotsPath);
        }

        // Procurar o InputManager.prefab
        string[] inputManagersGuids = AssetDatabase.FindAssets("InputManager t:Prefab", new[] { "Assets/_src/SlotMachine/Prefabs" });
        if (inputManagersGuids.Length > 0)
        {
            string inputManagersPath = AssetDatabase.GUIDToAssetPath(inputManagersGuids[0]);
            inputManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(inputManagersPath);
        }

        // Procurar o AudioManager.prefab
        string[] audioManagerGuids = AssetDatabase.FindAssets("AudioManager t:Prefab", new[] { "Assets/_src/SlotMachine/Prefabs" });
        if (audioManagerGuids.Length > 0)
        {
            string audioManagerPath = AssetDatabase.GUIDToAssetPath(audioManagerGuids[0]);
            audioManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(audioManagerPath);
        }

        return canvasPrefab != null && lineSlotsPrefab != null &&  inputManagerPrefab != null && audioManagerPrefab != null;
    }
    private GameObject InstantiateCanvas()
    {
        if (canvasPrefab == null)
        {
            Debug.LogError("Canvas prefab não encontrado!");
            return null;
        }

        // Verificar se já existe um Canvas na cena
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            Debug.Log("Canvas já existe na cena: " + existingCanvas.gameObject.name);
            return existingCanvas.gameObject;
        }

        // Instanciar novo Canvas
        GameObject canvasInstance = PrefabUtility.InstantiatePrefab(canvasPrefab) as GameObject;
        canvasInstance.name = "Canvas";

        Undo.RegisterCreatedObjectUndo(canvasInstance, "Instantiate Canvas");
        Debug.Log("Canvas instanciado: " + canvasInstance.name);

        return canvasInstance;
    }

    private GameObject InstantiateInputManager()
    {
        if (inputManagerPrefab == null)
        {
            Debug.LogError("InputManager prefab não encontrado!");
            return null;
        }

        // Verificar se já existe um WG_InputManager na cena
        WG_InputManager existingInput = FindObjectOfType<WG_InputManager>();
        if (existingInput != null)
        {
            Debug.Log("Input já existe na cena: " + existingInput.gameObject.name);
            return existingInput.gameObject;
        }

        // Instanciar novo Canvas
        GameObject inputInstance = PrefabUtility.InstantiatePrefab(inputManagerPrefab) as GameObject;
        inputInstance.name = "inputManager";

        Undo.RegisterCreatedObjectUndo(inputInstance, "Instantiate input Manager");
        Debug.Log("Canvas instanciado: " + inputInstance.name);

        return inputInstance;
    }

    private GameObject InstantiateAudioManager()
    {
        if (audioManagerPrefab == null)
        {
            Debug.LogError("AudioManagerPrefab prefab não encontrado!");
            return null;
        }

        // Verificar se já existe um WG_AudioManager na cena
        WG_AudioManager existingAudio = FindObjectOfType<WG_AudioManager>();
        if (existingAudio != null)
        {
            Debug.Log("Audio Manager já existe na cena: " + existingAudio.gameObject.name);
            return existingAudio.gameObject;
        }

        // Instanciar novo Canvas
        GameObject audioInstance = PrefabUtility.InstantiatePrefab(audioManagerPrefab) as GameObject;
        audioInstance.name = "audioManager";

        Undo.RegisterCreatedObjectUndo(audioInstance, "Instantiate audio Manager");
        Debug.Log("Audio instanciado: " + audioInstance.name);

        return audioInstance;
    }

    private Transform FindMaskTransform(GameObject canvasInstance)
    {
        if (canvasInstance == null) return null;

        // Procurar pelo caminho: Canvas > VerticallFullHDArea > GamePanel > Board Transform > Board > Mask
        Transform verticalFullHD = canvasInstance.transform.Find("VerticallFullHDArea");
        if (verticalFullHD == null)
        {
            Debug.LogError("VerticallFullHDArea não encontrado!");
            return null;
        }

        Transform gamePanel = verticalFullHD.Find("GamePanel");
        if (gamePanel == null)
        {
            Debug.LogError("GamePanel não encontrado!");
            return null;
        }

        Transform boardTransform = gamePanel.Find("Board Transform");
        if (boardTransform == null)
        {
            Debug.LogError("Board Transform não encontrado!");
            return null;
        }

        Transform board = boardTransform.Find("Board");
        if (board == null)
        {
            Debug.LogError("Board não encontrado!");
            return null;
        }

        Transform Grid = board.Find("Grid");
        if (board == null)
        {
            Debug.LogError("Grid não encontrado!");
            return null;
        }

        Transform mask = Grid.Find("Mask");
        if (mask == null)
        {
            Debug.LogError("Mask não encontrado!");
            return null;
        }

        return mask;
    }

    private void GenerateLineSlots(Transform parentTransform, int numberOfLines)
    {
        if (lineSlotsPrefab == null || parentTransform == null)
        {
            Debug.LogError("Prefab ou parent transform não encontrado!");
            return;
        }

        // Limpar LineSlots existentes (opcional)
        foreach (Transform child in parentTransform)
        {
            if (child.name.StartsWith("LineSlots"))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        // Criar novos LineSlots baseado no número de linhas
        for (int i = 0; i < numberOfLines; i++)
        {
            GameObject lineSlotsInstance = PrefabUtility.InstantiatePrefab(lineSlotsPrefab) as GameObject;
            lineSlotsInstance.name = $"LineSlots_{i}";
            lineSlotsInstance.transform.SetParent(parentTransform, false);

            // Configurar posição se necessário
            lineSlotsInstance.transform.localPosition = Vector3.zero;
            lineSlotsInstance.transform.localScale = Vector3.one;

            Undo.RegisterCreatedObjectUndo(lineSlotsInstance, "Instantiate LineSlots");

            Debug.Log($"LineSlots criado: {lineSlotsInstance.name}");
        }
    }

    private void PopulateLineRows(WG_SlotMachine machine, Transform parentTransform)
    {
        if (machine == null || parentTransform == null) return;

        machine.lineRows.Clear();

        // Encontrar todos os componentes WG_LineRows nos LineSlots
        WG_LineRows[] foundLineRows = parentTransform.GetComponentsInChildren<WG_LineRows>();

        foreach (WG_LineRows lineRow in foundLineRows)
        {
            lineRow.SetReels(machine.gameSetup.slotPattern.reelCount);
            machine.lineRows.Add(lineRow);
            Debug.Log($"LineRow adicionado: {lineRow.gameObject.name}");
        }

        // Se não encontrou nenhum, criar manualmente
        if (machine.lineRows.Count == 0)
        {
            Debug.LogWarning("Nenhum WG_LineRows encontrado nos LineSlots. Criando manualmente...");

            foreach (Transform child in parentTransform)
            {
                if (child.name.StartsWith("LineSlots"))
                {
                    WG_LineRows newLineRow = child.GetComponent<WG_LineRows>();
                    if (newLineRow == null)
                    {
                        newLineRow = child.gameObject.AddComponent<WG_LineRows>();
                    }
                    machine.lineRows.Add(newLineRow);
                    Debug.Log($"LineRow criado manualmente: {child.name}");
                }
            }
        }

        ForceUpdateWizard();

        EditorUtility.SetDirty(machine);
    }

    private void ForceUpdateWizard()
    {
        if (slotMachine.gameSetup != null)
        {
            slotMachine.gameSetup.onUpdateWizard = true;
            EditorUtility.SetDirty(slotMachine.gameSetup);

            // Refresh all line rows
            foreach (var line in slotMachine.lineRows)
            {
                if (line != null)
                {
                    slotMachine.SetWizard(line);
                }
            }

            Debug.Log("Wizard configuration updated!");
        }
    }

    private void FindExistingComponents()
    {
        slotMachine = FindObjectOfType<WG_SlotMachine>();
        paymentSystem = FindObjectOfType<WG_SlotPaymentSystem>();
        
        if (slotMachine != null && slotMachine.gameSetup != null)
        {
            gameSetup = slotMachine.gameSetup;
            if (gameSetup.slotPattern != null)
            {
                newSlotPattern = gameSetup.slotPattern;
            }
        }
    }

    private void CreateNewSlotMachine()
    {
        GameObject machineObject = new GameObject("WG_SlotMachine");
        slotMachine = machineObject.AddComponent<WG_SlotMachine>();
        paymentSystem = machineObject.AddComponent<WG_SlotPaymentSystem>();
        
        Undo.RegisterCreatedObjectUndo(machineObject, "Create Slot Machine");
        Selection.activeGameObject = machineObject;
        
        Debug.Log("Nova máquina caça-níqueis criada: " + machineObject.name);
    }

    private void CreateSlotPattern()
    {
        SlotPatternWizard.CreateWizard();
        // O pattern será atribuído quando o usuário criar através do wizard
    }

    private void CreateGameSetup()
    {
        gameSetup = ScriptableObject.CreateInstance<WG_SlotSetup>();
        
        string path = EditorUtility.SaveFilePanelInProject(
            "Salvar Game Setup",
            setupName + ".asset",
            "asset",
            "Salvar configuração do jogo");
        
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(gameSetup, path);
            AssetDatabase.SaveAssets();
            
            // Conectar o pattern se existir
            if (newSlotPattern != null)
            {
                gameSetup.slotPattern = newSlotPattern;
                EditorUtility.SetDirty(gameSetup);
                AssetDatabase.SaveAssets();
            }
            
            Selection.activeObject = gameSetup;
            Debug.Log("Game Setup criado: " + path);
        }
    }

    private void ConnectToMachine()
    {
        if (slotMachine != null && gameSetup != null)
        {
            slotMachine.gameSetup = gameSetup;
            EditorUtility.SetDirty(slotMachine);
            
            if (newSlotPattern != null)
            {
                gameSetup.slotPattern = newSlotPattern;
                EditorUtility.SetDirty(gameSetup);
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log("Game Setup conectado à Slot Machine");
        }
    }

    private void CompleteSetup()
    {
        // Garantir que tudo está conectado
        if (slotMachine != null && gameSetup != null)
        {
            slotMachine.gameSetup = gameSetup;
            EditorUtility.SetDirty(slotMachine);
        }

        if (gameSetup != null && newSlotPattern != null)
        {
            gameSetup.slotPattern = newSlotPattern;
            EditorUtility.SetDirty(gameSetup);
        }

        // GERAR UI AUTOMATICAMENTE
        if (!IsUIGenerated())
        {
            GenerateUI();
        }

        AssetDatabase.SaveAssets();

        Debug.Log("Setup da Slot Machine completado com sucesso!");
        EditorUtility.DisplayDialog("Setup Completo",
            "Sua máquina caça-níqueis foi configurada com sucesso!\n\n" +
            "A UI foi gerada automaticamente. Verifique se tudo está correto " +
            "e use os botões no passo final para ajustar qualquer configuração.",
            "OK");
    }

    private bool IsUIGenerated()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return false;

        Transform mask = FindMaskTransform(canvas.gameObject);
        if (mask == null) return false;

        return mask.childCount > 0;
    }

    private void GenerateUI()
    {
        // Encontrar prefabs
        if (!FindPrefabs())
        {
            Debug.LogError("Não foi possível encontrar todos os prefabs necessários!");
            return;
        }

        // Instanciar Canvas
        GameObject canvasInstance = InstantiateCanvas();
        if (canvasInstance == null) return;

        GameObject inputInstance = InstantiateInputManager();
        if (inputInstance == null) return;

        GameObject audioInstance = InstantiateAudioManager();
        if (audioInstance == null) return;

        // Encontrar o Mask transform
        maskTransform = FindMaskTransform(canvasInstance);
        if (maskTransform == null) return;

        // Gerar LineSlots baseado no número de linhas do pattern
        if (newSlotPattern != null)
        {
            int numberOfLines = newSlotPattern.visibleRows;
            GenerateLineSlots(maskTransform, numberOfLines);

            // Preencher lineRows na SlotMachine
            if (slotMachine != null)
            {
                PopulateLineRows(slotMachine, maskTransform);
            }
        }
    }

    private void RunQuickSetup()
    {
        // Setup automático rápido
        if (slotMachine == null)
        {
            CreateNewSlotMachine();
        }

        currentStep = SetupStep.CreateSlotPattern;

        // Criar pattern padrão
        CreateDefaultPattern();

        // Criar game setup
        CreateDefaultGameSetup();

        // Conectar tudo
        ConnectToMachine();

        // GERAR UI
        GenerateUI();

        currentStep = SetupStep.Complete;

        Debug.Log("Setup rápido completado!");
    }

    private void CreateDefaultPattern()
    {
        newSlotPattern = ScriptableObject.CreateInstance<SlotPattern>();
        newSlotPattern.patternName = "DefaultPattern";
        newSlotPattern.reelCount = 5;
        newSlotPattern.visibleRows = 3;
        newSlotPattern.animationType = ReelAnimationType.IndependentRows;
        
        // Criar grid padrão (todos ativos)
        bool[,] grid = new bool[5, 12];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 12; j++)
                grid[i, j] = true;
                
        newSlotPattern.GenerateReelsInfosFromGrid(grid);
        
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/DefaultSlotPattern.asset");
        AssetDatabase.CreateAsset(newSlotPattern, path);
        AssetDatabase.SaveAssets();
    }

    private void CreateDefaultGameSetup()
    {
        gameSetup = ScriptableObject.CreateInstance<WG_SlotSetup>();
        gameSetup.slotPattern = newSlotPattern;
        
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/DefaultGameSetup.asset");
        AssetDatabase.CreateAsset(gameSetup, path);
        AssetDatabase.SaveAssets();
    }

    // Atualizar quando um pattern é criado pelo wizard
    public static void OnPatternCreated(SlotPattern pattern)
    {
        WG_SlotMachineSetupWizard window = GetWindow<WG_SlotMachineSetupWizard>();
        if (window != null)
        {
            window.newSlotPattern = pattern;
            window.Repaint();
        }
    }
}

public class SymbolsManagerWindow : EditorWindow
{
    private WG_SlotSetup gameSetup;
    private Vector2 scrollPosition;

    public static void ShowWindow(WG_SlotSetup setup)
    {
        SymbolsManagerWindow window = GetWindow<SymbolsManagerWindow>("Gerenciador de Símbolos");
        window.gameSetup = setup;
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        if (gameSetup == null)
        {
            EditorGUILayout.HelpBox("Nenhum Game Setup selecionado.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gerenciador de Símbolos", EditorStyles.largeLabel);
        EditorGUILayout.LabelField($"Setup: {gameSetup.name}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Lista de símbolos existentes
        EditorGUILayout.LabelField("Símbolos Configurados:", EditorStyles.boldLabel);

        if (gameSetup.m_symbols == null || gameSetup.m_symbols.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhum símbolo configurado.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < gameSetup.m_symbols.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                gameSetup.m_symbols[i] = (WG_SymbolSO)EditorGUILayout.ObjectField(
                    gameSetup.m_symbols[i], typeof(WG_SymbolSO), false);

                if (GUILayout.Button("Editar", GUILayout.Width(60)))
                {
                    Selection.activeObject = gameSetup.m_symbols[i];
                }

                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    gameSetup.m_symbols.RemoveAt(i);
                    i--;
                    EditorUtility.SetDirty(gameSetup);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // Adicionar novo símbolo
        EditorGUILayout.LabelField("Adicionar Símbolo:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Criar Novo Símbolo"))
        {
            CreateNewSymbol();
        }

        if (GUILayout.Button("Adicionar Símbolo Existente"))
        {
            AddExistingSymbol();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Dica: Crie símbolos para representar cada tipo de ícone que aparecerá nos reels. " +
            "Cada símbolo pode ter sprites normais, de movimento e animações.",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();

        // Aplicar mudanças
        if (GUI.changed)
        {
            EditorUtility.SetDirty(gameSetup);
        }
    }

    private void CreateNewSymbol()
    {
        WG_SymbolSO newSymbol = ScriptableObject.CreateInstance<WG_SymbolSO>();

        string path = EditorUtility.SaveFilePanelInProject(
            "Criar Novo Símbolo",
            "NewSymbol.asset",
            "asset",
            "Salvar novo símbolo");

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newSymbol, path);
            AssetDatabase.SaveAssets();

            if (gameSetup.m_symbols == null)
                gameSetup.m_symbols = new List<WG_SymbolSO>();

            gameSetup.m_symbols.Add(newSymbol);
            EditorUtility.SetDirty(gameSetup);

            Selection.activeObject = newSymbol;
        }
    }

    private void AddExistingSymbol()
    {
        string path = EditorUtility.OpenFilePanel("Selecionar Símbolo", "Assets", "asset");
        if (!string.IsNullOrEmpty(path))
        {
            // Converter para path relativo
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            WG_SymbolSO symbol = AssetDatabase.LoadAssetAtPath<WG_SymbolSO>(path);
            if (symbol != null)
            {
                if (gameSetup.m_symbols == null)
                    gameSetup.m_symbols = new List<WG_SymbolSO>();

                if (!gameSetup.m_symbols.Contains(symbol))
                {
                    gameSetup.m_symbols.Add(symbol);
                    EditorUtility.SetDirty(gameSetup);
                }
            }
        }
    }
}
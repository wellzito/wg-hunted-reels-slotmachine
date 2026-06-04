using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using WG_Casino.SlotMachine;
using UnityEngine.Events;

[CustomEditor(typeof(WG_SlotMachine))]
public class WG_SlotMachineEditor : Editor
{
    private WG_SlotMachine slotMachine;
    private SerializedProperty gameSetup;
    private SerializedProperty animationType;
    private SerializedProperty lineRows;
    private SerializedProperty useAnimationSlots;
    private SerializedProperty timeStopMax;
    private SerializedProperty timeStopMaxTurbo;
    private SerializedProperty onSpeedMode;
    private SerializedProperty onReelsStartedCallbacks;
    private SerializedProperty onReelsCompletedCallbacks;
    private SerializedProperty isCallbackInProgress;
    private SerializedProperty callbackCooldown;

    private bool showGameSettings = false;
    private bool showAnimationSettings = false;
    private bool showReelSettings = false;
    private bool showDebugInfo = true;
    private bool showPatternPreview = false;
    private bool showCallbacks = false;

    private Texture2D headerTexture;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle warningStyle;
    private GUIStyle successStyle;

    private void OnEnable()
    {
        slotMachine = (WG_SlotMachine)target;
        gameSetup = serializedObject.FindProperty("gameSetup");
        animationType = serializedObject.FindProperty("m_animationType");
        lineRows = serializedObject.FindProperty("lineRows");
        useAnimationSlots = serializedObject.FindProperty("m_useAnimationSlots");
        timeStopMax = serializedObject.FindProperty("timeStopMax");
        timeStopMaxTurbo = serializedObject.FindProperty("timeStopMaxTurbo");
        onSpeedMode = serializedObject.FindProperty("onSpeedMode");
        onReelsStartedCallbacks = serializedObject.FindProperty("m_onReelsStartedCallbacks");
        onReelsCompletedCallbacks = serializedObject.FindProperty("m_onReelsCompletedCallbacks");
        isCallbackInProgress = serializedObject.FindProperty("m_isCallbackInProgress");
        callbackCooldown = serializedObject.FindProperty("m_callbackCooldown");
    }

    private void CreateStyles()
    {
        // Header style
        headerStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        // Box style - usar estilo padrão do Editor
        boxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        };

        // Warning style
        warningStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = Color.yellow },
            fontStyle = FontStyle.Bold
        };

        // Success style
        successStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = Color.green },
            fontStyle = FontStyle.Bold
        };

        // Create header texture
        headerTexture = new Texture2D(1, 1);
        headerTexture.SetPixel(0, 0, new Color(0.1f, 0.3f, 0.5f, 1f));
        headerTexture.Apply();
    }

    public override void OnInspectorGUI()
    {
        // Criar estilos apenas durante OnGUI
        CreateStyles();

        serializedObject.Update();

        DrawHeader();
        DrawGameSettings();
        DrawAnimationSettings();
        DrawCallbackSettings();
        DrawReelSettings();
        DrawPatternPreview();
        DrawDebugInfo();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        // Header background
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        var headerRect = GUILayoutUtility.GetRect(0, 30);
        EditorGUI.DrawRect(headerRect, new Color(0.1f, 0.3f, 0.5f, 1f));

        // Title
        EditorGUI.LabelField(headerRect, "🎰 WG Slot Machine", headerStyle);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);
    }

    private void DrawGameSettings()
    {
        showGameSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGameSettings, "⚙️ Game Settings");
        if (showGameSettings)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(gameSetup, new GUIContent("Game Setup", "ScriptableObject with game configuration"));

            if (slotMachine.gameSetup == null)
            {
                EditorGUILayout.HelpBox("Game Setup is required!", MessageType.Error);
                if (GUILayout.Button("Create New Game Setup"))
                {
                    CreateNewGameSetup();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Setup Status:", GUILayout.Width(80));
                EditorGUILayout.LabelField("✓ Configured", successStyle);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Edit Game Setup"))
                {
                    Selection.activeObject = slotMachine.gameSetup;
                }
            }

            EditorGUILayout.Space();

            // VERSÃO ALTERNATIVA: Desenhar a lista manualmente
            EditorGUILayout.LabelField("Line Rows Configuration", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Lines:", GUILayout.Width(80));
            EditorGUILayout.LabelField(slotMachine.lineRows != null ? slotMachine.lineRows.Count.ToString() : "0");
            EditorGUILayout.EndHorizontal();

            // Desenhar a lista manualmente
            if (slotMachine.lineRows != null)
            {
                for (int i = 0; i < slotMachine.lineRows.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    // Mostrar índice
                    EditorGUILayout.LabelField($"Line {i}:", GUILayout.Width(50));

                    // Mostrar o objeto atual
                    slotMachine.lineRows[i] = (WG_LineRows)EditorGUILayout.ObjectField(
                        slotMachine.lineRows[i],
                        typeof(WG_LineRows),
                        true,
                        GUILayout.ExpandWidth(true));

                    // Botão para remover
                    if (GUILayout.Button("−", GUILayout.Width(25)))
                    {
                        slotMachine.lineRows.RemoveAt(i);
                        i--; // Ajustar índice após remoção
                        EditorUtility.SetDirty(slotMachine);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            // Botão para adicionar novo item
            if (GUILayout.Button("+ Add New Line Row"))
            {
                if (slotMachine.lineRows == null)
                    slotMachine.lineRows = new List<WG_LineRows>();

                slotMachine.lineRows.Add(null);
                EditorUtility.SetDirty(slotMachine);
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawAnimationSettings()
    {
        showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "🎬 Animation Settings");
        if (showAnimationSettings)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(animationType, new GUIContent("Animation Type"));
            EditorGUILayout.PropertyField(useAnimationSlots, new GUIContent("Use Animation Slots"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Timing Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(timeStopMax, new GUIContent("Normal Stop Time"));
            EditorGUILayout.PropertyField(timeStopMaxTurbo, new GUIContent("Turbo Stop Time"));
            EditorGUILayout.PropertyField(onSpeedMode, new GUIContent("Speed Mode"));

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawReelSettings()
    {
        showReelSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showReelSettings, "🌀 Reel Configuration");
        if (showReelSettings)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (slotMachine.lineRows != null && slotMachine.lineRows.Count > 0)
            {
                EditorGUILayout.LabelField($"Total Lines: {slotMachine.lineRows.Count}", EditorStyles.boldLabel);

                for (int i = 0; i < slotMachine.lineRows.Count; i++)
                {
                    var line = slotMachine.lineRows[i];
                    if (line != null && line.m_reels != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Line {i}:", GUILayout.Width(50));
                        EditorGUILayout.LabelField($"{line.m_reels.Count} reels", GUILayout.Width(80));

                        if (line.m_reels.Count > 0 && line.m_reels[0] != null && line.m_reels[0].m_icons != null)
                        {
                            EditorGUILayout.LabelField($"{line.m_reels[0].m_icons.Length} icons/reel");
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No line rows configured!", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawPatternPreview()
    {
        if (slotMachine.gameSetup == null || slotMachine.gameSetup.slotPattern == null)
            return;

        var pattern = slotMachine.gameSetup.slotPattern;

        showPatternPreview = EditorGUILayout.BeginFoldoutHeaderGroup(showPatternPreview, "📊 Pattern Preview");
        if (showPatternPreview)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // Pattern info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pattern:", GUILayout.Width(60));
            EditorGUILayout.LabelField(pattern.patternName, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Reels:", GUILayout.Width(60));
            EditorGUILayout.LabelField(pattern.reelCount.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Visible:", GUILayout.Width(60));
            EditorGUILayout.LabelField($"{pattern.visibleRows} rows");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Animation:", GUILayout.Width(60));
            EditorGUILayout.LabelField(pattern.animationType.ToString());
            EditorGUILayout.EndHorizontal();

            // Grid preview
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Slot Grid Preview:", EditorStyles.boldLabel);

            DrawSlotGrid(pattern);

            // Quick actions
            EditorGUILayout.Space();
            if (GUILayout.Button("Edit Pattern"))
            {
                Selection.activeObject = pattern;
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawSlotGrid(SlotPattern pattern)
    {
        var grid = pattern.GetSlotGrid();
        int visibleStart = pattern.GetVisibleStartRow();
        int visibleEnd = pattern.GetVisibleEndRow();

        // Grid header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(60));
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            EditorGUILayout.LabelField($"R{reel}", GUILayout.Width(20));
        }
        EditorGUILayout.EndHorizontal();

        // Grid rows
        for (int row = 0; row < pattern.totalRows; row++)
        {
            EditorGUILayout.BeginHorizontal();

            // Row label with visibility indicator
            string rowLabel = $"Row {row}";
            bool isVisibleRow = (row >= visibleStart && row <= visibleEnd);

            if (isVisibleRow)
            {
                rowLabel = $"▶ {rowLabel}";
                GUI.contentColor = Color.green;
            }
            EditorGUILayout.LabelField(rowLabel, GUILayout.Width(60));
            GUI.contentColor = Color.white;

            // Slot states
            for (int reel = 0; reel < pattern.reelCount; reel++)
            {
                bool isActive = grid[reel, row];
                string symbol = isActive ? "■" : "□";

                if (isActive)
                {
                    if (isVisibleRow)
                        GUI.contentColor = Color.green;
                    else
                        GUI.contentColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
                else
                {
                    GUI.contentColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }

                EditorGUILayout.LabelField(symbol, GUILayout.Width(20));
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        // Legend
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUI.contentColor = Color.green;
        EditorGUILayout.LabelField("■", GUILayout.Width(20));
        GUI.contentColor = Color.white;
        EditorGUILayout.LabelField("Active & Visible", GUILayout.Width(100));

        GUI.contentColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        EditorGUILayout.LabelField("■", GUILayout.Width(20));
        GUI.contentColor = Color.white;
        EditorGUILayout.LabelField("Active & Hidden", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.contentColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        EditorGUILayout.LabelField("□", GUILayout.Width(20));
        GUI.contentColor = Color.white;
        EditorGUILayout.LabelField("Inactive", GUILayout.Width(100));

        EditorGUILayout.LabelField("▶", GUILayout.Width(20));
        EditorGUILayout.LabelField("Visible Row", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        // Debug info para verificar os cálculos
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Visible Area: Rows {visibleStart} to {visibleEnd}", EditorStyles.miniLabel);
    }

    private void DrawCallbackSettings()
    {
        showCallbacks = EditorGUILayout.BeginFoldoutHeaderGroup(true, "📞 Callback System");
        if (showCallbacks)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // Status dos callbacks
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Callback Status:", GUILayout.Width(100));

            if (isCallbackInProgress != null)
            {
                if (isCallbackInProgress.boolValue)
                {
                    EditorGUILayout.LabelField("🟡 In Progress", warningStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("🟢 Ready", successStyle);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Cooldown setting
            if (callbackCooldown != null)
            {
                EditorGUILayout.PropertyField(callbackCooldown, new GUIContent("Callback Cooldown"));
            }

            // CALLBACKS DE INÍCIO
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("On Reels Started", EditorStyles.boldLabel);

            if (onReelsStartedCallbacks != null)
            {
                // Mostrar o tamanho da lista
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Count:", GUILayout.Width(60));
                EditorGUILayout.LabelField(onReelsStartedCallbacks.arraySize.ToString());
                EditorGUILayout.EndHorizontal();

                // Desenhar cada elemento da lista manualmente
                for (int i = 0; i < onReelsStartedCallbacks.arraySize; i++)
                {
                    SerializedProperty callbackEvent = onReelsStartedCallbacks.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField($"Start {i + 1}", GUILayout.Width(60));

                    // Desenhar o UnityEvent sem foldout
                    if (callbackEvent != null)
                    {
                        EditorGUILayout.PropertyField(callbackEvent, GUIContent.none);
                    }

                    // Botão para remover
                    if (GUILayout.Button("−", GUILayout.Width(25)))
                    {
                        onReelsStartedCallbacks.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(2);
                }

                // Botão para adicionar novo callback de início
                if (GUILayout.Button("+ Add Start Callback"))
                {
                    onReelsStartedCallbacks.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // CALLBACKS DE TÉRMINO
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("On Reels Completed", EditorStyles.boldLabel);

            if (onReelsCompletedCallbacks != null)
            {
                // Mostrar o tamanho da lista
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Count:", GUILayout.Width(60));
                EditorGUILayout.LabelField(onReelsCompletedCallbacks.arraySize.ToString());
                EditorGUILayout.EndHorizontal();

                // Desenhar cada elemento da lista manualmente
                for (int i = 0; i < onReelsCompletedCallbacks.arraySize; i++)
                {
                    SerializedProperty callbackEvent = onReelsCompletedCallbacks.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField($"Complete {i + 1}", GUILayout.Width(70));

                    // Desenhar o UnityEvent sem foldout
                    if (callbackEvent != null)
                    {
                        EditorGUILayout.PropertyField(callbackEvent, GUIContent.none);
                    }

                    // Botão para remover
                    if (GUILayout.Button("−", GUILayout.Width(25)))
                    {
                        onReelsCompletedCallbacks.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(2);
                }

                // Botão para adicionar novo callback de término
                if (GUILayout.Button("+ Add Complete Callback"))
                {
                    onReelsCompletedCallbacks.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Informações
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Start Callbacks: Called when reels start spinning\n" +
                "Complete Callbacks: Called when all reels finish spinning\n\n" +
                "You can also use code:\n" +
                "- OnReelsStarted event (when reels start)\n" +
                "- OnReelsCompleted event (when reels complete)",
                MessageType.Info);

            // Botões de ação
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Start Callbacks"))
            {
                var method = slotMachine.GetType().GetMethod("StartReelsStartedCallbacks",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(slotMachine, null);
                    EditorUtility.SetDirty(slotMachine);
                }
            }

            if (GUILayout.Button("Test Complete Callbacks"))
            {
                var method = slotMachine.GetType().GetMethod("StartCallbackSequence",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(slotMachine, null);
                    EditorUtility.SetDirty(slotMachine);
                }
            }

            if (GUILayout.Button("Clear All"))
            {
                slotMachine.ClearAllCallbacks();
                EditorUtility.SetDirty(slotMachine);
            }
            EditorGUILayout.EndHorizontal();

            // Exemplo de código
            if (GUILayout.Button("Show Code Example"))
            {
                string codeExample = @"// ADD IN START():
WG_SlotMachine.Instance.OnReelsStarted += OnSlotsStart;
WG_SlotMachine.Instance.OnReelsCompleted += OnSlotsComplete;

// METHODS:
void OnSlotsStart() {
    Debug.Log(""Slots started spinning!"");
    // Disable buttons, show effects, etc.
}

void OnSlotsComplete() {
    Debug.Log(""All slots completed!"");
    // Enable buttons, check wins, show results, etc.
}

// CLEANUP IN ONDESTROY():
WG_SlotMachine.Instance.OnReelsStarted -= OnSlotsStart;
WG_SlotMachine.Instance.OnReelsCompleted -= OnSlotsComplete;";

                EditorUtility.DisplayDialog("Code Implementation", codeExample, "OK");
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawDebugInfo()
    {
        showDebugInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugInfo, "🔧 Debug Information");
        if (showDebugInfo)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // Game state
            EditorGUILayout.LabelField("Game State:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Running:", GUILayout.Width(80));
            EditorGUILayout.LabelField(slotMachine.m_gameRunning ? "Yes" : "No");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ended:", GUILayout.Width(80));
            EditorGUILayout.LabelField(slotMachine.m_gameEnded ? "Yes" : "No");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("On Bet:", GUILayout.Width(80));
            EditorGUILayout.LabelField(slotMachine.onBet ? "Yes" : "No");
            EditorGUILayout.EndHorizontal();

            // Preview reels info
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Reels:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {slotMachine.previewsReels?.Count ?? 0}");

            if (slotMachine.previewsReels != null && slotMachine.previewsReels.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(slotMachine.previewsReels.Count, 5); i++) // Show first 5
                {
                    var preview = slotMachine.previewsReels[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Reel {i}:", GUILayout.Width(60));
                    EditorGUILayout.LabelField($"{preview.slots?.Count ?? 0} slots");
                    EditorGUILayout.EndHorizontal();
                }

                if (slotMachine.previewsReels.Count > 5)
                {
                    EditorGUILayout.LabelField($"... and {slotMachine.previewsReels.Count - 5} more");
                }
            }

            // Debug actions
            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Configuration"))
            {
                ValidateConfiguration();
            }

            if (GUILayout.Button("Force Update Wizard"))
            {
                ForceUpdateWizard();
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void CreateNewGameSetup()
    {
        var setup = ScriptableObject.CreateInstance<WG_SlotSetup>();
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Game Setup",
            "NewSlotGameSetup",
            "asset",
            "Create a new game setup asset");

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(setup, path);
            AssetDatabase.SaveAssets();
            slotMachine.gameSetup = setup;
            EditorUtility.SetDirty(slotMachine);
        }
    }

    private void ValidateConfiguration()
    {
        bool isValid = true;
        List<string> issues = new List<string>();

        if (slotMachine.gameSetup == null)
        {
            issues.Add("❌ Game Setup is not assigned");
            isValid = false;
        }

        if (slotMachine.lineRows == null || slotMachine.lineRows.Count == 0)
        {
            issues.Add("❌ No Line Rows configured");
            isValid = false;
        }

        if (slotMachine.gameSetup?.slotPattern == null)
        {
            issues.Add("⚠️ No Slot Pattern assigned in Game Setup");
        }

        // Show validation results
        if (isValid && issues.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation", "✓ Configuration is valid!", "OK");
        }
        else
        {
            string message = "Configuration Issues:\n\n" + string.Join("\n", issues);
            EditorUtility.DisplayDialog("Validation", message, "OK");
        }
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
}
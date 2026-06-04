using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(PaymentPattern))]
public class PaymentPatternEditor : Editor
{
    private PaymentPattern pattern;
    private Vector2 scrollPosition;
    private Vector2Int selectedSlot = new Vector2Int(-1, -1);

    private Texture2D selectedTex;
    private Texture2D unselectedTex;
    private Texture2D editingTex;

    private void OnEnable()
    {
        pattern = (PaymentPattern)target;

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

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pattern Editor", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Grid Size: {pattern.visibleRows} rows x {pattern.reelCount} reels");

        EditorGUILayout.Space();

        // Botões de ação
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear All Slots"))
        {
            pattern.selectedSlots.Clear();
            EditorUtility.SetDirty(pattern);
        }

        if (GUILayout.Button("Open in Editor Window"))
        {
            SlotPatternEditorWindow.ShowWindow();
            SlotPatternEditorWindow window = EditorWindow.GetWindow<SlotPatternEditorWindow>();
            window.SetPattern(pattern);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        DrawSlotGrid();

        if (selectedSlot.x >= 0 && selectedSlot.y >= 0)
        {
            DrawSpriteEditingSection();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(pattern);
        }
    }

    private void DrawSlotGrid()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Cabeçalho
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(50));
        for (int reel = 0; reel < pattern.reelCount; reel++)
        {
            GUIStyle reelStyle = new GUIStyle(EditorStyles.boldLabel);
            reelStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField($"Reel {reel}", reelStyle, GUILayout.Width(50));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Linhas 0 a visibleRows-1
        for (int row = 0; row < pattern.visibleRows; row++)
        {
            EditorGUILayout.BeginHorizontal();

            string rowLabel = row == 0 ? "Topo (L0)" : (row == 1 ? "Meio (L1)" : (row == 2 ? "Baixo (L2)" : $"Linha {row}"));
            EditorGUILayout.LabelField(rowLabel, GUILayout.Width(50));

            for (int reel = 0; reel < pattern.reelCount; reel++)
            {
                bool isSelected = pattern.IsSlotSelected(reel, row);
                bool isEditing = selectedSlot.x == reel && selectedSlot.y == row;

                GUIStyle style = new GUIStyle(GUI.skin.button)
                {
                    fixedWidth = 50,
                    fixedHeight = 50,
                    normal = { background = isEditing ? editingTex : (isSelected ? selectedTex : unselectedTex) }
                };

                Rect buttonRect = EditorGUILayout.GetControlRect(false, 50, style, GUILayout.Width(50), GUILayout.Height(50));

                if (GUI.Button(buttonRect, "", style))
                {
                    if (Event.current.button == 0) // Left click
                    {
                        if (isSelected)
                        {
                            pattern.RemoveSlot(reel, row);
                            if (selectedSlot.x == reel && selectedSlot.y == row)
                                selectedSlot = new Vector2Int(-1, -1);
                        }
                        else
                        {
                            pattern.AddSlot(reel, row);
                            selectedSlot = new Vector2Int(reel, row);
                        }
                    }
                    else if (Event.current.button == 1 && isSelected) // Right click
                    {
                        selectedSlot = new Vector2Int(reel, row);
                    }
                }

                // Desenhar sprite se selecionado
                if (isSelected)
                {
                    var slot = pattern.selectedSlots.Find(s => s.reelIndex == reel && s.rowIndex == row);
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
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "Left click: select/deselect slot | Right click: edit sprite\n" +
            $"Selected slots: {pattern.selectedSlots.Count}",
            MessageType.Info);
    }

    private void DrawSpriteEditingSection()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Slot Sprite Editor", EditorStyles.boldLabel);

        var slot = pattern.selectedSlots.Find(s =>
            s.reelIndex == selectedSlot.x &&
            s.rowIndex == selectedSlot.y);

        if (slot != null)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField($"Editing: Reel {selectedSlot.x}, Row {selectedSlot.y}");

            Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(
                "Slot Sprite",
                slot.slotSprite,
                typeof(Sprite),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                slot.slotSprite = newSprite;
                EditorUtility.SetDirty(pattern);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Sprite"))
            {
                slot.slotSprite = null;
                EditorUtility.SetDirty(pattern);
            }

            if (GUILayout.Button("Close Editor"))
            {
                selectedSlot = new Vector2Int(-1, -1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Selected slot not found.", MessageType.Warning);
            selectedSlot = new Vector2Int(-1, -1);
        }
    }
}
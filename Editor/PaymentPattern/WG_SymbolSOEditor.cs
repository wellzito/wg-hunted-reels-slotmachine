using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine.UI;

namespace WG_Casino.Setup
{
    [CustomEditor(typeof(WG_SymbolSO))]
    public class WG_SymbolSOEditor : Editor
    {
        private WG_SymbolSO symbol;

        // Sprite animation variables
        private bool showMaterialPreview = true;
        private bool isAnimating = false;
        private int currentAnimFrame = 0;
        private float animTimer = 0f;
        private float animSpeed = 0.1f;

        // Spine preview variables
        private bool isSpineAnimating = false;
        private string selectedSpineAnimation = "";
        private List<string> spineAnimationNames = new List<string>();

        private SkeletonGraphic spinePreviewInstance;
        private GameObject previewRootGO;
        private Canvas previewCanvas;
        private Camera previewCamera;
        private RenderTexture renderTexture;
        private Rect spinePreviewRect;
        private double lastEditorTime;

        // Default Spine Preview variables
        private SkeletonGraphic defaultSpinePreviewInstance;
        private GameObject defaultSpinePreviewRoot;
        private Canvas defaultSpinePreviewCanvas;
        private Camera defaultSpinePreviewCamera;
        private RenderTexture defaultSpinePreviewRenderTexture;
        private Rect defaultSpinePreviewRect;
        private double lastDefaultSpineTime;
        private bool isDefaultSpineAnimating = false;
        private string selectedDefaultSpineAnimation = "";
        private List<string> defaultSpineAnimationNames = new List<string>();

        private void OnEnable()
        {
            symbol = (WG_SymbolSO)target;
            EditorApplication.update += OnEditorUpdate;
            lastEditorTime = EditorApplication.timeSinceStartup;
            lastDefaultSpineTime = EditorApplication.timeSinceStartup;
            UpdateSpineAnimationList();
            UpdateDefaultSpineAnimationList();
            CreateSpinePreview();
            CreateDefaultSpinePreview();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopAnimation();
            StopSpineAnimation();
            StopDefaultSpineAnimation();
            DestroySpinePreview();
            DestroyDefaultSpinePreview();
        }

        private void OnEditorUpdate()
        {
            if (symbol == null) return;

            if (!symbol.onSpine)
            {
                // Atualização sprite
                if (isAnimating && symbol.m_reelIconsList.Count > 0)
                {
                    animTimer += Time.deltaTime;
                    if (animTimer >= animSpeed)
                    {
                        animTimer = 0f;
                        currentAnimFrame = (currentAnimFrame + 1) % symbol.m_reelIconsList.Count;
                        Repaint();
                    }
                }
            }
            else
            {
                // Atualização Spine
                if (isSpineAnimating && spinePreviewInstance != null)
                {
                    double currentTime = EditorApplication.timeSinceStartup;
                    float deltaTime = (float)(currentTime - lastEditorTime);
                    lastEditorTime = currentTime;

                    spinePreviewInstance.Update(deltaTime);
                    spinePreviewInstance.LateUpdate();
                    Repaint();
                }
            }

            // Atualização do Default Spine Preview
            if (isDefaultSpineAnimating && defaultSpinePreviewInstance != null && defaultSpinePreviewInstance.IsValid)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - lastDefaultSpineTime);
                lastDefaultSpineTime = currentTime;

                defaultSpinePreviewInstance.Update(deltaTime);
                defaultSpinePreviewInstance.LateUpdate();
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Não excluir os novos campos
            DrawPropertiesExcluding(serializedObject, "m_Script", "defaultSprite", "moveSprite", "m_reelIconsList", "materialsLine", "onSpine", "skeletonGraphic", "useSpineDefault", "defaultSpinePreview");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Symbol Type", EditorStyles.boldLabel);

            bool previousOnSpine = symbol.onSpine;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onSpine"));

            if (previousOnSpine != symbol.onSpine)
            {
                if (symbol.onSpine)
                    CreateSpinePreview();
                else
                    DestroySpinePreview();
            }

            EditorGUILayout.Space();

            // Default Spine Preview Section
            DrawDefaultSpinePreviewSection();

            // Sprite Preview - só mostra se não estiver usando spine default
            if (!symbol.useSpineDefault)
            {
                EditorGUILayout.LabelField("Sprite Preview", EditorStyles.boldLabel);
                DrawSpritePreview(symbol.defaultSprite, "Default Sprite");
            }

            showMaterialPreview = EditorGUILayout.Toggle("Show Material Preview", showMaterialPreview);
            if (showMaterialPreview && symbol.materialsLine != null)
                DrawMaterialLine(symbol.materialsLine);

            if (symbol.onSpine)
                DrawSpineSection();
            else
                DrawSpriteAnimationSection();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSprite"));
            if (!symbol.onSpine)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_reelIconsList"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("materialsLine"));

            if (symbol.onSpine)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("skeletonGraphic"));
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSpineAnimationList();
                    CreateSpinePreview();
                }
            }

            // Draw RectTransform fields
            DrawRectTransformFields();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultSpineAnimationFields()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Spine Animations", EditorStyles.boldLabel);

            // Obter lista de animações disponíveis
            List<string> animationOptions = new List<string>();
            animationOptions.Add("(None)");

            if (symbol.defaultSpinePreview != null && symbol.defaultSpinePreview.SkeletonData != null)
            {
                var animations = symbol.defaultSpinePreview.SkeletonData.Animations;
                if (animations != null)
                {
                    foreach (var anim in animations)
                    {
                        animationOptions.Add(anim.Name);
                    }
                }
            }

            // Dropdown para animação Idle
            int idleIndex = GetAnimationIndex(symbol.defaultIdleAnimation, animationOptions);
            int newIdleIndex = EditorGUILayout.Popup("Idle Animation", idleIndex, animationOptions.ToArray());
            if (newIdleIndex != idleIndex)
            {
                symbol.defaultIdleAnimation = newIdleIndex > 0 ? animationOptions[newIdleIndex] : "";
                EditorUtility.SetDirty(symbol);
            }

            // Dropdown para animação Spin
            int spinIndex = GetAnimationIndex(symbol.defaultSpinAnimation, animationOptions);
            int newSpinIndex = EditorGUILayout.Popup("Spin Animation", spinIndex, animationOptions.ToArray());
            if (newSpinIndex != spinIndex)
            {
                symbol.defaultSpinAnimation = newSpinIndex > 0 ? animationOptions[newSpinIndex] : "";
                EditorUtility.SetDirty(symbol);
            }

            // Preview das animações selecionadas
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Idle:", GUILayout.Width(40));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(symbol.defaultIdleAnimation) ? "(none)" : symbol.defaultIdleAnimation, GUILayout.Width(150));
            EditorGUILayout.LabelField("Spin:", GUILayout.Width(40));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(symbol.defaultSpinAnimation) ? "(none)" : symbol.defaultSpinAnimation, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
        }

        private int GetAnimationIndex(string animationName, List<string> animationOptions)
        {
            if (string.IsNullOrEmpty(animationName)) return 0;
            int index = animationOptions.IndexOf(animationName);
            return index >= 0 ? index : 0;
        }

        private void DrawDefaultSpinePreviewSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Preview Settings", EditorStyles.boldLabel);

            SerializedProperty useSpineDefault = serializedObject.FindProperty("useSpineDefault");
            EditorGUILayout.PropertyField(useSpineDefault, new GUIContent("Use Spine as Default"));

            if (useSpineDefault.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultSpinePreview"),
                    new GUIContent("Default Spine Preview"));

                if (EditorGUI.EndChangeCheck())
                {
                    UpdateDefaultSpineAnimationList();
                    CreateDefaultSpinePreview();
                }

                // Preview do spine default
                if (symbol.defaultSpinePreview != null && symbol.defaultSpinePreview.skeletonDataAsset != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Default Spine Preview", EditorStyles.boldLabel);

                    DrawDefaultSpineAnimationControls();
                    DrawDefaultSpineAnimationFields(); // NOVO: campos para selecionar animações
                    DrawDefaultSkeletonGraphicPreview();
                }
                else if (useSpineDefault.boolValue)
                {
                    EditorGUILayout.HelpBox("Please assign a SkeletonGraphic for default spine preview", MessageType.Warning);
                }
            }
        }
        private void DrawRectTransformFields()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RectTransform Settings", EditorStyles.boldLabel);

            // Mostrar os três RectTransformVectors
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rectTransformVector"),
                new GUIContent("Spine Animation Rect", "Usado para animações de spine durante o jogo"), true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("rectTransformVectorIcon"),
                new GUIContent("Icon Idle Rect", "Usado para ícone em estado idle (parado)"), true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("rectTransformVectorIconSpin"),
                new GUIContent("Icon Spin Rect", "Usado para ícone em estado spin (girando)"), true);
        }
        private void DrawDefaultSpineAnimationControls()
        {
            EditorGUILayout.BeginHorizontal();
            if (defaultSpineAnimationNames.Count > 0)
            {
                int currentIndex = defaultSpineAnimationNames.IndexOf(selectedDefaultSpineAnimation);
                if (currentIndex < 0 && defaultSpineAnimationNames.Count > 0)
                    currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Animation", currentIndex, defaultSpineAnimationNames.ToArray());
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < defaultSpineAnimationNames.Count)
                {
                    selectedDefaultSpineAnimation = defaultSpineAnimationNames[newIndex];
                    if (isDefaultSpineAnimating)
                        StartDefaultSpineAnimation();
                }
            }
            else
            {
                EditorGUILayout.Popup("Animation", 0, new string[] { "No animations" });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isDefaultSpineAnimating ? "■ Stop" : "▶ Play", GUILayout.Width(60)))
            {
                if (isDefaultSpineAnimating)
                    StopDefaultSpineAnimation();
                else
                    StartDefaultSpineAnimation();
            }
            if (GUILayout.Button("Update Animations", GUILayout.Width(140)))
            {
                UpdateDefaultSpineAnimationList();
                CreateDefaultSpinePreview();
            }
            EditorGUILayout.EndHorizontal();

            if (defaultSpinePreviewInstance != null && defaultSpinePreviewInstance.IsValid)
            {
                EditorGUILayout.BeginHorizontal();
                float currentTimeScale = defaultSpinePreviewInstance.timeScale;
                float newTimeScale = EditorGUILayout.Slider("Animation Speed", currentTimeScale, 0f, 2f);
                if (newTimeScale != currentTimeScale)
                    defaultSpinePreviewInstance.timeScale = newTimeScale;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDefaultSkeletonGraphicPreview()
        {
            defaultSpinePreviewRect = GUILayoutUtility.GetRect(356, 356, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(defaultSpinePreviewRect, new Color(0.1f, 0.1f, 0.1f, 1f));

            if (defaultSpinePreviewInstance == null || !defaultSpinePreviewInstance.IsValid)
            {
                EditorGUI.LabelField(defaultSpinePreviewRect, "Spine not initialized", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            RenderDefaultSpineToTexture();
            if (defaultSpinePreviewRenderTexture != null)
                GUI.DrawTexture(defaultSpinePreviewRect, defaultSpinePreviewRenderTexture, ScaleMode.ScaleToFit);
        }

        private void RenderDefaultSpineToTexture()
        {
            if (defaultSpinePreviewInstance == null || defaultSpinePreviewCamera == null) return;

            int w = Mathf.Max(1, (int)defaultSpinePreviewRect.width);
            int h = Mathf.Max(1, (int)defaultSpinePreviewRect.height);

            if (defaultSpinePreviewRenderTexture == null || defaultSpinePreviewRenderTexture.width != w || defaultSpinePreviewRenderTexture.height != h)
            {
                if (defaultSpinePreviewRenderTexture != null)
                    RenderTexture.ReleaseTemporary(defaultSpinePreviewRenderTexture);
                defaultSpinePreviewRenderTexture = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
                defaultSpinePreviewCamera.targetTexture = defaultSpinePreviewRenderTexture;
            }

            defaultSpinePreviewInstance.Rebuild(CanvasUpdate.PreRender);
            defaultSpinePreviewInstance.UpdateMesh();
            defaultSpinePreviewCamera.Render();
        }

        private void CreateDefaultSpinePreview()
        {
            DestroyDefaultSpinePreview();

            if (!symbol.useSpineDefault || symbol.defaultSpinePreview == null || symbol.defaultSpinePreview.skeletonDataAsset == null)
                return;

            defaultSpinePreviewRoot = new GameObject("DefaultSpinePreviewRoot");
            defaultSpinePreviewRoot.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontSaveInBuild | HideFlags.NotEditable;

            defaultSpinePreviewCanvas = defaultSpinePreviewRoot.AddComponent<Canvas>();
            defaultSpinePreviewCanvas.renderMode = RenderMode.ScreenSpaceCamera;

            defaultSpinePreviewCamera = new GameObject("DefaultSpinePreviewCamera").AddComponent<Camera>();
            defaultSpinePreviewCamera.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
            defaultSpinePreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            defaultSpinePreviewCamera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            defaultSpinePreviewCamera.orthographic = true;
            defaultSpinePreviewCamera.orthographicSize = 2f;
            defaultSpinePreviewCamera.transform.position = new Vector3(0, 0, -10);
            defaultSpinePreviewCamera.enabled = false;

            defaultSpinePreviewCanvas.worldCamera = defaultSpinePreviewCamera;

            defaultSpinePreviewInstance = new GameObject("DefaultSkeletonGraphicPreview").AddComponent<SkeletonGraphic>();
            defaultSpinePreviewInstance.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
            defaultSpinePreviewInstance.transform.SetParent(defaultSpinePreviewCanvas.transform, false);
            defaultSpinePreviewInstance.skeletonDataAsset = symbol.defaultSpinePreview.skeletonDataAsset;
            defaultSpinePreviewInstance.initialSkinName = symbol.defaultSpinePreview.initialSkinName;
            defaultSpinePreviewInstance.color = Color.white;
            defaultSpinePreviewInstance.rectTransform.sizeDelta = new Vector2(400, 400);
            defaultSpinePreviewInstance.rectTransform.anchorMin = Vector2.zero;
            defaultSpinePreviewInstance.rectTransform.anchorMax = Vector2.one;
            defaultSpinePreviewInstance.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            defaultSpinePreviewInstance.Initialize(true);

            CenterDefaultSpineBasedOnBounds();

            if (defaultSpineAnimationNames.Count > 0)
            {
                selectedDefaultSpineAnimation = defaultSpineAnimationNames[0];
                StartDefaultSpineAnimation();
            }
        }

        private void CenterDefaultSpineBasedOnBounds()
        {
            if (defaultSpinePreviewInstance == null || !defaultSpinePreviewInstance.IsValid) return;

            defaultSpinePreviewInstance.LateUpdate();

            Mesh mesh = defaultSpinePreviewInstance.GetLastMesh();
            if (mesh != null && mesh.vertexCount > 0)
            {
                mesh.RecalculateBounds();
                Bounds bounds = mesh.bounds;
                Vector3 offset = -bounds.center;
                defaultSpinePreviewInstance.transform.localPosition = offset;

                if (defaultSpinePreviewCamera != null && bounds.size != Vector3.zero)
                {
                    float width = bounds.size.x;
                    float height = bounds.size.y;
                    float requiredSize = Mathf.Max(height * 0.6f, width * 0.6f / defaultSpinePreviewCamera.aspect);
                    defaultSpinePreviewCamera.orthographicSize = requiredSize;
                }
            }
            else
            {
                if (defaultSpinePreviewCamera != null)
                    defaultSpinePreviewCamera.orthographicSize = 2f;
            }
        }

        private void DestroyDefaultSpinePreview()
        {
            if (defaultSpinePreviewRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(defaultSpinePreviewRenderTexture);
                defaultSpinePreviewRenderTexture = null;
            }
            if (defaultSpinePreviewCamera != null)
                DestroyImmediate(defaultSpinePreviewCamera.gameObject);
            if (defaultSpinePreviewRoot != null)
                DestroyImmediate(defaultSpinePreviewRoot);
            defaultSpinePreviewInstance = null;
        }

        private void StartDefaultSpineAnimation()
        {
            if (defaultSpinePreviewInstance == null || !defaultSpinePreviewInstance.IsValid || defaultSpineAnimationNames.Count == 0)
                return;

            if (string.IsNullOrEmpty(selectedDefaultSpineAnimation) && defaultSpineAnimationNames.Count > 0)
                selectedDefaultSpineAnimation = defaultSpineAnimationNames[0];

            if (!string.IsNullOrEmpty(selectedDefaultSpineAnimation))
            {
                defaultSpinePreviewInstance.AnimationState.SetAnimation(0, selectedDefaultSpineAnimation, true);
                isDefaultSpineAnimating = true;
                lastDefaultSpineTime = EditorApplication.timeSinceStartup;
            }
        }

        private void StopDefaultSpineAnimation()
        {
            if (defaultSpinePreviewInstance != null && defaultSpinePreviewInstance.IsValid)
                defaultSpinePreviewInstance.AnimationState.ClearTrack(0);
            isDefaultSpineAnimating = false;
        }

        private void UpdateDefaultSpineAnimationList()
        {
            defaultSpineAnimationNames.Clear();
            selectedDefaultSpineAnimation = "";

            if (symbol.defaultSpinePreview != null && symbol.defaultSpinePreview.SkeletonData != null)
            {
                var animations = symbol.defaultSpinePreview.SkeletonData.Animations;
                if (animations != null)
                {
                    foreach (var anim in animations)
                    {
                        defaultSpineAnimationNames.Add(anim.Name);
                    }
                }
                if (defaultSpineAnimationNames.Count > 0)
                    selectedDefaultSpineAnimation = defaultSpineAnimationNames[0];
            }
        }

        private void DrawSpritePreview(Sprite sprite, string label)
        {
            if (sprite == null)
            {
                EditorGUILayout.HelpBox($"{label}: No sprite assigned", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(label);
            Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(false));
            EditorGUI.DrawTextureTransparent(rect, sprite.texture, ScaleMode.ScaleToFit);
        }

        private void DrawMaterialLine(Material material)
        {
            EditorGUILayout.Space(5);
            if (material == null) return;

            Color currentColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUILayout.ColorField("Material Color", currentColor);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(material, "Change Material Color");
                material.SetColor("_Color", newColor);
                EditorUtility.SetDirty(material);
            }
        }

        private void DrawSpriteAnimationSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sprite Animation Preview", EditorStyles.boldLabel);

            if (symbol.m_reelIconsList.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(isAnimating ? "■ Stop" : "▶ Play", GUILayout.Width(60)))
                {
                    if (isAnimating) StopAnimation(); else StartAnimation();
                }
                animSpeed = EditorGUILayout.Slider("Speed", animSpeed, 0f, 5f);
                EditorGUILayout.EndHorizontal();
                DrawCurrentAnimationFrame();
            }
            else
            {
                EditorGUILayout.HelpBox("No animation frames added", MessageType.Info);
            }
        }

        private void DrawCurrentAnimationFrame()
        {
            Sprite currentFrame = isAnimating ?
                symbol.m_reelIconsList[currentAnimFrame] :
                symbol.m_reelIconsList.Count > 0 ? symbol.m_reelIconsList[0] : null;

            if (currentFrame != null)
                DrawSpritePreview(currentFrame, $"Frame {currentAnimFrame + 1}/{symbol.m_reelIconsList.Count}");
        }

        private void DrawSpineSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spine Animation Preview", EditorStyles.boldLabel);

            if (symbol.skeletonGraphic == null)
            {
                EditorGUILayout.HelpBox("No SkeletonGraphic assigned", MessageType.Warning);
                return;
            }

            DrawSpineAnimationControls();
            DrawSkeletonGraphicPreview();
        }

        private void DrawSpineAnimationControls()
        {
            EditorGUILayout.BeginHorizontal();
            if (spineAnimationNames.Count > 0)
            {
                int currentIndex = spineAnimationNames.IndexOf(selectedSpineAnimation);
                int newIndex = EditorGUILayout.Popup("Animation", currentIndex, spineAnimationNames.ToArray());
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < spineAnimationNames.Count)
                {
                    selectedSpineAnimation = spineAnimationNames[newIndex];
                    if (isSpineAnimating) StartSpineAnimation();
                }
            }
            else
            {
                EditorGUILayout.Popup("Animation", 0, new string[] { "No animations" });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isSpineAnimating ? "■ Stop" : "▶ Play", GUILayout.Width(60)))
            {
                if (isSpineAnimating) StopSpineAnimation(); else StartSpineAnimation();
            }
            if (GUILayout.Button("Update Animations", GUILayout.Width(140)))
            {
                UpdateSpineAnimationList();
                CreateSpinePreview();
            }
            EditorGUILayout.EndHorizontal();

            if (spinePreviewInstance != null)
            {
                EditorGUILayout.BeginHorizontal();
                float currentTimeScale = spinePreviewInstance.timeScale;
                float newTimeScale = EditorGUILayout.Slider("Animation Speed", currentTimeScale, 0f, 2f);
                if (newTimeScale != currentTimeScale)
                    spinePreviewInstance.timeScale = newTimeScale;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSkeletonGraphicPreview()
        {
            spinePreviewRect = GUILayoutUtility.GetRect(356, 356, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(spinePreviewRect, new Color(0.1f, 0.1f, 0.1f, 1f));

            if (spinePreviewInstance == null || !spinePreviewInstance.IsValid)
            {
                EditorGUI.LabelField(spinePreviewRect, "Spine not initialized", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            RenderSpineToTexture();
            if (renderTexture != null)
                GUI.DrawTexture(spinePreviewRect, renderTexture, ScaleMode.ScaleToFit);
        }

        private void RenderSpineToTexture()
        {
            if (spinePreviewInstance == null || previewCamera == null) return;

            int w = Mathf.Max(1, (int)spinePreviewRect.width);
            int h = Mathf.Max(1, (int)spinePreviewRect.height);

            if (renderTexture == null || renderTexture.width != w || renderTexture.height != h)
            {
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                renderTexture = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
                previewCamera.targetTexture = renderTexture;
            }

            spinePreviewInstance.Rebuild(CanvasUpdate.PreRender);
            spinePreviewInstance.UpdateMesh();
            previewCamera.Render();
        }

        private void CreateSpinePreview()
        {
            DestroySpinePreview();

            if (!symbol.onSpine || symbol.skeletonGraphic == null || symbol.skeletonGraphic.skeletonDataAsset == null)
                return;

            previewRootGO = new GameObject("SpinePreviewRoot");
            previewRootGO.hideFlags = HideFlags.HideAndDontSave;
            previewRootGO.hideFlags |= HideFlags.DontSaveInBuild | HideFlags.NotEditable;

            previewCanvas = previewRootGO.AddComponent<Canvas>();
            previewCanvas.renderMode = RenderMode.ScreenSpaceCamera;

            previewCamera = new GameObject("SpinePreviewCamera").AddComponent<Camera>();
            previewCamera.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 2f;
            previewCamera.transform.position = new Vector3(0, 0, -10);
            previewCamera.enabled = false;

            previewCanvas.worldCamera = previewCamera;

            spinePreviewInstance = new GameObject("SkeletonGraphicPreview").AddComponent<SkeletonGraphic>();
            spinePreviewInstance.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
            spinePreviewInstance.transform.SetParent(previewCanvas.transform, false);
            spinePreviewInstance.skeletonDataAsset = symbol.skeletonGraphic.skeletonDataAsset;
            spinePreviewInstance.initialSkinName = symbol.skeletonGraphic.initialSkinName;
            spinePreviewInstance.color = Color.white;
            spinePreviewInstance.rectTransform.sizeDelta = new Vector2(400, 400);
            spinePreviewInstance.rectTransform.anchorMin = Vector2.zero;
            spinePreviewInstance.rectTransform.anchorMax = Vector2.one;
            spinePreviewInstance.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            spinePreviewInstance.Initialize(true);

            CenterSpineBasedOnBounds();
        }

        private void CenterSpineBasedOnBounds()
        {
            if (spinePreviewInstance == null || !spinePreviewInstance.IsValid) return;

            spinePreviewInstance.LateUpdate();

            Mesh mesh = spinePreviewInstance.GetLastMesh();
            if (mesh != null && mesh.vertexCount > 0)
            {
                mesh.RecalculateBounds();
                Bounds bounds = mesh.bounds;
                Vector3 offset = -bounds.center;
                spinePreviewInstance.transform.localPosition = offset;

                if (previewCamera != null && bounds.size != Vector3.zero)
                {
                    float width = bounds.size.x;
                    float height = bounds.size.y;
                    float requiredSize = Mathf.Max(height * 0.6f, width * 0.6f / previewCamera.aspect);
                    previewCamera.orthographicSize = requiredSize;
                }
            }
            else
            {
                if (previewCamera != null)
                    previewCamera.orthographicSize = 2f;
            }
        }

        private void DestroySpinePreview()
        {
            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                renderTexture = null;
            }
            if (previewCamera != null)
                DestroyImmediate(previewCamera.gameObject);
            if (previewRootGO != null)
                DestroyImmediate(previewRootGO);
            spinePreviewInstance = null;
        }

        private void StartAnimation()
        {
            isAnimating = true;
            currentAnimFrame = 0;
            animTimer = 0f;
        }

        private void StopAnimation()
        {
            isAnimating = false;
            currentAnimFrame = 0;
            animTimer = 0f;
        }

        private void StartSpineAnimation()
        {
            if (spinePreviewInstance == null || !spinePreviewInstance.IsValid || spineAnimationNames.Count == 0)
                return;

            if (string.IsNullOrEmpty(selectedSpineAnimation))
                selectedSpineAnimation = spineAnimationNames[0];

            spinePreviewInstance.AnimationState.SetAnimation(0, selectedSpineAnimation, true);
            isSpineAnimating = true;
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        private void StopSpineAnimation()
        {
            if (spinePreviewInstance != null && spinePreviewInstance.IsValid)
                spinePreviewInstance.AnimationState.ClearTrack(0);
            isSpineAnimating = false;
        }

        private void UpdateSpineAnimationList()
        {
            spineAnimationNames.Clear();
            selectedSpineAnimation = "";
            if (symbol.skeletonGraphic != null && symbol.skeletonGraphic.SkeletonData != null)
            {
                var animations = symbol.skeletonGraphic.SkeletonData.Animations;
                if (animations != null)
                {
                    foreach (var anim in animations)
                        spineAnimationNames.Add(anim.Name);
                }
                if (spineAnimationNames.Count > 0)
                    selectedSpineAnimation = spineAnimationNames[0];
            }
        }
    }
}
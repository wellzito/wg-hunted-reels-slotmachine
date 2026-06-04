using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WG_Casino.Setup;
using PrimeTween;
using WG_Casino.Systems;
using static WG_Casino.SlotMachine.WG_SlotMachine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace WG_Casino.SlotMachine
{
    public class WG_SlotMachine : MonoBehaviour
    {
        public static WG_SlotMachine Instance;

        [Header("Slot Machine"), Space(10)]
        public WG_SlotSetup gameSetup;
        public ReelAnimationType m_animationType = ReelAnimationType.IndependentRows;
        public bool onMask2D = false;
        public bool onSimulatorIcon = false;

        [Header("Lines Rows")]
        public List<WG_LineRows> lineRows = new List<WG_LineRows>();

        public bool m_gameRunning;
        public bool m_gameEnded;

        private WG_SlotPaymentSystem m_paymentSystem;
        public WG_SlotPaymentSystem PaymentSystem => m_paymentSystem;

        [SerializeField] private bool m_replay = false;

        public Dictionary<int, Sprite> m_numberSprites;
        public Dictionary<int, Sprite> m_reelIcons;
        public Dictionary<int, Sprite> m_reelIconsMove;

        // Adicione esta variável na seção de variáveis públicas
        [Header("Animation Settings")]
        [SerializeField] public bool m_useAnimationSlots = false;
        public bool OnAnimationSlots
        {
            get { return m_useAnimationSlots; }
            set
            {
                m_useAnimationSlots = value;
            }
        }

        [SerializeField] private bool m_needToPay = false;
        private bool stopEnd = false;
        private bool onStop;
        [SerializeField] private float timeStopMax = 3;
        [SerializeField] private float timeStopMaxTurbo = 1;
        public bool onSpeedMode;

        private float timeStopCur;

        /*[HideInInspector]*/ public List<PreviewsReels> previewsReels = new List<PreviewsReels>();

        [System.Serializable]
        public class PreviewsReels
        {
            public int slot2;
            public List<int> slots = new List<int>();
        }

        public bool onBet;

        public int reelsIndex = 0;

        [Header("Callback System")]
        [SerializeField] private List<UnityEvent> m_onReelsStartedCallbacks = new List<UnityEvent>();
        [SerializeField] private List<UnityEvent> m_onReelsCompletedCallbacks = new List<UnityEvent>();
        [SerializeField] private bool m_isCallbackInProgress = false;
        [SerializeField] private float m_callbackCooldown = 0.1f;
        private Coroutine m_currentCallbackCoroutine;

        // Eventos públicos para outros scripts se inscreverem
        public System.Action OnReelsCompleted;
        public System.Action OnReelsStarted;

        public List<UnityEvent> OnReelsStartedCallbacks => m_onReelsCompletedCallbacks;
        public List<UnityEvent> OnReelsCompletedCallbacks => m_onReelsCompletedCallbacks;
        public bool IsCallbackInProgress => m_isCallbackInProgress;


        private void Awake()
        {
            Instance = this;
            m_paymentSystem = GetComponent<WG_SlotPaymentSystem>();
            m_reelIcons = new Dictionary<int, Sprite>();
            m_reelIconsMove = new Dictionary<int, Sprite>();
            m_numberSprites = new Dictionary<int, Sprite>();

            Sprite[] numSprites = Resources.LoadAll<Sprite>("Graphics/SlotMachine/numbers");

            for (int i = 0; i < numSprites.Length; i++)
                m_numberSprites.Add(i, numSprites[i]);
        }

        public void InitializeGame(WG_LineRows lineRows)
        {
            m_replay = false;

            var m_symbols = gameSetup.m_symbols;

            Sprite[] iconsArray = new Sprite[m_symbols.Count];
            for (int i = 0; i < m_symbols.Count; i++)
            {
                iconsArray[i] = m_symbols[i].defaultSprite;
            }

            Sprite[] iconsArrayMove = new Sprite[m_symbols.Count];

            for (int i = 0; i < m_symbols.Count; i++)
            {
                iconsArrayMove[i] = m_symbols[i].moveSprite;
            }

            foreach (WG_SlotMachineReel r in lineRows.m_reels)
            {
                r.AnimationType = m_animationType;
                r.InitializeReel(this, iconsArray, iconsArrayMove);
            }

            lineRows.EnableMask(false);
        }

        public List<Sprite> GetAnimationSpritesForSymbol(int symbolIndex)
        {
            var m_symbols = gameSetup.m_symbols;

            if (symbolIndex >= 0 && symbolIndex < m_symbols.Count)
            {
                return m_symbols[symbolIndex].m_reelIconsList;
            }
            return null;
        }

        private void Start()
        {
            foreach (var line in lineRows)
                SetWizard(line);

            int numberOfReels = lineRows.Count > 0 ? lineRows[0].m_reels.Count : 5;
            previewsReels.Clear();
            for (int i = 0; i < numberOfReels; i++)
            {
                previewsReels.Add(new PreviewsReels());
            }

            foreach (var line in lineRows)
                InitializeGame(line);
        }

        public void ExternalStart()
        {
            foreach (var line in lineRows)
                SetWizard(line);

            int numberOfReels = lineRows.Count > 0 ? lineRows[0].m_reels.Count : 5;
            previewsReels.Clear();
            for (int i = 0; i < numberOfReels; i++)
            {
                previewsReels.Add(new PreviewsReels());
            }

            foreach (var line in lineRows)
                InitializeGame(line);
        }

        public void SetWizard(WG_LineRows line)
        {
            if (!gameSetup.slotPattern) return;

            m_animationType = gameSetup.slotPattern.animationType;

            // Primeiro encontra o índice desta linha no WG_SlotSetup
            int currentLineIndex = lineRows.IndexOf(line);

            for (int i = 0; i < line.m_reels.Count; i++)
            {
                line.m_reels[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < gameSetup.slotPattern.reelCount; i++)
            {
                if (line.m_reels.Count <= i) continue;
                line.m_reels[i].gameObject.SetActive(true);
                line.m_reels[i].onShakeSlot = gameSetup.slotPattern.onShakeSlot;
            }

            // Lógica específica para IndependentRows
            if (gameSetup.slotPattern.animationType == ReelAnimationType.IndependentRows || gameSetup.slotPattern.animationType == ReelAnimationType.CascadeFall)
            {
                for (int reelIndex = 0; reelIndex < gameSetup.slotPattern.reelCount; reelIndex++)
                {
                    // Para cada reel, pega o valor do slot correspondente à LINHA ATUAL
                    // currentLineIndex = qual linha estamos (0, 1, 2, 3, 4, 5)
                    // reelIndex = qual reel estamos (0, 1, 2, 3, 4)
                    bool lineActiveState = gameSetup.slotPattern.IsSlotActive(reelIndex, currentLineIndex);
                    if(line.m_reels.Count <= reelIndex) continue;
                    // Aplica o mesmo valor para TODOS os 12 slots deste reel
                    for (int slotIndex = 0; slotIndex < line.m_reels[reelIndex].m_icons.Length; slotIndex++)
                    {
                        line.m_reels[reelIndex].m_icons[slotIndex].bg.SetActive(lineActiveState);
                        line.m_reels[reelIndex].m_icons[slotIndex].onShakeSlot = gameSetup.slotPattern.onShakeSlot;
                    }
                }
            }
            else
            {
                // Comportamento original para outros tipos de animação
                for (int i = 0; i < gameSetup.slotPattern.reelCount; i++)
                {
                    for (int j = 0; j < line.m_reels[i].m_icons.Length; j++)
                    {
                        line.m_reels[i].m_icons[j].bg.SetActive(gameSetup.slotPattern.IsSlotActive(i, j));
                        line.m_reels[i].m_icons[j].onShakeSlot = gameSetup.slotPattern.onShakeSlot;
                    }
                }
            }
        }
        void ResetLines()
        {
            m_gameEnded = false;
            m_needToPay = false;
            m_replay = false;
        }

        IEnumerator Delay(float time = 2f)
        {
            float timer = 0f;

            while (timer < time)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            ResetLines();
        }

        private void Update()
        {
            if(previewsReels.Count == 0)
            {
                int numberOfReels = lineRows.Count > 0 ? lineRows[0].m_reels.Count : 5;
                previewsReels.Clear();
                for (int i = 0; i < numberOfReels; i++)
                {
                    previewsReels.Add(new PreviewsReels());
                }
            }
            if (m_gameRunning) // Reels are spinning
            {
                bool stop = false;
                timeStopCur += Time.deltaTime;

                if ((WG_InputManager.IsConfirmInputPressedDown() || timeStopCur >= (onSpeedMode ? timeStopMaxTurbo : timeStopMax)) && !onStop || m_animationType == ReelAnimationType.CascadeFall && !onStop)
                {
                    onStop = true;
                    foreach (var line in lineRows) StartCoroutine(StopReels(line));
                }
                stop = stopEnd;

                if (stop)
                {
                    stopEnd = false;
                    timeStopCur = 0;
                    onStop = false;
                    m_gameRunning = false;
                    m_gameEnded = true;
                    WG_AudioManager.PlaySEStop();
                    Payout();
                }
            }
            else if (m_gameEnded)
            {

                StartCoroutine(Delay(2f));
                if (WG_InputManager.IsConfirmInputPressedDown())
                {
                    ResetLines();
                }
            }
            else if (m_needToPay)
            {
                return;
            }
            else if (onBet && !m_gameRunning)
            {
                onBet = false;
                m_gameRunning = true;
                WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_spinner);

                foreach (var line in lineRows)
                {
                    if (reelsIndex == 0)
                    {
                        foreach (WG_SlotMachineReel reel in line.m_reels)
                        {
                            reel.StartSpinning();
                        }
                    }
                    else
                    {
                        line.m_reels[reelsIndex - 1].StartSpinning();
                    }
                }
            }

            if (gameSetup.onUpdateWizard)
            {
                gameSetup.onUpdateWizard = false;
                foreach (var line in lineRows)
                    SetWizard(line);
            }
        }

        IEnumerator StopReels(WG_LineRows line)
        {
            if (reelsIndex == 0)
            {
                for (int i = 0; i < line.m_reels.Count; i++)
                {
                    yield return new WaitUntil(() => line.m_reels[i].IsSpinning);
                    if (line.m_reels[i].IsSpinning)
                    {
                        //line.m_reels[i].m_index = previewsReels[i].slot2;
                        line.m_reels[i].StopSpinning(m_replay);
                    }
                    yield return new WaitForSeconds(.1f);
                }

                line.EnableMask(false);
            }
            else
            {
                WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_slotStop[reelsIndex - 1]);

                for (int i = 0; i < line.m_reels.Count; i++)
                {
                    line.EnableMask(false);

                    yield return new WaitUntil(() => line.m_reels[i].IsSpinning);
                    if (line.m_reels[i].IsSpinning)
                    {
                        line.m_reels[i].m_index = previewsReels[i].slot2;
                        line.m_reels[i].StopSpinning(m_replay);
                    }
                }
            }

            yield return StartCoroutine(WaitForColumnsCompletion());
            stopEnd = true;
        }
        IEnumerator WaitForColumnsCompletion()
        {
            int totalColumns = lineRows[0].m_reels.Count; // Quantidade de colunas

            for (int columnIndex = 0; columnIndex < totalColumns; columnIndex++)
            {
                //Debug.Log($"Verificando coluna {columnIndex}...");

                // Aguardar até que TODOS os reels desta coluna em TODAS as linhas parem
                yield return StartCoroutine(WaitForColumnStop(columnIndex));

                //Debug.Log($"Coluna {columnIndex} completamente parada!");

                // Aqui você pode fazer algo específico quando cada coluna parar
                //OnColumnStopped(columnIndex);
            }

            //Debug.Log("TODAS AS COLUNAS PARARAM!");
        }

        IEnumerator WaitForColumnStop(int columnIndex)
        {
            bool allStopped = false;

            while (!allStopped)
            {
                allStopped = true;

                // Verificar se TODOS os reels desta coluna em TODAS as linhas pararam
                foreach (var line in lineRows)
                {
                    if (columnIndex < line.m_reels.Count) // Verificar se a coluna existe nesta linha
                    {
                        var reel = line.m_reels[columnIndex];

                        // Verificar se o reel ainda está em algum estado de movimento
                        if (reel.m_spinning || reel.m_preSpinning || reel.m_stopping || reel.m_stoppingEnd || reel.m_preSpinPause)
                        {
                            allStopped = false;
                            break; // Já sabemos que não parou tudo, pode sair do loop
                        }
                    }
                }

                if (!allStopped)
                    yield return null; // Aguardar próximo frame
            }
        }


        void Payout()
        {
            StartCoroutine(PayoutRoutine());
        }
        IEnumerator PayoutRoutine()
        {
            // Iniciar callbacks após o payout
            StartCallbackSequence();

            yield return new WaitUntil(() => !m_isCallbackInProgress);

            foreach (var line in lineRows)
            {
                m_paymentSystem.CheckWinningCombinationsAndShowLines(line);
                line.EnableMask(false);
            }

            foreach (var line in lineRows)
            {
                //StartCoroutine(CheckPay(line));
            }
            //checkPayCoroutine = StartCoroutine(CheckPay());
        }
        private Row[] ConvertRowsToReels(Row[] rows)
        {
            if (rows == null || rows.Length == 0)
                return null;

            // Determinar o número de reels (colunas) baseado no primeiro elemento
            int numReels = rows[0]?.elements?.Length ?? 0;

            if (numReels == 0)
                return null;

            Row[] reels = new Row[numReels];

            for (int reelIndex = 0; reelIndex < numReels; reelIndex++)
            {
                // Cada reel terá um número de elementos igual ao número de linhas
                byte[] reelElements = new byte[rows.Length];

                for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    // Pega o elemento da coluna 'reelIndex' na linha 'rowIndex'
                    if (rows[rowIndex]?.elements != null && reelIndex < rows[rowIndex].elements.Length)
                    {
                        reelElements[rowIndex] = rows[rowIndex].elements[reelIndex];
                    }
                    else
                    {
                        reelElements[rowIndex] = 0; // Valor padrão se não existir
                    }
                }

                reels[reelIndex] = new Row { elements = reelElements };
            }

            return reels;
        }

        IEnumerator CheckPay(WG_LineRows line)
        {
            var slotSetup = gameSetup;
            m_paymentSystem.CheckPayLine(line);
            for (int p = 0; p < slotSetup.paymentPatterns.Count; p++)
            {
                var LinePay = slotSetup.paymentPatterns.Count == 0 ? slotSetup.curPaymentPattern : slotSetup.paymentPatterns[p];
                if (m_paymentSystem.CheckPayment(LinePay))
                {
                    foreach (var item in line.m_reels)
                    {
                        foreach (var ic in item.m_icons)
                        {
                            ic.Fade(true);
                            ic.CardGlow(false);
                        }
                    }
                    for (int i = 0; i < previewsReels.Count; i++)
                    {
                        var icon = line.m_reels[LinePay.selectedSlots[i].reelIndex].m_icons[LinePay.selectedSlots[i].rowIndex];
                        switch (LinePay.selectedSlots[i].rowIndex)
                        {
                            case 3:
                                icon.ShowLineMark();
                                icon.ShowLine(LinePay.selectedSlots[i].slotSprite);
                                icon.Fade(false);
                                icon.CardGlow(true);
                                break;

                            case 4:
                                icon.ShowLineMark();
                                icon.ShowLine(LinePay.selectedSlots[i].slotSprite);
                                icon.Fade(false);
                                icon.CardGlow(true);
                                break;
                            case 5:
                                icon.ShowLineMark();
                                icon.ShowLine(LinePay.selectedSlots[i].slotSprite);
                                icon.Fade(false);
                                icon.CardGlow(true);
                                break;
                        }
                    }

                    //if (!onSound)
                    {
                        //onSound = true;
                        WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_line);
                    }
                }
                yield return new WaitForSeconds(2f);
            }
        }

        bool onSound = false;
        IEnumerator ShowWinningLine(int lineIndex, int linesToCheck, WG_LineRows line)
        {
            if (lineIndex < 3)
            {
                // Linha central
                int[] iconIndices = { 3, 4, 5 }; // Índices das posições centralizadas
                if (linesToCheck == 1)
                {
                    int iconIndex = iconIndices[1];

                    int sprite1 = line.m_reels[0].m_icons[iconIndex].m_spriteNumber;
                    int sprite2 = line.m_reels[1].m_icons[iconIndex].m_spriteNumber;
                    int sprite3 = line.m_reels[2].m_icons[iconIndex].m_spriteNumber;

                    // Conta quantos são 9
                    int countNines = (sprite1 == 9 ? 1 : 0) + (sprite2 == 9 ? 1 : 0) + (sprite3 == 9 ? 1 : 0);

                    // Se os três símbolos são iguais
                    bool allEqual = (sprite1 == sprite2 && sprite2 == sprite3);

                    // Se há 1 "9" e os outros dois são iguais
                    bool oneNineAndEqualOthers = (countNines == 1) && ((sprite1 == sprite2) || (sprite1 == sprite3) || (sprite2 == sprite3));

                    // Se há exatamente dois "9"
                    bool twoNines = countNines == 2;

                    if (allEqual || oneNineAndEqualOthers || twoNines)
                    {
                        Debug.Log($"twoNines: {twoNines}, oneNineAndEqualOthers: {oneNineAndEqualOthers}, allEqual: {allEqual}");

                        if (!onSound)
                        {
                            onSound = true;
                            WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_line);
                        }
                        line.m_reels[0].m_icons[iconIndex].ShowLineMark();
                        line.m_reels[1].m_icons[iconIndex].ShowLineMark();
                        line.m_reels[2].m_icons[iconIndex].ShowLineMark();

                        yield return new WaitForSeconds(.2f);
                        line.m_reels[0].m_icons[iconIndex].ShowLineS(2);
                        line.m_reels[1].m_icons[iconIndex].ShowLineS(2);
                        line.m_reels[2].m_icons[iconIndex].ShowLineS(2);
                    }
                }
                else
                {
                    foreach (int iconIndex in iconIndices)
                    {

                        int sprite1 = line.m_reels[0].m_icons[iconIndex].m_spriteNumber;
                        int sprite2 = line.m_reels[1].m_icons[iconIndex].m_spriteNumber;
                        int sprite3 = line.m_reels[2].m_icons[iconIndex].m_spriteNumber;

                        // Conta quantos são 9
                        int countNines = (sprite1 == 9 ? 1 : 0) + (sprite2 == 9 ? 1 : 0) + (sprite3 == 9 ? 1 : 0);

                        // Se os três símbolos são iguais
                        bool allEqual = (sprite1 == sprite2 && sprite2 == sprite3);

                        // Se há 1 "9" e os outros dois são iguais
                        bool oneNineAndEqualOthers = (countNines == 1) && ((sprite1 == sprite2) || (sprite1 == sprite3) || (sprite2 == sprite3));

                        // Se há exatamente dois "9"
                        bool twoNines = countNines == 2;

                        if (allEqual || oneNineAndEqualOthers || twoNines)
                        {
                            Debug.Log($"twoNines: {twoNines}, oneNineAndEqualOthers: {oneNineAndEqualOthers}, allEqual: {allEqual}");

                            if (!onSound)
                            {
                                onSound = true;
                                WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_line);
                            }
                            line.m_reels[0].m_icons[iconIndex].ShowLineMark();
                            line.m_reels[1].m_icons[iconIndex].ShowLineMark();
                            line.m_reels[2].m_icons[iconIndex].ShowLineMark();

                            yield return new WaitForSeconds(.2f);
                            line.m_reels[0].m_icons[iconIndex].ShowLineS(2);
                            line.m_reels[1].m_icons[iconIndex].ShowLineS(2);
                            line.m_reels[2].m_icons[iconIndex].ShowLineS(2);
                        }
                    }
                }
            }

            if (linesToCheck > 3)
            {
                // Diagonais
                var diagonals = new[]
                {
                    new { Icons = new[] { 3, 4, 5 }, LineType = 0 }, // Diagonal esquerda
                    new { Icons = new[] { 5, 4, 3 }, LineType = 1 }  // Diagonal direita
                };

                foreach (var diagonal in diagonals)
                {
                    int sprite1 = line.m_reels[0].m_icons[diagonal.Icons[0]].m_spriteNumber;
                    int sprite2 = line.m_reels[1].m_icons[diagonal.Icons[1]].m_spriteNumber;
                    int sprite3 = line.m_reels[2].m_icons[diagonal.Icons[2]].m_spriteNumber;

                    // Conta quantos são 9
                    int countNines = (sprite1 == 9 ? 1 : 0) + (sprite2 == 9 ? 1 : 0) + (sprite3 == 9 ? 1 : 0);

                    // Se os três símbolos são iguais
                    bool allEqual = (sprite1 == sprite2 && sprite2 == sprite3);

                    // Se há 1 "9" e os outros dois são iguais
                    bool oneNineAndEqualOthers = (countNines == 1) && ((sprite1 == sprite2) || (sprite1 == sprite3) || (sprite2 == sprite3));

                    // Se há exatamente dois "9"
                    bool twoNines = countNines == 2;

                    if (allEqual || oneNineAndEqualOthers || twoNines)
                    {
                        if (!onSound)
                        {
                            onSound = true;
                            WG_AudioManager.PlaySE(WG_AudioManager.AudioClips.m_line);
                        }
                        line.m_reels[0].m_icons[diagonal.Icons[0]].ShowLineMark();
                        line.m_reels[1].m_icons[diagonal.Icons[1]].ShowLineMark();
                        line.m_reels[2].m_icons[diagonal.Icons[2]].ShowLineMark();

                        yield return new WaitForSeconds(.2f);
                        line.m_reels[0].m_icons[diagonal.Icons[0]].ShowLineS(diagonal.LineType);
                        line.m_reels[1].m_icons[diagonal.Icons[1]].ShowLineS(diagonal.LineType);
                        line.m_reels[2].m_icons[diagonal.Icons[2]].ShowLineS(diagonal.LineType);
                    }
                }
            }
        }

        //External
        public void BetPlayBtn()
        {
            if (!AreAllReelsCompleted()) return;

            onBet = true;
            reelsIndex = 0;

            // Disparar evento de início ANTES de começar a girar
            StartReelsStartedCallbacks();

            foreach (var line in lineRows)
            {
                m_paymentSystem.ClearAllLineMarks(line);

                //line.EnableMask(true);

                foreach (var item in line.m_reels)
                {
                    /*if (m_animationType == ReelAnimationType.IndependentRows)
                        item.m_icons[1].gameObject.SetActive(true);*/

                    foreach (var ic in item.m_icons)
                    {
                        ic.Fade(false);
                        ic.CardGlow(false);
                    }
                }
            }

        }

        /// <summary>
        /// Recebe uma única row (linha) com 5 elementos e atribui aos 5 reels da lineRow
        /// </summary>
        public void SetPaymentPlayResponseRow(Row row, WG_LineRows line)
        {
            if (row == null)
            {
                Debug.LogError("Row is null!");
                return;
            }

            if (row.elements == null || row.elements.Length == 0)
            {
                Debug.LogError("Row elements is null or empty!");
                return;
            }

            Debug.Log($"[SetPaymentPlayResponseRow] Line: {line.name}, Elements: [{string.Join(", ", row.elements)}]");

            // Para cada reel (coluna) nesta linha, atribuir o elemento correspondente
            for (int reelIndex = 0; reelIndex < line.m_reels.Count && reelIndex < row.elements.Length; reelIndex++)
            {
                WG_SlotMachineReel reel = line.m_reels[reelIndex];

                // Limpar slots existentes
                if (reelIndex < previewsReels.Count)
                {
                    previewsReels[reelIndex].slots.Clear();
                }
                else
                {
                    Debug.LogError($"previewsReels index {reelIndex} out of range! previewsReels.Count: {previewsReels.Count}");
                    continue;
                }

                reel.m_IndexIcons.Clear();

                // Adicionar o elemento deste reel/coluna
                byte elementValue = row.elements[reelIndex];

                if (reelIndex < previewsReels.Count)
                {
                    previewsReels[reelIndex].slots.Add(elementValue);
                }
                reel.m_IndexIcons.Add(elementValue);

                //Debug.Log($"[SetPaymentPlayResponseRow] Reel {reelIndex} recebeu elemento: {elementValue}");
            }

            // Aplicar os ícones
            foreach (var reel in line.m_reels)
            {
                reel.GetIcons();
            }
        }
        public void SetPaymentPlayResponse(Row[] rows, WG_LineRows line)
        {
            if (rows == null)
            {
                Debug.LogError("Rows array is null!");
                return;
            }

            // Verificar se o número de rows corresponde ao número de reels
            /*if (rows.Length != previewsReels.Count)
            {
                Debug.LogError($"Rows count ({rows.Length}) doesn't match reels count ({previewsReels.Count})");
                return;
            }*/

            for (int i = 0; i < previewsReels.Count; i++)
            {
                WG_SlotMachineReel reel = line.m_reels[i];
                // Limpar slots existentes antes de adicionar novos
                previewsReels[i].slots.Clear();
                reel.m_IndexIcons.Clear();
                // Verificar se a row atual não é nula
                if (rows[i] == null)
                {
                    Debug.LogWarning($"Row {i} is null!");
                    continue;
                }

                // Verificar se elements não é nulo
                if (rows[i].elements == null)
                {
                    Debug.LogWarning($"Elements in row {i} is null!");
                    continue;
                }

                // Adicionar todos os elementos da row ao reel correspondente
                for (int j = 0; j < rows[i].elements.Length; j++)
                {
                    previewsReels[i].slots.Add(rows[i].elements[j]);
                    reel.m_IndexIcons.Add(rows[i].elements[j]);
                }

                //Debug.Log($"Reel {i} populated with {rows[i].elements.Length} symbols: [{string.Join(", ", rows[i].elements)}]");
            }

            foreach (var reel in line.m_reels)
            {
                reel.GetIcons();
            }
        }


        //Checks
        public bool AreAllReelsCompleted()
        {
            foreach (var line in lineRows)
            {
                if (line == null) continue;

                foreach (var reel in line.m_reels)
                {
                    if (reel == null) continue;

                    // Usa o novo método unificado
                    if (reel.IsReelActive())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #region Callback System
        /// <summary>
        /// Executa callbacks quando os reels começam a girar
        /// </summary>
        private void StartReelsStartedCallbacks()
        {
            // Disparar evento de início
            OnReelsStarted?.Invoke();

            // Executar UnityEvents do Inspector
            foreach (var callbackEvent in m_onReelsStartedCallbacks)
            {
                if (callbackEvent != null)
                {
                    callbackEvent.Invoke();
                }
            }
        }
        /// <summary>
        /// Adiciona um callback via código para quando os reels começam
        /// </summary>
        public void AddStartCallback(System.Action callback)
        {
            if (callback != null)
                OnReelsStarted += callback;
        }
        /// <summary>
        /// Remove um callback via código para quando os reels começam
        /// </summary>
        public void RemoveStartCallback(System.Action callback)
        {
            if (callback != null)
                OnReelsStarted -= callback;
        }
        /// <summary>
        /// Adiciona um UnityEvent via código para início
        /// </summary>
        public void AddUnityEventStartCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null && !m_onReelsStartedCallbacks.Contains(callbackEvent))
                m_onReelsStartedCallbacks.Add(callbackEvent);
        }
        /// <summary>
        /// Remove um UnityEvent via código para início
        /// </summary>
        public void RemoveUnityEventStartCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null)
                m_onReelsStartedCallbacks.Remove(callbackEvent);
        }


        /// <summary>
        /// Inicia a sequência de callbacks quando os reels completam
        /// </summary>
        public void StartCallbackSequence()
        {
            if (m_isCallbackInProgress) return;

            if (m_currentCallbackCoroutine != null)
                StopCoroutine(m_currentCallbackCoroutine);

            m_currentCallbackCoroutine = StartCoroutine(ExecuteCallbacks());
        }

        /// <summary>
        /// Executa todos os callbacks registrados
        /// </summary>
        private IEnumerator ExecuteCallbacks()
        {
            m_isCallbackInProgress = true;

            // Disparar evento de completion
            OnReelsCompleted?.Invoke();

            // Executar UnityEvents do Inspector
            foreach (var callbackEvent in m_onReelsCompletedCallbacks)
            {
                if (callbackEvent != null)
                {
                    callbackEvent.Invoke();
                    yield return new WaitForSeconds(m_callbackCooldown);
                }
            }

            // Aguardar um frame para garantir que tudo foi processado
            yield return new WaitForEndOfFrame();

            m_isCallbackInProgress = false;
            m_currentCallbackCoroutine = null;
        }

        /// <summary>
        /// Adiciona um callback via código (System.Action)
        /// </summary>
        public void AddCompletionCallback(System.Action callback)
        {
            if (callback != null)
                OnReelsCompleted += callback;
        }

        /// <summary>
        /// Remove um callback via código (System.Action)
        /// </summary>
        public void RemoveCompletionCallback(System.Action callback)
        {
            if (callback != null)
                OnReelsCompleted -= callback;
        }

        /// <summary>
        /// Adiciona um UnityEvent via código
        /// </summary>
        public void AddUnityEventCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null && !m_onReelsCompletedCallbacks.Contains(callbackEvent))
                m_onReelsCompletedCallbacks.Add(callbackEvent);
        }

        /// <summary>
        /// Remove um UnityEvent via código
        /// </summary>
        public void RemoveUnityEventCallback(UnityEvent callbackEvent)
        {
            if (callbackEvent != null)
                m_onReelsCompletedCallbacks.Remove(callbackEvent);
        }

        /// <summary>
        /// Limpa todos os callbacks
        /// </summary>
        public void ClearAllCallbacks()
        {
            OnReelsCompleted = null;
            m_onReelsCompletedCallbacks.Clear();
        }

        /// <summary>
        /// Verifica se há callbacks em execução
        /// </summary>
        public bool CallbackInProgress()
        {
            return m_isCallbackInProgress;
        }

        #endregion

        public void ForceStop()
        {
            onStop = true;
            foreach (var line in lineRows) StartCoroutine(StopReels(line));

            var stop = stopEnd;

            if (stop)
            {
                stopEnd = false;
                timeStopCur = 0;
                onStop = false;
                m_gameRunning = false;
                m_gameEnded = true;
                WG_AudioManager.PlaySEStop();
                Payout();
            }

            GameEvents.StopAutoSpin();
        }

        public void ForceStopExit()
        {
            onStop = true;
            foreach (var line in lineRows) StartCoroutine(StopReels(line));

            var stop = stopEnd;

            if (stop)
            {
                stopEnd = false;
                timeStopCur = 0;
                onStop = false;
                m_gameRunning = false;
                m_gameEnded = true;
            }
        }

        public void ClearAnimations()
        {
            foreach (var line in lineRows)
            {
                m_paymentSystem.ClearAllLineMarks(line);

                foreach (var item in line.m_reels)
                {
                    if (m_animationType == ReelAnimationType.IndependentRows)
                        item.m_icons[1].gameObject.SetActive(false);

                    foreach (var icon in item.m_icons)
                    {
                        icon.lineMark.SetActive(false);
                        icon.linePayment.gameObject.SetActive(false);
                        icon.Fade(false);
                        icon.CardGlow(false);
                        icon.isActive = true;
                        icon.StopAnimation();
                    }
                }
            }
        }
    }
}

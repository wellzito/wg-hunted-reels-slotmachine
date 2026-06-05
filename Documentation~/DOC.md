```markdown
# WG Hunted Reels - Slot Machine

## Overview

WG Hunted Reels é um sistema completo de slot machine para Unity, projetado para ser modular, extensível e de fácil integração. O sistema inclui suporte a animações Spine, efeitos de tween com PrimeTween, sistema de eventos desacoplado e uma arquitetura que permite personalização total.

### Características Principais

- Slot machine 3x5 com sistema de pagamento por padrões
- 6 símbolos normais + 1 símbolo Wild
- Multiplicadores de 150x até 1x
- Sistema de bônus do polvo mecânico
- Mini-game interativo
- Sistema de replay com restauração completa de estado
- Auto-spin configurável (10, 25, 50, 100, 1000 spins)
- Win sequence (Big Win, Mega Win, Super Win)
- Suporte a animações Spine (opcional)
- Tweens suaves com PrimeTween (opcional)
- Sistema de eventos totalmente desacoplado
- Gerenciamento de áudio
- Interface de usuário completa
- Sistema de histórico (até 50 itens)
- Controles de velocidade (pausa e fast-forward)
- Suporte a múltiplos idiomas (português, inglês, espanhol)

## Installation

### Via Package Manager (Git URL)

1. Abra o Package Manager em Unity (Window > Package Manager)
2. Clique no botão "+" e selecione "Add package from git URL"
3. Insira a URL do repositório:
   ```
   https://github.com/wellzito/wg-hunted-reels-slotmachine.git
   ```
4. Clique em "Add"

### Via arquivo local

1. Baixe o pacote
2. Abra o Package Manager
3. Clique no botão "+" e selecione "Add package from disk"
4. Selecione o arquivo `package.json` do pacote

### Dependências

#### Obrigatórias

O pacote requer as seguintes dependências do Unity:

```json
{
  "com.unity.ugui": "1.0.0",
  "com.unity.textmeshpro": "3.0.6",
  "com.unity.mathematics": "1.2.6"
}
```

#### Opcionais

Para funcionalidades completas, instale:

| Pacote | Uso | Como instalar |
|--------|-----|---------------|
| **Spine** (com.esotericsoftware.spine.spine-unity) | Animações esqueléticas | Package Manager > Add package by name |
| **PrimeTween** (com.kybernetik.primetween) | Animações suaves | Package Manager > Add package by name |

Se estas dependências não estiverem instaladas, o sistema funciona com fallback para sprites e animações simples.

## Quick Start

### Configuração básica

```csharp
using UnityEngine;
using WG_Casino.Core;
using WG_Casino.SlotMachine;

public class MyGame : MonoBehaviour
{
    void Start()
    {
        // Solicitar um spin
        GameEvents.RequestSpin();
    }
}
```

### Exemplo completo de integração

```csharp
using UnityEngine;
using WG_Casino.Core;
using WG_Casino.SlotMachine;
using WG_Casino.Systems;

public class GameController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private TextMeshProUGUI winText;

    private void OnEnable()
    {
        // Inscrever nos eventos do jogo
        GameEvents.OnBalanceUpdated += UpdateBalanceUI;
        GameEvents.OnBetChanged += UpdateBetUI;
        GameEvents.OnPayout += OnWin;
        GameEvents.OnSpinCompleted += OnSpinComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnBalanceUpdated -= UpdateBalanceUI;
        GameEvents.OnBetChanged -= UpdateBetUI;
        GameEvents.OnPayout -= OnWin;
        GameEvents.OnSpinCompleted -= OnSpinComplete;
    }

    private void Start()
    {
        // Configurar valores iniciais
        GameEvents.ChangeBet(500); // R$5,00
    }

    public void OnPlayButtonClick()
    {
        GameEvents.RequestSpin();
    }

    public void OnAutoSpinButtonClick(int spins)
    {
        GameEvents.StartAutoSpin(spins);
    }

    private void UpdateBalanceUI(int balance)
    {
        balanceText.text = GameUtils.FormatCurrency(balance);
    }

    private void UpdateBetUI(int bet)
    {
        betText.text = GameUtils.FormatCurrency(bet);
    }

    private void OnWin(int amount, int symbolId)
    {
        winText.text = GameUtils.FormatCurrency(amount);
        StartCoroutine(ShowWinEffect());
    }

    private void OnSpinComplete()
    {
        Debug.Log("Spin completed!");
    }

    private IEnumerator ShowWinEffect()
    {
        // Efeito visual de vitória
        yield return new WaitForSeconds(2f);
        winText.text = "";
    }
}
```

### Configuração da Win Sequence

No Inspector do componente `PayoutAnimation`, configure os níveis de vitória:

| Nível | Nome | Threshold Base (aposta R$1) | Multiplicador | Descrição |
|-------|------|----------------------------|---------------|-----------|
| Big Win | "Big Win" | 2000 (R$20) | 20x | Vitória significativa |
| Mega Win | "Mega Win" | 5000 (R$50) | 50x | Grande vitória |
| Super Win | "Super Win" | 10000 (R$100) | 100x | Vitória extraordinária |

Os thresholds são automaticamente ajustados com base na aposta atual.

### Sistema de Eventos

Todos os eventos disponíveis em `GameEvents`:

```csharp
// Spin events
GameEvents.OnSpinRequested    // Antes de validar o spin
GameEvents.OnSpinApproved     // Spin aprovado
GameEvents.OnSpinDenied       // Spin negado (saldo insuficiente, etc)
GameEvents.OnSpinCompleted    // Spin finalizado

// Bet events
GameEvents.OnBetChanged       // Aposta alterada

// Balance events
GameEvents.OnBalanceUpdated   // Saldo atualizado
GameEvents.OnGain             // Ganho acumulado
GameEvents.OnPayout           // Pagamento processado
GameEvents.OnPayoutCompleted  // Pagamento finalizado

// Bonus events
GameEvents.OnBonusStarted     // Bônus iniciado
GameEvents.OnBonusEnded       // Bônus finalizado

// Auto-spin events
GameEvents.OnAutoSpinStarted  // Auto-spin iniciado
GameEvents.OnAutoSpinStopped  // Auto-spin parado
GameEvents.OnAutoSpinStep     // Um spin do auto-spin completado

// UI events
GameEvents.OnMessageShow      // Mostrar mensagem na UI
GameEvents.OnShakeRequested   // Solicitar efeito de shake
```

### Utilitários

```csharp
// Formatar moeda
string formatted = GameUtils.FormatCurrency(12345); // "R$ 123,45"

// Verificar saldo suficiente
bool hasCredits = GameUtils.HasSufficientCredits(credits, bet);

// Converter centavos para decimal
decimal value = GameUtils.CentsToDecimal(500); // 5.00m

// Converter decimal para centavos
int cents = GameUtils.DecimalToCents(5.00m); // 500
```

## Estrutura do Projeto

```
Packages/com.picdoor.slotmachine/
├── Runtime/
│   ├── Core/           # Eventos e utilitários (GameEvents, GameUtils)
│   ├── SlotMachine/    # Componentes visuais (WG_SlotMachine, WG_SlotMachineReel, WG_SlotMachineIcon)
│   ├── Payment/        # Sistema de pagamento (PayoutAnimation, WG_SlotPaymentSystem)
│   ├── Math/           # Cálculos matemáticos (SlotMathCalculator, SlotMathRunner)
│   ├── Network/        # Comunicação e histórico (SlotClient, SerializableLastPlay)
│   ├── Audio/          # Gerenciamento de áudio (WG_AudioManager)
│   ├── UI/             # Interface (CanvasManager, PopUpManager, AutoSpinManager)
│   ├── Input/          # Input do usuário (WG_InputManager)
│   └── Managers/       # Gerenciadores principais (GameManager, BonusManager, HistoryUIManager)
├── Editor/             # Ferramentas do editor
├── Samples~/           # Exemplos de integração
└── Documentation~/     # Documentação
```

## Licença

Este software é de uso restrito para testes. Para uso comercial, é necessária a aquisição de uma licença comercial.

- **Licença de Teste**: Gratuita para avaliação
- **Licença Comercial**: Mediante pagamento ao desenvolvedor

Leia o arquivo [LICENSE](Documentation~/license.md) para mais informações.

## Suporte

- **Desenvolvedor**: Well Gomes
- **Email**: suportegamebug@gmail.com
- **GitHub**: https://github.com/wellzito

---

**Copyright (c) 2026 Well Gomes. Todos os direitos reservados.**
```
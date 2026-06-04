using UnityEngine;

public class ScreenOrientationChecker : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private bool checkOnStart = true;
    [SerializeField] private bool checkOnUpdate = false;
    [SerializeField] private float checkInterval = 1f;

    [Header("UI de Aviso")]
    [SerializeField] private GameObject warningPanel;

    private float timer = 0f;

    void Start()
    {
        if (checkOnStart && Application.isMobilePlatform)
        {
            CheckScreenOrientation();
        }
    }

    void Update()
    {
        if (checkOnUpdate && Application.isMobilePlatform)
        {
            timer += Time.deltaTime;

            if (timer >= checkInterval)
            {
                CheckScreenOrientation();
                timer = 0f;
            }
        }
    }

    /// <summary>
    /// Verifica se deve fazer a verificação de orientação
    /// Retorna true apenas para dispositivos móveis (incluindo WebGL mobile)
    /// </summary>
    public bool ShouldCheckOrientation()
    {
        return Application.isMobilePlatform;
    }

    /// <summary>
    /// Verifica se a tela está em orientação vertical (retrato)
    /// </summary>
    public void CheckScreenOrientation()
    {
        // Só verifica se for dispositivo móvel
        if (!Application.isMobilePlatform)
            return;

        bool isPortrait = IsScreenPortrait();

        if (warningPanel != null)
        {
            warningPanel.SetActive(isPortrait);
        }

        if (isPortrait)
        {
            Debug.LogWarning("Tela está em modo retrato. Orientação recomendada: paisagem (horizontal)");
        }
    }

    /// <summary>
    /// Verifica se a tela está em modo retrato
    /// </summary>
    public bool IsScreenPortrait()
    {
        if (!Application.isMobilePlatform)
            return false;

#if UNITY_WEBGL
        // Para WebGL, Screen.orientation pode não estar disponível
        // Usamos a proporção da tela como indicador
        return Screen.height > Screen.width;
#else
            // Para builds nativos mobile
            ScreenOrientation orientation = Screen.orientation;
            return orientation == ScreenOrientation.Portrait || 
                   orientation == ScreenOrientation.PortraitUpsideDown;
#endif
    }

    /// <summary>
    /// Verifica se a tela tem proporção próxima de 16:9 (paisagem)
    /// </summary>
    public bool Is16_9AspectRatio()
    {
        if (!Application.isMobilePlatform)
            return false;

        float aspectRatio = (float)Screen.width / Screen.height;
        float targetRatio = 16f / 9f; // 1.777...

        return Mathf.Abs(aspectRatio - targetRatio) < 0.2f;
    }

    /// <summary>
    /// Força orientação paisagem (apenas mobile nativo)
    /// </summary>
    public void ForceLandscapeOrientation()
    {
        if (!Application.isMobilePlatform)
            return;

#if !UNITY_WEBGL
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Debug.Log("Orientação forçada para paisagem");
#else
        // Em WebGL, não podemos forçar orientação diretamente
        // Podemos mostrar uma mensagem para o usuário
        Debug.Log("WebGL: Por favor, gire seu dispositivo para modo paisagem");

        // Opcional: Podemos tentar solicitar tela cheia
        // Isso às vezes ajuda com a orientação
        if (Screen.fullScreen)
        {
            Screen.fullScreen = false;
        }
        else
        {
            Screen.fullScreen = true;
        }
#endif
    }

    /// <summary>
    /// Força orientação retrato (apenas mobile nativo)
    /// </summary>
    public void ForcePortraitOrientation()
    {
        if (!Application.isMobilePlatform)
            return;

#if !UNITY_WEBGL
            Screen.orientation = ScreenOrientation.Portrait;
            Debug.Log("Orientação forçada para retrato");
#else
        Debug.Log("WebGL: Por favor, gire seu dispositivo para modo retrato");
#endif
    }

    /// <summary>
    /// Permite todas as orientações (apenas mobile nativo)
    /// </summary>
    public void AllowAutoRotation()
    {
        if (!Application.isMobilePlatform)
            return;

#if !UNITY_WEBGL
            Screen.orientation = ScreenOrientation.AutoRotation;
            Debug.Log("Auto-rotação ativada");
#endif
    }

    // Método para ser chamado por botões UI
    public void CloseWarning()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }
}
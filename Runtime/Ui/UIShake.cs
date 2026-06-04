using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    [Header("Configurações do Shake")]
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.5f;

    [Header("Objetos UI para tremer")]
    [SerializeField] private List<RectTransform> uiObjectsToShake = new List<RectTransform>();

    [Header("Objetos 3D/2D para tremer")]
    [SerializeField] private List<Transform> worldObjectsToShake = new List<Transform>();

    [Header("Configurações Avançadas")]
    [SerializeField] private bool useReducedShake = false;
    [SerializeField] private float reductionFactor = 0.9f;
    [SerializeField] private bool shakeIn3D = true;
    [SerializeField] private bool useWorldSpace = false; // Nova opção para escolher entre espaço global ou local

    private Dictionary<RectTransform, Vector3> originalUIPositions = new Dictionary<RectTransform, Vector3>();
    private Dictionary<Transform, Vector3> originalWorldPositions = new Dictionary<Transform, Vector3>();
    private bool isShaking = false;

    void Start()
    {
        // Salva as posições originais dos objetos UI
        foreach (RectTransform rectTransform in uiObjectsToShake)
        {
            if (rectTransform != null)
            {
                originalUIPositions[rectTransform] = rectTransform.anchoredPosition;
            }
        }

        // Salva as posições originais dos objetos 3D/2D
        foreach (Transform worldTransform in worldObjectsToShake)
        {
            if (worldTransform != null)
            {
                // Salva a posição local ou global baseado na configuração
                originalWorldPositions[worldTransform] = useWorldSpace ? worldTransform.position : worldTransform.localPosition;
            }
        }
    }

    // Método público para iniciar o shake com valores padrão
    public void StartShake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(shakeIntensity, shakeDuration));
        }
    }

    // Método público para iniciar o shake com intensidade e duração customizadas
    public void StartShake(float intensity, float duration)
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }
    }

    // Método público para shake rápido (intensidade alta, duração curta)
    public void StartQuickShake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(shakeIntensity * 1.5f, shakeDuration * 0.3f));
        }
    }

    // Método público para shake suave (intensidade baixa, duração longa)
    public void StartSoftShake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(shakeIntensity * 0.5f, shakeDuration * 1.5f));
        }
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Aplica shake nos objetos UI
            foreach (RectTransform rectTransform in uiObjectsToShake)
            {
                if (rectTransform != null && originalUIPositions.ContainsKey(rectTransform))
                {
                    float currentIntensity = CalculateCurrentIntensity(intensity, elapsed, duration);
                    Vector2 shakeOffset = GenerateShakeOffset(currentIntensity);

                    Vector3 originalPos = originalUIPositions[rectTransform];
                    rectTransform.anchoredPosition = originalPos + (Vector3)shakeOffset;
                }
            }

            // Aplica shake nos objetos 3D/2D
            foreach (Transform worldTransform in worldObjectsToShake)
            {
                if (worldTransform != null && originalWorldPositions.ContainsKey(worldTransform))
                {
                    float currentIntensity = CalculateCurrentIntensity(intensity, elapsed, duration);
                    Vector3 shakeOffset = GenerateWorldShakeOffset(currentIntensity);

                    Vector3 originalPos = originalWorldPositions[worldTransform];

                    if (useWorldSpace)
                    {
                        worldTransform.position = originalPos + shakeOffset;
                    }
                    else
                    {
                        worldTransform.localPosition = originalPos + shakeOffset;
                    }
                }
            }

            yield return null;
        }

        // Retorna todos os objetos às suas posições originais
        ResetAllPositions();
        isShaking = false;
    }

    private float CalculateCurrentIntensity(float baseIntensity, float elapsed, float duration)
    {
        float currentIntensity = baseIntensity;
        if (useReducedShake)
        {
            currentIntensity = baseIntensity * (1 - (elapsed / duration) * reductionFactor);
        }
        return currentIntensity;
    }

    private Vector2 GenerateShakeOffset(float intensity)
    {
        float x = Random.Range(-1f, 1f) * intensity;
        float y = Random.Range(-1f, 1f) * intensity;
        return new Vector2(x, y);
    }

    private Vector3 GenerateWorldShakeOffset(float intensity)
    {
        if (shakeIn3D)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            float z = Random.Range(-1f, 1f) * intensity;
            return new Vector3(x, y, z);
        }
        else
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            return new Vector3(x, y, 0);
        }
    }

    // Retorna todos os objetos às posições originais
    public void ResetAllPositions()
    {
        // Reset objetos UI
        foreach (var kvp in originalUIPositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.anchoredPosition = kvp.Value;
            }
        }

        // Reset objetos 3D/2D
        foreach (var kvp in originalWorldPositions)
        {
            if (kvp.Key != null)
            {
                if (useWorldSpace)
                {
                    kvp.Key.position = kvp.Value;
                }
                else
                {
                    kvp.Key.localPosition = kvp.Value;
                }
            }
        }
    }

    // Métodos para adicionar objetos UI
    public void AddUIObject(RectTransform uiObject)
    {
        if (!uiObjectsToShake.Contains(uiObject))
        {
            uiObjectsToShake.Add(uiObject);
            originalUIPositions[uiObject] = uiObject.anchoredPosition;
        }
    }

    public void RemoveUIObject(RectTransform uiObject)
    {
        if (uiObjectsToShake.Contains(uiObject))
        {
            uiObjectsToShake.Remove(uiObject);
            originalUIPositions.Remove(uiObject);
        }
    }

    // Métodos para adicionar objetos 3D/2D
    public void AddWorldObject(Transform worldObject)
    {
        if (!worldObjectsToShake.Contains(worldObject))
        {
            worldObjectsToShake.Add(worldObject);
            originalWorldPositions[worldObject] = useWorldSpace ? worldObject.position : worldObject.localPosition;
        }
    }

    public void RemoveWorldObject(Transform worldObject)
    {
        if (worldObjectsToShake.Contains(worldObject))
        {
            worldObjectsToShake.Remove(worldObject);
            originalWorldPositions.Remove(worldObject);
        }
    }

    // Limpa todas as listas de objetos
    public void ClearAllUIObjects()
    {
        uiObjectsToShake.Clear();
        originalUIPositions.Clear();
    }

    public void ClearAllWorldObjects()
    {
        worldObjectsToShake.Clear();
        originalWorldPositions.Clear();
    }

    public void ClearAllObjects()
    {
        ClearAllUIObjects();
        ClearAllWorldObjects();
    }

    // Métodos para verificar status
    public bool IsShaking()
    {
        return isShaking;
    }

    public int GetTotalShakingObjects()
    {
        return uiObjectsToShake.Count + worldObjectsToShake.Count;
    }

    // Método para atualizar as posições originais (útil se os objetos se moverem durante o jogo)
    public void UpdateOriginalPositions()
    {
        foreach (RectTransform rectTransform in uiObjectsToShake)
        {
            if (rectTransform != null)
            {
                originalUIPositions[rectTransform] = rectTransform.anchoredPosition;
            }
        }

        foreach (Transform worldTransform in worldObjectsToShake)
        {
            if (worldTransform != null)
            {
                originalWorldPositions[worldTransform] = useWorldSpace ? worldTransform.position : worldTransform.localPosition;
            }
        }
    }
}
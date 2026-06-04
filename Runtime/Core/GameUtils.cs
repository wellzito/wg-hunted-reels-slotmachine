// GameUtils.cs
using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Utilitário independente para operações comuns do jogo.
/// Não depende de MonoBehaviour, GameManager ou CanvasManager.
/// </summary>
public static class GameUtils
{
    private static CultureInfo s_culture = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Obtém ou define a cultura atual para formatação de moeda.
    /// </summary>
    public static CultureInfo Culture
    {
        get => s_culture;
        set => s_culture = value ?? CultureInfo.GetCultureInfo("pt-BR");
    }

    /// <summary>
    /// Formata um valor em centavos para string de moeda localizada.
    /// Exemplo: 12345 -> "R$ 123.45" (para pt-BR)
    /// </summary>
    /// <param name="valueInCents">Valor em centavos (ex: 100 = R$ 1.00)</param>
    /// <returns>String formatada com símbolo da moeda</returns>
    public static string FormatCurrency(int valueInCents)
    {
        decimal realValue = valueInCents / 100m;
        return realValue.ToString("C2", Culture);
    }

    /// <summary>
    /// Formata um valor em centavos para string de moeda localizada, sem o símbolo.
    /// </summary>
    public static string FormatCurrencyValueOnly(int valueInCents)
    {
        decimal realValue = valueInCents / 100m;
        return realValue.ToString("F2", Culture);
    }

    /// <summary>
    /// Verifica se o saldo é suficiente para a aposta.
    /// </summary>
    /// <param name="creditsInCents">Saldo em centavos</param>
    /// <param name="betInCents">Aposta em centavos</param>
    /// <returns>True se créditos >= aposta</returns>
    public static bool HasSufficientCredits(int creditsInCents, int betInCents)
    {
        return creditsInCents >= betInCents;
    }

    /// <summary>
    /// Converte centavos para valor decimal.
    /// </summary>
    public static decimal CentsToDecimal(int cents)
    {
        return cents / 100m;
    }

    /// <summary>
    /// Converte valor decimal para centavos.
    /// </summary>
    public static int DecimalToCents(decimal value)
    {
        return (int)(value * 100);
    }

    public static IEnumerator Shake(Transform obj)
    {
        if (obj == null) yield break;

        Vector3 originalPos = obj.localPosition;
        float shakeTime = 0.3f;
        float elapsedTime = 0f;
        float intensity = 10f;

        while (elapsedTime < shakeTime)
        {
            elapsedTime += Time.deltaTime;
            float xShake = UnityEngine.Random.Range(-intensity, intensity);
            float yShake = UnityEngine.Random.Range(-intensity, intensity);
            obj.localPosition = originalPos + new Vector3(xShake, yShake, 0);
            yield return null;
        }
        obj.localPosition = originalPos;
    }
}
using PrimeTween;
using TMPro;
using UnityEngine;

public class TextAnimator : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    public TMP_Text GetText
    {
        get { return text; }
    }
    [SerializeField, Range(0.1f, 10f)] public float time = 1f;
    [SerializeField] bool money = false;
    [SerializeField] bool credits = false;
    public Tween tween = new Tween();

    private void OnValidate()
    {
        text = text ?? GetComponent<TMP_Text>();
    }

    public void SetAnimationNum(int inicial, int final, bool force = false)
    {
        final = (final < 0) ? 0 : final;
        inicial = (inicial < 0) ? 0 : inicial;

        tween.Stop();

        //if (money && inicial > final) WG_AudioManager.Instance.PlayFX(BingoSound.Coins);
        string inicialStr = $"{GameUtils.FormatCurrency(inicial)}";
        string finalStr = $"{GameUtils.FormatCurrency(final)}";
        if (force)
        {
            tween = Tween.Custom(inicial, final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                onValueChange: (value) => {
                    text.text = $"{GameUtils.FormatCurrency((int)value)}";
                });
            // Debug.Log($"Forçando animação: {GameUtils.FormatCurrency(final)}");
            return;
        }
        if (money && inicialStr != finalStr)
        {
            if (credits)
            {
                if (inicial > final)
                {
                    text.text = $"{GameUtils.FormatCurrency(inicial)}";
                    // Debug.Log($"Sem animação: inicial {inicial} | final {GameUtils.FormatCurrency(final)}");
                }
                else
                {
                    tween = Tween.Custom(inicial, final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                        onValueChange: (value) => {
                            text.text = $"{GameUtils.FormatCurrency((int)value)}";
                        });
                }
            }
            else
            {
                tween = Tween.Custom(inicial, final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                    onValueChange: (value) => {
                        text.text = $"{GameUtils.FormatCurrency((int)value)}";
                    });
            }
        }
        else
        {
            text.text = $"{GameUtils.FormatCurrency(final)}";
        }
    }

    public void SetAnimationNum(float inicial, float final , bool force = false)
    {
        final = (final < 0) ? 0 : final;
        inicial = (inicial < 0) ? 0 : inicial;

        tween.Stop();

        //if (money && inicial > final) WG_AudioManager.Instance.PlayFX(BingoSound.Coins);
        string inicialStr = $"{GameUtils.FormatCurrency((int)inicial)}";
        string finalStr = $"{GameUtils.FormatCurrency((int)final)}";
        if (force)
        {
            tween = Tween.Custom(inicial, final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                onValueChange: (value) => {
                    text.text = $"{GameUtils.FormatCurrency((int)value)}";
                });
            // Debug.Log($"Forçando animação: {GameUtils.FormatCurrency(final)}");
            return;
        }
        if (money && inicialStr != finalStr)
        {
            if (credits)
            {
                if (inicial > final)
                {
                    text.text = $"{GameUtils.FormatCurrency((int)inicial)}";
                    // Debug.Log($"Sem animação: inicial {inicial} | final {GameUtils.FormatCurrency(final)}");
                }
                else
                {
                    tween = Tween.Custom(inicial, final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                        onValueChange: (value) => {
                            text.text = $"{GameUtils.FormatCurrency((int)value)}";
                        });
                }
            }
            else
            {
            tween = Tween.Custom(inicial ,  final, duration: final == 0 ? time * 2 : time , ease: Ease.Linear, 
                onValueChange: (value) => {
                text.text = $"{GameUtils.FormatCurrency((int)value)}";
             });
            }
        }
        else
        {
            text.text = $"{GameUtils.FormatCurrency((int)final)}";
        }
    }

    public void SetAnimationNum(double inicial, double final, bool force = false)
    {
        final = (final < 0) ? 0 : final;
        inicial = (inicial < 0) ? 0 : inicial;

       /* inicial = System.Math.Floor(inicial * 100) / 100;
        final = System.Math.Floor(final * 100) / 100;*/

        tween.Stop();

        //if (money && inicial > final) WG_AudioManager.Instance.PlayFX(BingoSound.Coins);
        string inicialStr = $"{GameUtils.FormatCurrency((int)inicial)}";
        string finalStr = $"{GameUtils.FormatCurrency((int)final)}";
        if (force)
        {
            tween = Tween.Custom((float)inicial, (float)final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                onValueChange: (value) => {
                    text.text = $"{GameUtils.FormatCurrency((int)value)}";
                });
            // Debug.Log($"Forçando animação: {GameUtils.FormatCurrency(final)}");
            return;
        }
        if (money && inicialStr != finalStr)
        {
            if (credits)
            {
                if (inicial > final)
                {
                    text.text = $"{GameUtils.FormatCurrency((int)inicial)}";
                    // Debug.Log($"Sem animação: inicial {inicial} | final {GameUtils.FormatCurrency(final)}");
                }
                else
                {
                    tween = Tween.Custom((float)inicial, (float)final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                        onValueChange: (value) => {
                            text.text = $"{GameUtils.FormatCurrency((int)value)}";
                        });
                }
            }
            else
            {
                tween = Tween.Custom((float)inicial, (float)final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                    onValueChange: (value) => {
                        text.text = $"{GameUtils.FormatCurrency((int)value)}";
                    });
            }
        }
        else
        {
            text.text = $"{GameUtils.FormatCurrency((int)final)}";
        }
    }

    public void SetAnimationNum(AutoTranslate autoTranslate, double inicial, double final, bool force = false)
    {
        final = (final < 0) ? 0 : final;
        inicial = (inicial < 0) ? 0 : inicial;

        inicial = System.Math.Floor(inicial * 100) / 100;
        final = System.Math.Floor(final * 100) / 100;

        tween.Stop();

        //if (money && inicial > final) WG_AudioManager.Instance.PlayFX(BingoSound.Coins);
        string inicialStr = $"{GameUtils.FormatCurrency((int)inicial)}";
        string finalStr = $"{GameUtils.FormatCurrency((int)final)}";
        if (force)
        {
            tween = Tween.Custom((float)inicial, (float)final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                onValueChange: (value) => {
                    autoTranslate.Key = autoTranslate.Key;
                    text.text = $"{autoTranslate.value.text} {GameUtils.FormatCurrency((int)value)}";
                });
            // Debug.Log($"Forçando animação: {GameUtils.FormatCurrency(final)}");
            return;
        }
        if (money && inicialStr != finalStr)
        {
            if (credits)
            {
                if (inicial > final)
                {
                    autoTranslate.Key = autoTranslate.Key;
                    text.text = $"{autoTranslate.value.text} {GameUtils.FormatCurrency((int)inicial)}";
                    // Debug.Log($"Sem animação: inicial {inicial} | final {GameUtils.FormatCurrency(final)}");
                }
                else
                {
                    tween = Tween.Custom((float)inicial, (float)final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                        onValueChange: (value) => {
                            autoTranslate.Key = autoTranslate.Key;
                            text.text = $"{autoTranslate.value.text} {GameUtils.FormatCurrency((int)value)}";
                        });
                }
            }
            else
            {
                tween = Tween.Custom((float)inicial, (float)final, duration: final == 0 ? time * 2 : time, ease: Ease.Linear,
                    onValueChange: (value) => {
                        autoTranslate.Key = autoTranslate.Key;
                        text.text = $"{autoTranslate.value.text} {GameUtils.FormatCurrency((int)value)}";
                    });
            }
        }
        else
        {
            autoTranslate.Key = autoTranslate.Key;
            text.text = $"{autoTranslate.value.text} {GameUtils.FormatCurrency((int)final)}";
        }
    }

    public void SetText(float final)
    {
        text.text= $"{GameUtils.FormatCurrency((int)final)}";
    }

    public void SetText(double final)
    {
        text.text = $"{GameUtils.FormatCurrency((int)final)}";
    }

    public void SetText(AutoTranslate autoTranslate, double final)
    {
        autoTranslate.Key = autoTranslate.Key;
        text.text = $"{autoTranslate.value.text} {GameUtils.FormatCurrency((int)final)}";
    }
}

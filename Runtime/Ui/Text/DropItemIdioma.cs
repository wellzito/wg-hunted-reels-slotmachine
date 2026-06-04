using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropItemIdioma : MonoBehaviour
{
    public int index;
    public TextMeshProUGUI language;
    [SerializeField] Image im;
    private void Update()
    {
        for (int i = 0; i < CanvasManager.Instance.popUpManager.idiomaConfigDrop.options.Count; i++)
        {
            if (CanvasManager.Instance.popUpManager.idiomaConfigDrop.options[i].text == language.text)
            {
                index=i; break;
            }
        }
        UpdateTemplateFlags(index);
    }
    private void UpdateTemplateFlags(int _)
    {
        im.sprite = CanvasManager.Instance.popUpManager.idiomaImages[index];
    }
}

using UnityEngine;

[ExecuteAlways]
public class CanvasResizer : MonoBehaviour
{
    #region SINGLETON
    public static CanvasResizer Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }
    #endregion

    [Header("CanvasResizer")]
    [SerializeField] RectTransform canvas;
    [SerializeField] RectTransform gameContainer;
    [SerializeField] float HeightToWidthProportion = 0.5625f;
    float verticalFullHDGameAreaWith = 607.5f;

    void Start()
    {
        SetNewAspectRatio(HeightToWidthProportion);
    }

    public void SetNewAspectRatio(float newAspectRatio)
    {
        HeightToWidthProportion = newAspectRatio;
        verticalFullHDGameAreaWith = canvas.rect.height * HeightToWidthProportion;
    }

    void Update()
    {
        if ((canvas.sizeDelta.x <= verticalFullHDGameAreaWith) || (((float)Screen.width) / ((float)Screen.height) <= HeightToWidthProportion))
        {
            gameContainer.anchorMin = new Vector2(0f, 0f);
            gameContainer.anchorMax = new Vector2(1f, 1f);
            gameContainer.anchoredPosition = new Vector2(0f, 0f);
            gameContainer.sizeDelta = new Vector2(0f, 0f);
        }
        else
        {
            gameContainer.anchorMin = new Vector2(0.5f, 0f);
            gameContainer.anchorMax = new Vector2(0.5f, 1f);
            gameContainer.anchoredPosition = new Vector2(0f, 0f);
            gameContainer.sizeDelta = new Vector2(verticalFullHDGameAreaWith, 0f);
        }
    }
    public void toggleCanvasResizer(bool active)
    {
        enabled = active;
    }
}

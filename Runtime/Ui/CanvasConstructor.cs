using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
#endif

public class CanvasConstructor : MonoBehaviour
{
    public static CanvasConstructor Instance;
    enum ScreenOption { Undefined, PCLandscape, PCPortrait, MobileLandscape, MobilePortrait }
    ScreenOption screenOption = ScreenOption.Undefined;
    ScreenOption lastScreenOption = ScreenOption.Undefined;

    public bool forceMobilePlatform = false;

    public ScreenProp PCLandscape;
    public ScreenProp PCPortrait;
    public ScreenProp MobileLandscape;
    public ScreenProp MobilePortrait;


    void Awake()
    {
        Instance = this;
    }

    public virtual void Save()
    {
#if UNITY_EDITOR
        int count = 0;
        for (int i = 0; i < PCLandscape.homeElements.Count; i++)
        {
            Element e = PCLandscape.homeElements[i];
            if (e.instance == null)
            {
                continue;
            }
            count++;
            e.Save();
        }
#endif
    }

    void Update()
    {
        if (Application.isMobilePlatform || forceMobilePlatform)
        {
            if (Screen.width > Screen.height)
            {
                screenOption = ScreenOption.MobileLandscape;
                if (screenOption != lastScreenOption)
                {
                    deactivateLastScreenProp();
                    ApplyMobileLandscape();
                }
            }
            else
            {
                screenOption = ScreenOption.MobilePortrait;
                if (screenOption != lastScreenOption)
                {
                    deactivateLastScreenProp();
                    ApplyMobilePortrait();
                }
            }
        }
        else
        {
            if (Screen.width > Screen.height)
            {
                screenOption = ScreenOption.PCLandscape;
                if (screenOption != lastScreenOption)
                {
                    deactivateLastScreenProp();
                    ApplyPCLandscape();
                }
            }
            else
            {
                screenOption = ScreenOption.PCPortrait;
                if (screenOption != lastScreenOption)
                {
                    deactivateLastScreenProp();
                    ApplyPCPortrait();
                }
            }
        }
    }

    void deactivateLastScreenProp()
    {
        switch (lastScreenOption)
        {
            case ScreenOption.PCLandscape:
                PCLandscape.onDeactivate.Invoke();
                break;
            case ScreenOption.PCPortrait:
                PCPortrait.onDeactivate.Invoke();
                break;
            case ScreenOption.MobileLandscape:
                MobileLandscape.onDeactivate.Invoke();
                break;
            case ScreenOption.MobilePortrait:
                MobilePortrait.onDeactivate.Invoke();
                break;
            default:
                break;
        }
        lastScreenOption = screenOption;
    }

    public void SavePCLandscape()
    {
        PCLandscape.SaveAllChildrenTransforms(transform);
    }
    public void ApplyPCLandscape()
    {
        Debug.Log("Applying PCLandscape configurations");
        PCLandscape.ApplyAllChildrenTransforms();
    }
    public void SavePCPortrait()
    {
        PCPortrait.SaveAllChildrenTransforms(transform);
    }
    public void ApplyPCPortrait()
    {
        Debug.Log("Applying PCPortrait configurations");
        PCPortrait.ApplyAllChildrenTransforms();
    }
    public void SaveMobileLandscape()
    {
        MobileLandscape.SaveAllChildrenTransforms(transform);
    }
    public void ApplyMobileLandscape()
    {
        Debug.Log("Applying MobileLandscape configurations");
        MobileLandscape.ApplyAllChildrenTransforms();
    }
    public void SaveMobilePortrait()
    {
        MobilePortrait.SaveAllChildrenTransforms(transform);
    }
    public void ApplyMobilePortrait()
    {
        Debug.Log("Applying MobilePortrait configurations");
        MobilePortrait.ApplyAllChildrenTransforms();
    }
}

[Serializable]
public class ScreenProp
{
    [Serializable] public class ScreenProportionChanged : UnityEvent { }
    public ScreenProportionChanged onActivate;
    public ScreenProportionChanged onDeactivate;

    public List<Element> homeElements = new List<Element>();

    public void SaveAllChildrenTransforms(Transform rootCanvas)
    {
        homeElements.Clear();
        homeElements = SaveAllChildrenElements(rootCanvas, homeElements);
    }

    public void ApplyAllChildrenTransforms()
    {
        for (int i = 0; i < homeElements.Count; i++)
        {
            Element element = homeElements[i];
            element.Apply();
        }
        onActivate.Invoke();
    }

    List<Element> SaveAllChildrenElements(Transform parent, List<Element> elements)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Element element = new Element();
            element.identity = child.name;
            element.parent = parent;
            element.instance = child.gameObject;
            element.Save();
            elements.Add(element);
            elements = SaveAllChildrenElements(child, elements);
        }
        return elements;
    }
}

[Serializable]
public class Element
{
    public string identity;
    public GameObject instance;
    public Transform parent;

    [HideInInspector] public int index = -1;
    //[HideInInspector] public bool active = true;
    [HideInInspector] public Vector2 pos;
    [HideInInspector] public Vector2 size = new Vector2(100, 100);
    [HideInInspector] public Vector2 pivot = new Vector2(.5f, .5f);
    [HideInInspector] public Vector2 minAnchor;
    [HideInInspector] public Vector2 maxAnchor;
    [HideInInspector] public Vector2 anchorPos;
    [HideInInspector] public Vector3 scale = new Vector3(1, 1, 1);
    [HideInInspector] public Quaternion rotation;

    internal void Save()
    {
        if (instance == null)
        {
            return;
        }

        //active = instance.activeSelf;
        index = instance.transform.GetSiblingIndex();

        pos = instance.transform.localPosition;
        RectTransform rect = instance.transform as RectTransform;
        size = rect.sizeDelta;
        pivot = rect.pivot;
        maxAnchor = rect.anchorMax;
        minAnchor = rect.anchorMin;
        anchorPos = rect.anchoredPosition;
        scale = instance.transform.localScale;
        rotation = instance.transform.rotation;
    }

    public void Apply()
    {
        if (!instance || !instance.scene.IsValid())
        {
            Debug.Log("Instance with identity " + identity + " is null or invalid");
            return;
        }
        if (index >= 0)
        {
            instance.transform.SetSiblingIndex(index);
        }

        RectTransform rect = instance.transform as RectTransform;
        rect.anchorMin = minAnchor;
        rect.anchorMax = maxAnchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        instance.transform.localPosition = pos;
        rect.anchoredPosition = anchorPos;
        instance.transform.localScale = scale;
        instance.transform.rotation = rotation;
        //instance.SetActive(active);
    }
}

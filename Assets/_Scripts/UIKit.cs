using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Utilidades para construir interfaz (UI) por codigo: lienzo, paneles, textos, botones y deslizadores.
/// Usa el sistema uGUI con la fuente integrada de Unity, asi no depende de ningun recurso extra.
/// </summary>
public static class UIKit
{
    static Font font;

    /// <summary>Fuente integrada de Unity usada en todos los textos.</summary>
    public static Font DefaultFont
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    /// <summary>Crea un lienzo (Canvas) que cubre toda la pantalla y escala con la resolucion.</summary>
    /// <param name="name">Nombre del objeto.</param>
    /// <param name="sortingOrder">Orden de dibujo: mayor = encima.</param>
    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>Crea el EventSystem si no existe (necesario para que los botones respondan).</summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    /// <summary>Panel de color que ocupa todo el espacio de su padre.</summary>
    public static RectTransform Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    /// <summary>Texto centrado en el punto indicado (relativo al centro del padre).</summary>
    public static Text Label(Transform parent, string text, int fontSize, Vector2 position, Vector2 size,
                             TextAnchor align = TextAnchor.MiddleCenter, Color? color = null)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var t = go.GetComponent<Text>();
        t.font = DefaultFont;
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color ?? Color.white;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Boton rectangular con texto. Se resalta en rojo al pasar el cursor.</summary>
    public static Button Button(Transform parent, string label, Vector2 position, Vector2 size, UnityAction onClick)
    {
        var go = new GameObject(label + " (Boton)", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.10f, 0.10f, 0.12f, 0.95f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.45f, 0.45f);
        colors.pressedColor = new Color(0.7f, 0.2f, 0.2f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
        btn.targetGraphic = img;
        if (onClick != null) btn.onClick.AddListener(onClick);

        var txt = Label(go.transform, label, 26, Vector2.zero, size);
        var trt = (RectTransform)txt.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return btn;
    }

    /// <summary>Deslizador horizontal de 0 a 1.</summary>
    public static Slider Slider(Transform parent, Vector2 position, Vector2 size, float value, UnityAction<float> onChange)
    {
        var root = new GameObject("Deslizador", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        var rrt = (RectTransform)root.transform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);
        rrt.anchoredPosition = position;
        rrt.sizeDelta = size;

        // fondo
        var bg = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var brt = (RectTransform)bg.transform;
        brt.anchorMin = new Vector2(0f, 0.35f); brt.anchorMax = new Vector2(1f, 0.65f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f);

        // relleno
        var fillArea = new GameObject("Area de relleno", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        var frt = (RectTransform)fillArea.transform;
        frt.anchorMin = new Vector2(0f, 0.35f); frt.anchorMax = new Vector2(1f, 0.65f);
        frt.offsetMin = new Vector2(5f, 0f); frt.offsetMax = new Vector2(-15f, 0f);

        var fill = new GameObject("Relleno", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = (RectTransform)fill.transform;
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;       // ocupa todo el alto del area, no 100 px
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.75f, 0.1f, 0.1f);

        // manija
        var handleArea = new GameObject("Area de manija", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        var hart = (RectTransform)handleArea.transform;
        hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one;
        hart.offsetMin = new Vector2(10f, 0f); hart.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Manija", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = (RectTransform)handle.transform;
        handleRt.anchorMin = new Vector2(0f, 0f); handleRt.anchorMax = new Vector2(0f, 1f);   // alto = alto del deslizador
        handleRt.sizeDelta = new Vector2(20f, 0f);
        var himg = handle.GetComponent<Image>();
        himg.color = Color.white;

        var s = root.GetComponent<Slider>();
        s.fillRect = (RectTransform)fill.transform;
        s.handleRect = (RectTransform)handle.transform;
        s.targetGraphic = himg;
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = value;
        if (onChange != null) s.onValueChanged.AddListener(onChange);
        return s;
    }
}

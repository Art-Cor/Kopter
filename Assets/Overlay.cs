using UnityEngine;
using UnityEngine.UI;

public class DebugDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public int fontSize = 20;
    public Color textColor = Color.white;
    public Vector2 screenPadding = new Vector2(10, 10);

    [Header("Custom Variables")]
    public string customLabel1 = "Custom1";
    public string customValue1;
    public string customLabel2 = "Custom2";
    public string customValue2;
    public string customLabel3 = "Custom3";
    public string customValue3;
    public string customLabel4 = "Custom4";
    public string customValue4;

    private Text displayText;
    private Vector3 position;
    private Vector3 rotation;

    void Start()
    {
        CreateDebugCanvas();
    }

    void Update()
    {
        string text = "";
        text += $"{customLabel1}: {customValue1}\n";
        text += $"{customLabel2}: {customValue2}\n";
        text += $"{customLabel3}: {customValue3}\n";
        text += $"{customLabel4}: {customValue4}\n";
        displayText.text = text;
    }

    void CreateDebugCanvas()
    {
        // Создаем Canvas
        GameObject canvasGO = new GameObject("DebugCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Высокий порядок для отображения поверх других элементов
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<GraphicRaycaster>();

        // Создаем текстовый объект
        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(canvasGO.transform);
        
        displayText = textGO.AddComponent<Text>();
        displayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        displayText.fontSize = fontSize;
        displayText.color = textColor;
        displayText.alignment = TextAnchor.UpperRight;
        displayText.horizontalOverflow = HorizontalWrapMode.Overflow;
        displayText.verticalOverflow = VerticalWrapMode.Overflow;

        // Настраиваем позицию в правом верхнем углу
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-screenPadding.x, -screenPadding.y);
    }
}
using UnityEngine;
using UnityEngine.UI;

public class PlayerBillboardHealthUI : MonoBehaviour
{
    [SerializeField] private Vector3 uiOffset = new Vector3(0f, 2.2f, 0f);
    private Transform _billboardRoot;
    private Slider _healthSlider;
    private Image _fillImage;
    private Text _nameText;
    private Camera _cachedCamera;

    public void SetupIfNeeded()
    {
        if (_billboardRoot != null)
        {
            return;
        }
        CreateUI();
    }

    private void CreateUI()
    {
        var root = new GameObject("BillboardUI");
        _billboardRoot = root.transform;
        _billboardRoot.SetParent(transform, false);
        _billboardRoot.localPosition = uiOffset;
        
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();
        
        var canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(180f, 55f);
        canvasRect.localScale = Vector3.one * 0.01f;
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(root.transform, false);
        
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, 0f);
        nameRect.sizeDelta = new Vector2(180f, 24f);
        _nameText = nameGo.AddComponent<Text>();
        _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _nameText.alignment = TextAnchor.MiddleCenter;
        _nameText.color = Color.white;
        _nameText.fontSize = 18;
        
        var sliderGo = new GameObject("HealthSlider");
        sliderGo.transform.SetParent(root.transform, false);
        
        var sliderRect = sliderGo.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0f);
        sliderRect.anchorMax = new Vector2(0.5f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 6f);
        sliderRect.sizeDelta = new Vector2(150f, 16f);
        
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);
        
        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        
        var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        _fillImage = fillGo.AddComponent<Image>();
        _fillImage.color = Color.green;
        _healthSlider = sliderGo.AddComponent<Slider>();
        _healthSlider.minValue = 0f;
        _healthSlider.maxValue = 1f;
        _healthSlider.value = 1f;
        _healthSlider.targetGraphic = _fillImage;
        _healthSlider.fillRect = fillRect;
        _healthSlider.direction = Slider.Direction.LeftToRight;
        _healthSlider.transition = Selectable.Transition.None;
        
        var handleSlideArea = new GameObject("Handle Slide Area");
        handleSlideArea.transform.SetParent(sliderGo.transform, false);
        
        var handleRect = handleSlideArea.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        _healthSlider.handleRect = null;
    }

    public void SetView(string playerName, float normalizedHp)
    {
        if (_billboardRoot == null)
        {
            return;
        }
        _nameText.text = playerName;
        _healthSlider.value = Mathf.Clamp01(normalizedHp);
        _fillImage.color = normalizedHp > 0.2f ? Color.green : Color.red;
    }

    private void LateUpdate()
    {
        if (_billboardRoot == null) { return; }
        if (_cachedCamera == null) { _cachedCamera = Camera.main; }
        if (_cachedCamera == null) { return; }
        _billboardRoot.forward = _cachedCamera.transform.forward;
    }
}

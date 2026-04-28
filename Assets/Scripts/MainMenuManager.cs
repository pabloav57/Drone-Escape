using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private const string DifficultyKey = "SelectedDifficulty";
    private const string ControlModeKey = "UseGyroscope";
    private const string SkinKey = "SelectedSkin";
    private const string BestScoreKey = "BestObstacleScore";
    private const string GameModeKey = "SelectedGameMode";
    private const string PursuitEnabledKey = "PursuitEnabled";
    private static MainMenu activeInstance;

    private static readonly List<string> GameModeNames = new List<string>
    {
        "Clasico",
        "Zen",
        "Rush"
    };

    private static readonly List<string> SkinNames = new List<string>
    {
        "Clasico",
        "Naranja",
        "Verde",
        "Azul",
        "Negro",
        "Blanco y Negro"
    };

    public Dropdown difficultyDropdown;
    public Dropdown skinDropdown;
    public Dropdown modeDropdown;
    public GameObject startMenu;
    public Text controlStatusText;
    public Text bestScoreText;
    public Text skinPreviewText;
    public Text modePreviewText;
    public Text volumeStatusText;
    public Slider volumeSlider;
    public Toggle muteToggle;
    public Image transitionOverlay;
    public float transitionDuration = 0.35f;
    public float menuIntroDuration = 0.55f;
    public float startButtonPulseAmount = 0.05f;
    public float startButtonPulseSpeed = 2.2f;
    public float previewFloatAmount = 4f;
    public float previewFloatSpeed = 1.8f;
    public Button startButton;

    private bool useGyroscope;
    private bool pursuitEnabled;
    private bool isTransitioning;
    private Vector3 startButtonBaseScale;
    private RectTransform[] floatingPreviewRects;
    private Vector2[] floatingPreviewBasePositions;
    private CanvasGroup startMenuCanvasGroup;
    private Font menuFont;

    void Awake()
    {
        DisableBlockingMenuShells();

        if (activeInstance != null && activeInstance != this)
        {
            enabled = false;
            return;
        }

        activeInstance = this;
    }

    void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    void Start()
    {
        if (activeInstance != this)
        {
            return;
        }

        EnsureEnvironmentStyler();

        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = PlayerPrefs.GetInt(DifficultyKey, 1);
        }

        SetupSkinDropdown();
        SetupModeDropdown();
        SetupAudioControls();

        useGyroscope = PlayerPrefs.GetInt(ControlModeKey, 0) == 1;
        pursuitEnabled = PlayerPrefs.GetInt(PursuitEnabledKey, PlayerPrefs.GetInt(GameModeKey, 0) == 3 ? 1 : 0) == 1;
        UpdateControlStatusText();
        UpdateBestScoreText();
        UpdateSkinPreviewText();
        ApplySelectedSkinPreview();
        UpdateModePreviewText();
        UpdateVolumeStatusText();

        SetupMenuCanvasGroup();
        ConfigureMainMenuPresentation();
        CacheFloatingPreviewTexts();

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
            startButtonBaseScale = startButton.transform.localScale;
        }

        SetOverlayAlpha(1f);
        StartCoroutine(IntroRoutine());
    }

    void Update()
    {
        AnimateStartButton();
        AnimatePreviewTexts();
    }

    public void StartGame()
    {
        if (isTransitioning)
        {
            return;
        }

        if (difficultyDropdown == null)
        {
            Debug.LogError("No hay un Dropdown de dificultad asignado.");
            return;
        }

        int difficultyIndex = difficultyDropdown.value;
        int selectedSkin = skinDropdown != null ? skinDropdown.value : PlayerPrefs.GetInt(SkinKey, 0);
        int selectedMode = modeDropdown != null ? modeDropdown.value : Mathf.Clamp(PlayerPrefs.GetInt(GameModeKey, 0), 0, GameModeNames.Count - 1);

        PlayerPrefs.SetInt(DifficultyKey, difficultyIndex);
        PlayerPrefs.SetInt(ControlModeKey, useGyroscope ? 1 : 0);
        PlayerPrefs.SetInt(SkinKey, selectedSkin);
        PlayerPrefs.SetInt(PursuitEnabledKey, pursuitEnabled ? 1 : 0);
        PlayerPrefs.SetInt(GameModeKey, pursuitEnabled ? 3 : selectedMode);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GameScene");
    }

    public void ToggleControlMode()
    {
        useGyroscope = !useGyroscope;
        PlayerPrefs.SetInt(ControlModeKey, useGyroscope ? 1 : 0);
        PlayerPrefs.Save();
        UpdateControlStatusText();
    }

    public void OnSkinChanged()
    {
        if (skinDropdown == null)
        {
            return;
        }

        PlayerPrefs.SetInt(SkinKey, skinDropdown.value);
        PlayerPrefs.Save();
        UpdateSkinPreviewText();
        ApplySelectedSkinPreview();
    }

    public void OnModeChanged()
    {
        if (modeDropdown == null)
        {
            return;
        }

        PlayerPrefs.SetInt(GameModeKey, modeDropdown.value);
        if (pursuitEnabled)
        {
            PlayerPrefs.SetInt(PursuitEnabledKey, 1);
        }

        PlayerPrefs.Save();
        UpdateModePreviewText();
    }

    public void TogglePursuitMode()
    {
        pursuitEnabled = !pursuitEnabled;
        PlayerPrefs.SetInt(PursuitEnabledKey, pursuitEnabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateModePreviewText();
        UpdatePursuitToggleButton();
    }

    public void OnVolumeChanged()
    {
        if (volumeSlider == null)
        {
            return;
        }

        GameAudioSettings.SetMasterVolume(volumeSlider.value);
        UpdateVolumeStatusText();
    }

    public void OnMuteChanged()
    {
        if (muteToggle == null)
        {
            return;
        }

        GameAudioSettings.SetMuted(muteToggle.isOn);
        UpdateVolumeStatusText();
    }

    private void CacheFloatingPreviewTexts()
    {
        List<RectTransform> rects = new List<RectTransform>();

        AddPreviewRect(rects, bestScoreText);
        AddPreviewRect(rects, skinPreviewText);
        AddPreviewRect(rects, modePreviewText);
        AddPreviewRect(rects, volumeStatusText);

        floatingPreviewRects = rects.ToArray();
        floatingPreviewBasePositions = new Vector2[floatingPreviewRects.Length];

        for (int i = 0; i < floatingPreviewRects.Length; i++)
        {
            floatingPreviewBasePositions[i] = floatingPreviewRects[i].anchoredPosition;
        }
    }

    private void AddPreviewRect(List<RectTransform> rects, Text textComponent)
    {
        if (textComponent != null)
        {
            RectTransform rect = textComponent.rectTransform;
            if (rect != null)
            {
                rects.Add(rect);
            }
        }
    }

    private void SetupSkinDropdown()
    {
        if (skinDropdown == null)
        {
            return;
        }

        int selectedSkin = Mathf.Clamp(PlayerPrefs.GetInt(SkinKey, 0), 0, SkinNames.Count - 1);

        if (skinDropdown.options.Count != SkinNames.Count)
        {
            skinDropdown.ClearOptions();
            skinDropdown.AddOptions(SkinNames);
        }

        skinDropdown.onValueChanged.RemoveListener(OnSkinDropdownValueChanged);
        skinDropdown.value = selectedSkin;
        skinDropdown.onValueChanged.AddListener(OnSkinDropdownValueChanged);
    }

    private void SetupModeDropdown()
    {
        if (modeDropdown == null)
        {
            return;
        }

        int savedMode = PlayerPrefs.GetInt(GameModeKey, 0);
        int selectedMode = Mathf.Clamp(savedMode == 3 ? 0 : savedMode, 0, GameModeNames.Count - 1);

        if (modeDropdown.options.Count != GameModeNames.Count)
        {
            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(GameModeNames);
        }

        modeDropdown.onValueChanged.RemoveListener(OnModeDropdownValueChanged);
        modeDropdown.value = selectedMode;
        modeDropdown.onValueChanged.AddListener(OnModeDropdownValueChanged);
    }

    private void SetupAudioControls()
    {
        GameAudioSettings.Apply();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = GameAudioSettings.GetMasterVolume();
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
        }

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.RemoveListener(OnMuteToggleValueChanged);
            muteToggle.isOn = GameAudioSettings.GetMuted();
            muteToggle.onValueChanged.AddListener(OnMuteToggleValueChanged);
        }
    }

    private void SetupMenuCanvasGroup()
    {
        if (startMenu == null)
        {
            return;
        }

        startMenuCanvasGroup = startMenu.GetComponent<CanvasGroup>();
        if (startMenuCanvasGroup == null)
        {
            startMenuCanvasGroup = startMenu.AddComponent<CanvasGroup>();
        }

        startMenuCanvasGroup.alpha = 0f;
        startMenuCanvasGroup.interactable = false;
        startMenuCanvasGroup.blocksRaycasts = false;
    }

    private void DisableBlockingMenuShells()
    {
        MainMenu[] menus = FindObjectsByType<MainMenu>(FindObjectsInactive.Include);
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] == null)
            {
                continue;
            }

            Image image = menus[i].GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 0f;
                image.color = color;
                image.raycastTarget = false;
            }

            LayoutGroup layoutGroup = menus[i].GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }
        }
    }

    private void EnsureEnvironmentStyler()
    {
        if (FindAnyObjectByType<EnvironmentStyler>() != null)
        {
            return;
        }

        gameObject.AddComponent<EnvironmentStyler>();
    }

    private void ConfigureMainMenuPresentation()
    {
        if (startMenu == null)
        {
            return;
        }

        menuFont = ResolveMenuFont();

        RectTransform menuRect = startMenu.GetComponent<RectTransform>();
        if (menuRect != null)
        {
            menuRect.anchorMin = Vector2.zero;
            menuRect.anchorMax = Vector2.one;
            menuRect.offsetMin = Vector2.zero;
            menuRect.offsetMax = Vector2.zero;
        }

        Image backdrop = GetOrCreateImage("MenuBackdrop", startMenu.transform);
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.SetAsFirstSibling();
        StretchToParent(backdropRect);
        backdrop.color = new Color(0.03f, 0.04f, 0.055f, 0.38f);
        backdrop.raycastTarget = false;

        Image panel = GetOrCreateImage("MenuPanel", startMenu.transform);
        RectTransform panelRect = panel.rectTransform;
        panelRect.SetSiblingIndex(1);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);
        panelRect.sizeDelta = new Vector2(820f, 660f);
        panel.color = new Color(0.035f, 0.045f, 0.065f, 0.82f);
        panel.raycastTarget = false;

        Text title = GetOrCreateText("MenuTitle", startMenu.transform);
        title.text = "Drone Escape";
        title.fontSize = 56;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.94f, 0.98f, 1f, 1f);
        PositionRect(title.rectTransform, new Vector2(0f, 275f), new Vector2(680f, 74f));

        Text subtitle = GetOrCreateText("MenuSubtitle", startMenu.transform);
        subtitle.text = "Ajusta tu vuelo y supera la ciudad.";
        subtitle.fontSize = 22;
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.color = new Color(0.72f, 0.79f, 0.9f, 1f);
        PositionRect(subtitle.rectTransform, new Vector2(0f, 224f), new Vector2(680f, 42f));

        StyleDropdownRow(difficultyDropdown, "Dificultad", new Vector2(-110f, 120f));
        StyleDropdownRow(skinDropdown, "Skin", new Vector2(-110f, 32f));
        StyleDropdownRow(modeDropdown, "Modo", new Vector2(-110f, -56f));

        StylePreviewText(bestScoreText, new Vector2(250f, 120f), 24, new Color(1f, 0.88f, 0.48f, 1f));
        StylePreviewText(skinPreviewText, new Vector2(250f, 62f), 19, new Color(0.72f, 0.88f, 1f, 1f));
        StylePreviewText(modePreviewText, new Vector2(250f, 12f), 19, new Color(0.72f, 0.88f, 1f, 1f));
        StylePreviewText(controlStatusText, new Vector2(250f, -38f), 18, new Color(0.72f, 0.79f, 0.9f, 1f));
        StylePreviewText(volumeStatusText, new Vector2(250f, -88f), 17, new Color(0.72f, 0.79f, 0.9f, 1f));

        StylePursuitToggleButton();
        StyleControlToggleButton();
        StyleStartButton();
        HideLegacyMenuText();
    }

    private void StyleDropdownRow(Dropdown dropdown, string label, Vector2 dropdownPosition)
    {
        if (dropdown == null)
        {
            return;
        }

        Text rowLabel = GetOrCreateText(label + "Label", startMenu.transform);
        rowLabel.text = label.ToUpperInvariant();
        rowLabel.fontSize = 16;
        rowLabel.fontStyle = FontStyle.Bold;
        rowLabel.alignment = TextAnchor.MiddleLeft;
        rowLabel.color = new Color(0.72f, 0.79f, 0.9f, 1f);
        PositionRect(rowLabel.rectTransform, dropdownPosition + new Vector2(-156f, 0f), new Vector2(135f, 46f));

        RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();
        if (dropdownRect != null)
        {
            dropdownRect.SetParent(startMenu.transform, false);
            PositionRect(dropdownRect, dropdownPosition, new Vector2(250f, 54f));
        }

        Image dropdownImage = dropdown.GetComponent<Image>();
        if (dropdownImage != null)
        {
            dropdownImage.color = new Color(0.93f, 0.96f, 1f, 0.96f);
        }

        ColorBlock colors = dropdown.colors;
        colors.normalColor = new Color(0.93f, 0.96f, 1f, 0.96f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.78f, 0.84f, 0.92f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        dropdown.colors = colors;

        Text caption = dropdown.captionText;
        if (caption != null)
        {
            caption.font = menuFont;
            caption.fontSize = 24;
            caption.alignment = TextAnchor.MiddleLeft;
            caption.color = new Color(0.08f, 0.1f, 0.13f, 1f);
        }

        Text itemText = dropdown.itemText;
        if (itemText != null)
        {
            itemText.font = menuFont;
            itemText.fontSize = 20;
            itemText.color = new Color(0.08f, 0.1f, 0.13f, 1f);
        }
    }

    private void StylePreviewText(Text text, Vector2 position, int fontSize, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.font = menuFont;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.raycastTarget = false;
        PositionRect(text.rectTransform, position, new Vector2(270f, 44f));
    }

    private void StyleStartButton()
    {
        if (startButton == null)
        {
            return;
        }

        RectTransform buttonRect = startButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.SetParent(startMenu.transform, false);
            PositionRect(buttonRect, new Vector2(0f, -255f), new Vector2(330f, 76f));
        }

        Image buttonImage = startButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.27f, 0.78f, 0.55f, 1f);
        }

        ColorBlock colors = startButton.colors;
        colors.normalColor = new Color(0.27f, 0.78f, 0.55f, 1f);
        colors.highlightedColor = new Color(0.36f, 0.9f, 0.65f, 1f);
        colors.pressedColor = new Color(0.18f, 0.58f, 0.4f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        startButton.colors = colors;

        Text buttonText = startButton.GetComponentInChildren<Text>(true);
        if (buttonText != null)
        {
            buttonText.font = menuFont;
            buttonText.text = "EMPEZAR";
            buttonText.fontSize = 26;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
        }
    }

    private void StyleControlToggleButton()
    {
        Button controlButton = FindButtonByText("Giroscopio");
        if (controlButton == null)
        {
            return;
        }

        RectTransform buttonRect = controlButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.SetParent(startMenu.transform, false);
            PositionRect(buttonRect, new Vector2(250f, -140f), new Vector2(220f, 42f));
        }

        Image buttonImage = controlButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.16f, 0.22f, 0.32f, 0.95f);
        }

        ColorBlock colors = controlButton.colors;
        colors.normalColor = new Color(0.16f, 0.22f, 0.32f, 0.95f);
        colors.highlightedColor = new Color(0.23f, 0.32f, 0.46f, 1f);
        colors.pressedColor = new Color(0.1f, 0.15f, 0.22f, 1f);
        colors.selectedColor = colors.highlightedColor;
        controlButton.colors = colors;

        Text buttonText = controlButton.GetComponentInChildren<Text>(true);
        if (buttonText != null)
        {
            buttonText.font = menuFont;
            buttonText.text = "Cambiar control";
            buttonText.fontSize = 15;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = new Color(0.9f, 0.96f, 1f, 1f);
        }
    }

    private void StylePursuitToggleButton()
    {
        Button pursuitButton = GetOrCreateButton("PursuitToggleButton", startMenu.transform);
        pursuitButton.onClick.RemoveListener(TogglePursuitMode);
        pursuitButton.onClick.AddListener(TogglePursuitMode);

        RectTransform buttonRect = pursuitButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            PositionRect(buttonRect, new Vector2(-110f, -145f), new Vector2(250f, 48f));
        }

        UpdatePursuitToggleButton();
    }

    private void UpdatePursuitToggleButton()
    {
        if (startMenu == null)
        {
            return;
        }

        Transform pursuitTransform = startMenu.transform.Find("PursuitToggleButton");
        Button pursuitButton = pursuitTransform != null ? pursuitTransform.GetComponent<Button>() : null;
        if (pursuitButton == null)
        {
            return;
        }

        Color buttonColor = pursuitEnabled
            ? new Color(0.85f, 0.22f, 0.24f, 1f)
            : new Color(0.16f, 0.22f, 0.32f, 0.95f);

        Image buttonImage = pursuitButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = buttonColor;
        }

        ColorBlock colors = pursuitButton.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(buttonColor, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        pursuitButton.colors = colors;

        Text buttonText = pursuitButton.GetComponentInChildren<Text>(true);
        if (buttonText != null)
        {
            buttonText.font = menuFont;
            buttonText.text = pursuitEnabled ? "Persecucion: SI" : "Persecucion: NO";
            buttonText.fontSize = 18;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
        }
    }

    private Button GetOrCreateButton(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        Button button = existing != null ? existing.GetComponent<Button>() : null;
        if (button != null)
        {
            return button;
        }

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        StretchToParent(textObject.GetComponent<RectTransform>());

        return buttonObject.GetComponent<Button>();
    }

    private Button FindButtonByText(string textValue)
    {
        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || texts[i].text != textValue)
            {
                continue;
            }

            Button button = texts[i].GetComponentInParent<Button>(true);
            if (button != null)
            {
                return button;
            }
        }

        return null;
    }

    private void HideLegacyMenuText()
    {
        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
            {
                continue;
            }

            if (texts[i].text == "Escoge el nivel de dificultad" ||
                texts[i].text == "Giroscopio Desactivado.")
            {
                texts[i].gameObject.SetActive(false);
            }
        }

        TextMeshProUGUI[] tmpTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        for (int i = 0; i < tmpTexts.Length; i++)
        {
            if (tmpTexts[i] == null)
            {
                continue;
            }

            if (tmpTexts[i].text == "Escoge el nivel de dificultad" ||
                tmpTexts[i].text == "Giroscopio Desactivado.")
            {
                tmpTexts[i].gameObject.SetActive(false);
            }
        }
    }

    private Image GetOrCreateImage(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            Image existingImage = existing.GetComponent<Image>();
            if (existingImage != null)
            {
                return existingImage;
            }
        }

        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private Text GetOrCreateText(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        Text text = existing != null ? existing.GetComponent<Text>() : null;
        if (text == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<Text>();
        }

        text.font = menuFont;
        text.raycastTarget = false;
        return text;
    }

    private void PositionRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Font ResolveMenuFont()
    {
        if (controlStatusText != null && controlStatusText.font != null)
        {
            return controlStatusText.font;
        }

        if (bestScoreText != null && bestScoreText.font != null)
        {
            return bestScoreText.font;
        }

        Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return builtinFont != null ? builtinFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void UpdateControlStatusText()
    {
        if (controlStatusText == null)
        {
            return;
        }

        if (useGyroscope && SystemInfo.supportsGyroscope)
        {
            controlStatusText.text = "Control: Giroscopio";
        }
        else if (useGyroscope)
        {
            controlStatusText.text = "Control: Teclado (sin giroscopio)";
        }
        else
        {
            controlStatusText.text = "Control: Teclado";
        }
    }

    private void UpdateBestScoreText()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = "Record: " + PlayerPrefs.GetInt(BestScoreKey, 0);
        }
    }

    private void UpdateSkinPreviewText()
    {
        if (skinPreviewText != null)
        {
            int selectedSkin = skinDropdown != null ? skinDropdown.value : PlayerPrefs.GetInt(SkinKey, 0);
            selectedSkin = Mathf.Clamp(selectedSkin, 0, SkinNames.Count - 1);
            skinPreviewText.text = "Skin: " + SkinNames[selectedSkin];
        }
    }

    private void ApplySelectedSkinPreview()
    {
        int selectedSkin = skinDropdown != null ? skinDropdown.value : PlayerPrefs.GetInt(SkinKey, 0);
        selectedSkin = Mathf.Clamp(selectedSkin, 0, SkinNames.Count - 1);

        DroneSkinController[] skinControllers = FindObjectsByType<DroneSkinController>(FindObjectsInactive.Include);
        for (int i = 0; i < skinControllers.Length; i++)
        {
            if (skinControllers[i] != null)
            {
                skinControllers[i].ApplySkin(selectedSkin);
            }
        }

        GameObject previewDrone = GameObject.Find("Drone Primary");
        if (previewDrone != null && previewDrone.GetComponent<DroneSkinController>() == null)
        {
            previewDrone.AddComponent<DroneSkinController>().ApplySkin(selectedSkin);
        }
    }

    private void UpdateModePreviewText()
    {
        if (modePreviewText != null)
        {
            int selectedMode = modeDropdown != null ? modeDropdown.value : PlayerPrefs.GetInt(GameModeKey, 0);
            selectedMode = Mathf.Clamp(selectedMode, 0, GameModeNames.Count - 1);
            modePreviewText.text = pursuitEnabled ? "Modo: Persecucion" : "Modo: " + GameModeNames[selectedMode];
        }
    }

    private void UpdateVolumeStatusText()
    {
        if (volumeStatusText == null)
        {
            return;
        }

        if (GameAudioSettings.GetMuted())
        {
            volumeStatusText.text = "Audio: Silenciado";
            return;
        }

        int volumePercent = Mathf.RoundToInt(GameAudioSettings.GetMasterVolume() * 100f);
        volumeStatusText.text = "Audio: " + volumePercent + "%";
    }

    private void OnSkinDropdownValueChanged(int value)
    {
        OnSkinChanged();
    }

    private void OnModeDropdownValueChanged(int value)
    {
        OnModeChanged();
    }

    private void OnVolumeSliderValueChanged(float value)
    {
        OnVolumeChanged();
    }

    private void OnMuteToggleValueChanged(bool value)
    {
        OnMuteChanged();
    }

    private IEnumerator IntroRoutine()
    {
        float elapsed = 0f;

        while (elapsed < menuIntroDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / menuIntroDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            SetOverlayAlpha(1f - eased);
            if (startMenuCanvasGroup != null)
            {
                startMenuCanvasGroup.alpha = eased;
            }

            yield return null;
        }

        SetOverlayAlpha(0f);
        if (startMenuCanvasGroup != null)
        {
            startMenuCanvasGroup.alpha = 1f;
            startMenuCanvasGroup.interactable = true;
            startMenuCanvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        isTransitioning = true;

        if (startMenuCanvasGroup != null)
        {
            startMenuCanvasGroup.interactable = false;
            startMenuCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            if (startMenuCanvasGroup != null)
            {
                startMenuCanvasGroup.alpha = 1f - eased;
            }

            SetOverlayAlpha(eased);
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (transitionOverlay == null)
        {
            return;
        }

        transitionOverlay.gameObject.SetActive(alpha > 0.001f);
        Color color = transitionOverlay.color;
        color.a = alpha;
        transitionOverlay.color = color;
    }

    private void AnimateStartButton()
    {
        if (startButton == null || isTransitioning)
        {
            return;
        }

        float pulse = 1f + (Mathf.Sin(Time.unscaledTime * startButtonPulseSpeed) * startButtonPulseAmount);
        startButton.transform.localScale = startButtonBaseScale * pulse;
    }

    private void AnimatePreviewTexts()
    {
        if (floatingPreviewRects == null)
        {
            return;
        }

        for (int i = 0; i < floatingPreviewRects.Length; i++)
        {
            if (floatingPreviewRects[i] == null)
            {
                continue;
            }

            float offset = Mathf.Sin((Time.unscaledTime * previewFloatSpeed) + i) * previewFloatAmount;
            floatingPreviewRects[i].anchoredPosition = floatingPreviewBasePositions[i] + new Vector2(0f, offset);
        }
    }
}

public static class GameAudioSettings
{
    private const string MasterVolumeKey = "MasterVolume";
    private const string MutedKey = "Muted";

    public static float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
    }

    public static bool GetMuted()
    {
        return PlayerPrefs.GetInt(MutedKey, 0) == 1;
    }

    public static void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
        Apply();
    }

    public static void SetMuted(bool value)
    {
        PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    public static void Apply()
    {
        AudioListener.volume = GetMuted() ? 0f : GetMasterVolume();
    }
}

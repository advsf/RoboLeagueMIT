using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class HandleSettings : MonoBehaviour
{
    public static HandleSettings instance;

    [Header("General Reference")]
    [SerializeField] private GameObject settingUI;
    [SerializeField] private GameObject gameplayMenuSetting;
    [SerializeField] private GameObject screenMenuSetting;
    [SerializeField] private GameObject controlMenuSetting;
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private Volume postVolume;

    [Header("Sub-Menu Reference")]
    [SerializeField] private GameObject videoSubMenu;
    [SerializeField] private GameObject graphicsSubMenu;
    [SerializeField] private GameObject audioSubMenu;

    [Header("Sens Reference")]
    [SerializeField] private Slider sensSlider;
    [SerializeField] private TMP_InputField sensInputField;

    [Header("Mouse Control Reference")]
    [SerializeField] private Toggle invertVerticalToggle;
    [SerializeField] private Toggle invertHorizontalToggle;

    [Header("Enable HUD Reference")]
    [SerializeField] private Toggle enableHudToggle;

    [Header("Camera Distance Reference")]
    [SerializeField] private Slider cameraDistanceSlider;
    [SerializeField] private TMP_InputField cameraDistanceInputField;

    [Header("Camera Y Offset Reference")]
    [SerializeField] private Slider cameraYOffsetSlider;
    [SerializeField] private TMP_InputField cameraYOffsetInputField;

    [Header("Enable Ball Dot Reference")]
    [SerializeField] private Toggle enableBallDotToggle;

    [Header("Ball Dot Min. Size")]
    [SerializeField] private Slider ballDotMinSizeSlider;
    [SerializeField] private TMP_InputField ballDotMinSizeInputField;

    [Header("Ball Dot Max Size")]
    [SerializeField] private Slider ballDotMaxSizeSlider;
    [SerializeField] private TMP_InputField ballDotMaxSizeInputField;

    [Header("Volume Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider playerVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider emoteVolumeSlider;
    [SerializeField] private Slider crowdVolumeSlider;

    [Header("Resolution Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    private Resolution[] resolutions;
    private int currentResolutionIndex = 0;

    [Header("Fullscreen Settings")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private TextMeshProUGUI fullscreenModeText;
    private int currentFullscreenModeIndex = 0; // 0 = Exclusive Fullscreen, 1 = Fullscreen Window, 2 = Maximized Window, 3 = Windowed

    [Header("Vsync Settings")]
    [SerializeField] private Toggle vSyncToggle;

    [Header("Limit FPS Settings")]
    [SerializeField] private TextMeshProUGUI limitFPSText;
    private int currentFPSSettings = 0;
    private int[] fpsOptions = { 30, 60, 144, 240, -1 };
    private int fpsIndex = 2; // default to 144 (index 2)

    [Header("Quality Settings")]
    [SerializeField] private TextMeshProUGUI qualitySettingText;
    private int currentQualitySettings = 0; // 0 - fancy, 1 - performative, 2 - balanced

    [Header("Anti-Aliasing Setting")]
    [SerializeField] private TextMeshProUGUI antiAliasingText;
    private int currentAntiAliasingSettings; // 0 = off, 1 = 2x, 2 = 4x, 3 = 8x

    [Header("Motion Blur Settings")]
    [SerializeField] private Toggle motionBlurToggle;
    [SerializeField] private float motionBlurIntensity = 0.5f;
    private MotionBlur motionBlur;

    [Header("Tonemapping Settings")]
    [SerializeField] private TextMeshProUGUI tonemappingText;
    private int currentTonemappingSetting; // 0 = none, 1 = neutral, 2 = ACES
    private Tonemapping tonemapping;

    public static event Action<bool> OnHUDToggled;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            CloseSettingsUI();
    }

    private void Awake()
    {
        // to be able to use FBPP for steam cloud save
        var config = new FBPPConfig()
        {
            SaveFileName = "roboLeague-save-file.txt",
            AutoSaveData = false,
            ScrambleSaveData = true,
            EncryptionSecret = "my-secret",
            SaveFilePath = Application.persistentDataPath
        };

        // pass it to FBPP
        FBPP.Start(config);
    }

    private void Start()
    {
        if (instance == null)
            instance = this;

        settingUI.SetActive(true);
        screenMenuSetting.SetActive(true);
        controlMenuSetting.SetActive(true);

        videoSubMenu.SetActive(true);
        graphicsSubMenu.SetActive(true);
        audioSubMenu.SetActive(true);

        // get all resolutions
        resolutions = Screen.resolutions.Distinct().ToArray();
        PopulateResolutionDropdown();

        // get all the post processing
        GetAllPostProcessing();

        // create or initialize data
        if (!FBPP.HasKey("IsDefaultCreated"))
            CreateDefaultSettings();
        else
            InitializeGameplaySettings();

        TurnOnSubVideoMenu();
        TurnOnGameplayMenu();
        settingUI.SetActive(false);
    }

    private void CreateDefaultSettings()
    {
        // create sensSlider value
        sensSlider.value = 200;
        UpdateSensitivityThroughSlider();

        // create mouse invert control values
        invertVerticalToggle.isOn = false;
        invertHorizontalToggle.isOn = false;
        InvertVerticalMouse();
        InvertHorizontalMouse();

        // create hub control values
        enableHudToggle.isOn = true;
        EnableHud(true);

        // create camera distance values
        cameraDistanceSlider.value = 12;
        UpdateCameraDistanceThroughSlider();

        // create camera y offset values
        cameraYOffsetSlider.value = 3f;
        UpdateCameraYOffsetThroughSlider();

        // create ball dot setting
        enableBallDotToggle.isOn = true;
        EnableBallDot();

        // create ball dot size setting
        ballDotMinSizeSlider.value = 0.05f;
        UpdateBallMinSizeThroughSlider();

        ballDotMaxSizeSlider.value = 0.4f;
        UpdateBallMaxSizeThroughSlider();

        // create volume control values
        masterVolumeSlider.value = 1;
        playerVolumeSlider.value = 1;
        musicVolumeSlider.value = 1;
        emoteVolumeSlider.value = 1;
        crowdVolumeSlider.value = 1;
        UpdateMasterVolume();
        UpdatePlayerVolume();
        UpdateMusicVolume();
        UpdateEmoteVolume();
        UpdateCrowdVolume();

        // resolutions
        currentResolutionIndex = resolutions.Length - 1;
        ApplyResolution();

        // fullscreen
        fullScreenToggle.isOn = true;
        UpdateFullscreen();

        // fullscreen mode
        PlayerPrefs.SetInt("FullscreenMode", 0);
        UpdateFullscreenMode();

        // vsync 
        vSyncToggle.isOn = false;
        UpdateVsync();

        // fps limit
        int refreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);

        fpsIndex = Array.IndexOf(fpsOptions, refreshRate);

        if (fpsIndex == -1)
        {
            int closest = fpsOptions
                .Where(fps => fps != -1)
                .OrderBy(fps => Mathf.Abs(fps - refreshRate))
                .First();
            fpsIndex = Array.IndexOf(fpsOptions, closest);
        }

        UpdateFPSSettings();

        // quality setting
        currentQualitySettings = 0;
        UpdateQuality();

        // anti aliasing
        currentAntiAliasingSettings = 0;
        UpdateAntiAliasing();

        // motion blur
        motionBlurToggle.isOn = false;
        UpdateMotionBlur();

        // tonemapping
        currentTonemappingSetting = 2;
        UpdateTonemappingSetting();

        PlayerPrefs.Save();

        FBPP.Save();
        FBPP.SetInt("IsDefaultCreated", 1);
    }

    private void InitializeGameplaySettings()
    {
        // sens UI
        sensSlider.value = FBPP.GetFloat("Sensitivity");
        sensInputField.text = FBPP.GetFloat("Sensitivity").ToString("F2");

        // mouse invert UI
        invertVerticalToggle.isOn = FBPP.GetInt("InvertVerticalMouse") == -1;
        invertHorizontalToggle.isOn = FBPP.GetInt("InvertHorizontalMouse") == -1;

        // hud enable UI
        enableHudToggle.isOn = FBPP.GetInt("EnableHud") == 1;
        EnableHud(enableHudToggle.isOn);

        // camera distance
        cameraDistanceSlider.value = FBPP.GetFloat("CameraDistance");
        cameraDistanceInputField.text = FBPP.GetFloat("CameraDistance").ToString("F2");

        // camera y offset
        cameraYOffsetSlider.value = FBPP.GetFloat("CameraYOffset");
        cameraYOffsetInputField.text = FBPP.GetFloat("CameraYOffset").ToString("F2");

        // ball dot size setting
        ballDotMinSizeSlider.value = FBPP.GetFloat("BallDotMinSize");
        UpdateBallMinSizeThroughSlider();

        ballDotMaxSizeSlider.value = FBPP.GetFloat("BallDotMaxSize");
        UpdateBallMaxSizeThroughSlider();

        // volume
        masterVolumeSlider.value = FBPP.GetFloat("MasterVolume");
        playerVolumeSlider.value = FBPP.GetFloat("PlayerVolume");
        musicVolumeSlider.value = FBPP.GetFloat("MusicVolume");
        emoteVolumeSlider.value = FBPP.GetFloat("EmoteVolume");
        crowdVolumeSlider.value = FBPP.GetFloat("CrowdVolume");
        UpdateMasterVolume();
        UpdatePlayerVolume();
        UpdateMusicVolume();
        UpdateEmoteVolume();
        UpdateCrowdVolume();

        // resolution
        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex");
        ApplyResolution();

        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // fullscreen
        fullScreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen") == 0;
        UpdateFullscreen();

        // fullscreen mode
        currentFullscreenModeIndex = PlayerPrefs.GetInt("FullscreenMode");
        UpdateFullscreenMode();

        // vsync
        vSyncToggle.isOn = PlayerPrefs.GetInt("VSync") == 1;
        UpdateVsync();

        // fps
        int savedFPS = PlayerPrefs.GetInt("FPS", 60); // default to 60
        fpsIndex = Array.IndexOf(fpsOptions, savedFPS);

        if (fpsIndex == -1)
            fpsIndex = 2;

        UpdateFPSSettings();

        // quality
        currentQualitySettings = FBPP.GetInt("Quality");
        UpdateQuality();

        // anti aliasing setting
        currentAntiAliasingSettings = FBPP.GetInt("AntiAliasing");
        UpdateAntiAliasing();

        // motion blur
        motionBlurToggle.isOn = FBPP.GetInt("MotionBlur") == 1;
        UpdateMotionBlur();

        // tonemapping
        currentTonemappingSetting = FBPP.GetInt("Tonemapping");
        UpdateTonemappingSetting();
    }

    private void GetAllPostProcessing()
    {
        postVolume.profile.TryGet(out motionBlur);
        postVolume.profile.TryGet(out tonemapping);
    }

    #region Sensitivity Settings
    public void UpdateSensitivityThroughSlider()
    {
        FBPP.SetFloat("Sensitivity", sensSlider.value);
        FBPP.Save();

        sensInputField.text = sensSlider.value.ToString("F2");
    }

    public void UpdateSensitivityThroughInputField()
    {
        if (float.TryParse(sensInputField.text.ToString(), out float sens))
        {
            FBPP.SetFloat("Sensitivity", sens);
            FBPP.Save();

            sensSlider.value = sens;
        }
    }

    #endregion

    #region Mouse Settings

    public void InvertVerticalMouse()
    {
        FBPP.SetInt("InvertVerticalMouse", invertVerticalToggle.isOn ? -1 : 1);
        FBPP.Save();
    }

    public void InvertHorizontalMouse()
    {
        FBPP.SetInt("InvertHorizontalMouse", invertHorizontalToggle.isOn ? -1 : 1);
        FBPP.Save();
    }

    #endregion

    #region Hud Settings

    public void EnableHud(bool isEnabled)
    {
        FBPP.SetInt("EnableHud", isEnabled ? 1 : 0);
        FBPP.Save();

        OnHUDToggled?.Invoke(isEnabled);
    }

    #endregion

    #region Camera Distance Settings

    public void UpdateCameraDistanceThroughSlider()
    {
        FBPP.SetFloat("CameraDistance", cameraDistanceSlider.value);
        FBPP.Save();

        cameraDistanceInputField.text = cameraDistanceSlider.value.ToString("F2");
    }

    public void UpdateCameraDistanceThroughInputField()
    {
        if (float.TryParse(cameraDistanceInputField.text.ToString(), out float distance))
        {
            FBPP.SetFloat("CameraDistance", distance);
            FBPP.Save();

            cameraDistanceSlider.value = distance;
        }
    }

    #endregion

    #region Camera Distance Settings

    public void UpdateCameraYOffsetThroughSlider()
    {
        FBPP.SetFloat("CameraYOffset", cameraYOffsetSlider.value);
        FBPP.Save();

        cameraYOffsetInputField.text = cameraYOffsetSlider.value.ToString("F2");
    }

    public void UpdateCameraYOffsetThroughInputField()
    {
        if (float.TryParse(cameraYOffsetInputField.text.ToString(), out float offset))
        {
            FBPP.SetFloat("CameraYOffset", offset);
            FBPP.Save();

            cameraYOffsetSlider.value = offset;
        }
    }

    #endregion

    #region Ball Dot Setting (Toggle)

    public void EnableBallDot()
    {
        FBPP.SetInt("EnableBallDot", enableBallDotToggle.isOn ? 1 : -1);
        FBPP.Save();
    }

    #endregion

    #region Ball Dot Min. Setting
    public void UpdateBallMinSizeThroughSlider()
    {
        FBPP.SetFloat("BallDotMinSize", ballDotMinSizeSlider.value);
        FBPP.Save();

        ballDotMinSizeInputField.text = ballDotMinSizeSlider.value.ToString("F2");
    }

    public void UpdateBallMinSizeThroughInputField()
    {
        if (float.TryParse(ballDotMinSizeInputField.text.ToString(), out float minSize))
        {
            minSize = Mathf.Clamp(minSize, ballDotMinSizeSlider.minValue, ballDotMinSizeSlider.maxValue);

            FBPP.SetFloat("BallDotMinSize", minSize);
            FBPP.Save();

            ballDotMinSizeSlider.value = minSize;
        }
    }

    #endregion

    #region Ball Dot Max Setting
    public void UpdateBallMaxSizeThroughSlider()
    {
        FBPP.SetFloat("BallDotMaxSize", ballDotMaxSizeSlider.value);
        FBPP.Save();

        ballDotMaxSizeInputField.text = ballDotMaxSizeSlider.value.ToString("F2");
    }

    public void UpdateBallMaxSizeThroughInputField()
    {
        if (float.TryParse(ballDotMaxSizeInputField.text.ToString(), out float maxSize))
        {
            maxSize = Mathf.Clamp(maxSize, ballDotMaxSizeSlider.minValue, ballDotMaxSizeSlider.maxValue);

            FBPP.SetFloat("BallDotMaxSize", maxSize);
            FBPP.Save();

            ballDotMaxSizeSlider.value = maxSize;
        }
    }

    #endregion

    #region Volume Settings

    public void UpdateMasterVolume()
    {
        float volume = masterVolumeSlider.value;

        FBPP.SetFloat("MasterVolume", volume);
        FBPP.Save();

        masterMixer.SetFloat("Master", Mathf.Log10(volume) * 20);
    }

    public void UpdatePlayerVolume()
    {
        float volume = playerVolumeSlider.value;

        FBPP.SetFloat("PlayerVolume", volume);
        FBPP.Save();

        masterMixer.SetFloat("Player", Mathf.Log10(volume) * 20);
    }

    public void UpdateMusicVolume()
    {
        float volume = musicVolumeSlider.value;

        FBPP.SetFloat("MusicVolume", volume);
        FBPP.Save();

        masterMixer.SetFloat("Music", Mathf.Log10(volume) * 20);
    }

    public void UpdateEmoteVolume()
    {
        float volume = emoteVolumeSlider.value;

        FBPP.SetFloat("EmoteVolume", volume);
        FBPP.Save();

        masterMixer.SetFloat("Emote", Mathf.Log10(volume) * 20);
    }

    public void UpdateCrowdVolume()
    {
        float volume = crowdVolumeSlider.value;

        FBPP.SetFloat("CrowdVolume", volume);
        FBPP.Save();

        masterMixer.SetFloat("Crowd", Mathf.Log10(volume) * 20);
    }

    #endregion

    #region Resolution Settings

    public void SetResolution(int resolutionIndex)
    {
        currentResolutionIndex = resolutionIndex;
    }

    private void ApplyResolution()
    {
        Resolution res = resolutions[currentResolutionIndex];

        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);

        Debug.Log(currentResolutionIndex);

        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);
        PlayerPrefs.Save();
    }

    private void PopulateResolutionDropdown()
    {
        resolutions = Screen.resolutions.Distinct().ToArray();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            int refreshRate = Mathf.RoundToInt((float)resolutions[i].refreshRateRatio.value);
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + refreshRate + "Hz";
            options.Add(option);

            Resolution screenRes = Screen.currentResolution;
            int screenRefresh = Mathf.RoundToInt((float)screenRes.refreshRateRatio.value);
            if (resolutions[i].width == screenRes.width &&
                resolutions[i].height == screenRes.height &&
                refreshRate == screenRefresh)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    #endregion

    #region Fullscreen Settings

    public void UpdateFullscreen()
    {
        // 0 = full screen, 1 = not fullscreen
        Screen.fullScreen = fullScreenToggle.isOn;
        PlayerPrefs.SetInt("Fullscreen", fullScreenToggle.isOn ? 0 : 1);
        PlayerPrefs.Save();
    }

    public void UpdateFullscreenMode()
    {
        // handle the text
        switch (currentFullscreenModeIndex)
        {
            case 0:
                fullscreenModeText.text = "Exclusive Fullscreen";
                break;
            case 1:
                fullscreenModeText.text = "Fullscreen Window";
                break;
            case 2:
                fullscreenModeText.text = "Maximized Window";
                break;
            case 3:
                fullscreenModeText.text = "Windowed";
                break;
        }

        Screen.fullScreenMode = (FullScreenMode)currentFullscreenModeIndex;
        PlayerPrefs.SetInt("FullscreenMode", currentFullscreenModeIndex);
        PlayerPrefs.Save();
    }

    public void GoToNextFullscreenMode()
    {
        currentFullscreenModeIndex++;

        if (currentFullscreenModeIndex > 3)
            currentFullscreenModeIndex = 0;

        UpdateFullscreenMode();
    }

    public void GoToPreviousFullscreenMode()
    {
        currentFullscreenModeIndex--;

        if (currentFullscreenModeIndex < 0)
            currentFullscreenModeIndex = 3;

        UpdateFullscreenMode();
    }

    #endregion

    #region Vsync Settings

    public void UpdateVsync()
    {
        PlayerPrefs.SetInt("VSync", vSyncToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();

        // 0 = off, 1 = on
        QualitySettings.vSyncCount = vSyncToggle.isOn ? 1 : 0;
    }

    #endregion

    #region Limit FPS Settings

    public void UpdateFPSSettings()
    {
        currentFPSSettings = fpsOptions[fpsIndex];

        if (currentFPSSettings == -1)
            limitFPSText.text = "Unlimited";
        else
            limitFPSText.text = currentFPSSettings.ToString();

        Application.targetFrameRate = currentFPSSettings;

        PlayerPrefs.SetInt("FPS", currentFPSSettings);
        PlayerPrefs.Save();
    }

    public void GoNextFPSLimit()
    {
        fpsIndex++;

        if (fpsIndex >= fpsOptions.Length)
            fpsIndex = 0;

        UpdateFPSSettings();
    }

    public void GoBackFPSLimit()
    {
        fpsIndex--;

        if (fpsIndex < 0)
            fpsIndex = fpsOptions.Length - 1;

        UpdateFPSSettings();
    }

    #endregion

    #region Quality Settings

    private void UpdateQuality()
    {
        QualitySettings.SetQualityLevel(currentQualitySettings);

        qualitySettingText.text = QualitySettings.names[currentQualitySettings];

        FBPP.SetInt("Quality", currentQualitySettings);
        FBPP.Save();
    }

    public void GoToNextQuality()
    {
        currentQualitySettings++;

        if (currentQualitySettings > 2)
            currentQualitySettings = 0;

        UpdateQuality();
    }

    public void GoToPreviousQuality()
    {
        currentQualitySettings--;

        if (currentQualitySettings < 0)
            currentQualitySettings = 2;

        UpdateQuality();
    }

    #endregion

    #region Anti-Aliasing Setting

    private void UpdateAntiAliasing()
    {
        switch (currentAntiAliasingSettings)
        {
            case 0: // off
                QualitySettings.antiAliasing = 0;
                antiAliasingText.text = "Off";
                break;
            case 1: // 2x
                QualitySettings.antiAliasing = 2;
                antiAliasingText.text = "2x";
                break;
            case 2: // 4x
                QualitySettings.antiAliasing = 4;
                antiAliasingText.text = "4x";
                break;
            case 3: // 8x
                QualitySettings.antiAliasing = 8;
                antiAliasingText.text = "8x";
                break;
        }

        PlayerPrefs.SetInt("AntiAliasing", currentAntiAliasingSettings);
        PlayerPrefs.Save();
    }

    public void GoToNextAntiAliasingSetting()
    {
        currentAntiAliasingSettings++;

        if (currentAntiAliasingSettings > 3)
            currentAntiAliasingSettings = 0;

        UpdateAntiAliasing();
    }

    public void GoToPreviousAntiAliasingSetting()
    {
        currentAntiAliasingSettings--;

        if (currentAntiAliasingSettings < 0)
            currentAntiAliasingSettings = 3;

        UpdateAntiAliasing();
    }

    #endregion

    #region Motion Blur Settings

    public void UpdateMotionBlur()
    {
        // 0 = off, 1 = on
        if (motionBlurToggle.isOn)
        {
            motionBlur.active = true;
            motionBlur.intensity.value = motionBlurIntensity;

            FBPP.SetInt("MotionBlur", 1);
            FBPP.Save();
        }

        else
        {
            motionBlur.active = false;
            motionBlur.intensity.value = 0;

            FBPP.SetInt("MotionBlur", 0);
            FBPP.Save();
        }
    }

    #endregion

    #region Tonemapping Settings

    private void UpdateTonemappingSetting()
    {
        switch (currentTonemappingSetting)
        {
            case 0: // none
                tonemapping.mode.value = TonemappingMode.None;
                tonemappingText.text = "None";
                break;
            case 1: // default
                tonemapping.mode.value = TonemappingMode.Neutral;
                tonemappingText.text = "Neutral";
                break;
            case 2: // ACES
                tonemapping.mode.value = TonemappingMode.ACES;
                tonemappingText.text = "ACES";
                break;
        }

        FBPP.SetInt("Tonemapping", currentTonemappingSetting);
        FBPP.Save();
    }

    public void GoToNextTonemappingSetting()
    {
        currentTonemappingSetting++;

        if (currentTonemappingSetting > 2)
            currentTonemappingSetting = 0;

        UpdateTonemappingSetting();
    }

    public void GoToPreviousTonemappingSetting()
    {
        currentTonemappingSetting--;

        if (currentTonemappingSetting < 0)
            currentTonemappingSetting = 2;

        UpdateTonemappingSetting();
    }

    #endregion 

    #region General Functions
    public void OpenSettingsUI()
    {
        settingUI.SetActive(true);
    }

    public void CloseSettingsUI()
    {
        settingUI.SetActive(false);
    }

    public void TurnOnGameplayMenu()
    {
        gameplayMenuSetting.SetActive(true);
        screenMenuSetting.SetActive(false);
        controlMenuSetting.SetActive(false);
    }

    public void TurnOnScreenMenu()
    {
        screenMenuSetting.SetActive(true);
        gameplayMenuSetting.SetActive(false);
        controlMenuSetting.SetActive(false);
    }

    public void TurnOnControlMenu()
    {
        controlMenuSetting.SetActive(true);
        screenMenuSetting.SetActive(false);
        gameplayMenuSetting.SetActive(false);
    }

    public void TurnOnSubVideoMenu()
    {
        videoSubMenu.SetActive(true);
        graphicsSubMenu.SetActive(false);
        audioSubMenu.SetActive(false);
    }

    public void TurnOnSubGraphicsMenu()
    {
        graphicsSubMenu.SetActive(true);
        videoSubMenu.SetActive(false);
        audioSubMenu.SetActive(false);
    }

    public void TurnOnSubAudioMenu()
    {
        audioSubMenu.SetActive(true);
        graphicsSubMenu.SetActive(false);
        videoSubMenu.SetActive(false);
    }

    public void ResetAllSettings()
    {
        float rankXP = FBPP.GetFloat("RankXP");
        int rankIndex = FBPP.GetInt("RankIndex");
        int goals = FBPP.GetInt("Goals");
        int assists = FBPP.GetInt("Assists");
        int saves = FBPP.GetInt("Saves");

        FBPP.DeleteAll();
        PlayerPrefs.DeleteAll();

        CreateDefaultSettings();
        InitializeGameplaySettings();

        FBPP.SetFloat("RankXP", rankXP);
        FBPP.SetInt("RankIndex", rankIndex);
        FBPP.SetInt("Goals", goals);
        FBPP.SetInt("Assists", assists);
        FBPP.SetInt("Saves", saves);

        FBPP.Save();
    }
    #endregion
}
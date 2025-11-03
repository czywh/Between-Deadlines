using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{

    [Header("UI References")]
    public Slider volumeSlider;
    public TMP_Dropdown qualityDropdown;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // load saved settings 
        float savedVol = PlayerPrefs.GetFloat("Volume", 1f);
        int savedQuality = PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());

        // apply the settings
        AudioListener.volume = savedVol;
        QualitySettings.SetQualityLevel(savedQuality);

        // sync UI with saved settings 
        volumeSlider.value = savedVol;
        qualityDropdown.value = savedQuality;

        volumeSlider.onValueChanged.AddListener(SetVolume);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
    }

    // called when volume slider adjusted 
    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }

    // called when quality dropdown selection changes 
    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt("Quality", index);
        PlayerPrefs.Save();
    }
}

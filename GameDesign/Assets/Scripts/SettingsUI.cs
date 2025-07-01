using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    private void OnEnable()
    {
        var settings = SettingsManager.GetInstance();

        volumeSlider.value = settings.Volume;
        fullscreenToggle.isOn = settings.IsFullscreen;

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }


    private void OnDisable()
    {
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
    }

    private void OnVolumeChanged(float value)
    {
        SettingsManager.Instance.SetVolume(value);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        SettingsManager.Instance.SetFullscreen(isFullscreen);
    }
}

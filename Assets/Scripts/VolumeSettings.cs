using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;

    [Header("Sliders (Collega qui i tuoi UI Slider)")]
    [SerializeField] private Slider masterSlider; // NOVITÀ: Controlla tutto assieme!
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // Carica i volumi salvati, oppure imposta a metà (0.5) di default
        if (PlayerPrefs.HasKey("musicVolume"))
        {
            LoadVolume();
        }
        else
        {
            SetMasterVolume();
            SetMusicVolume();
            SetSFXVolume();
        }
    }

    public void SetMasterVolume()
    {
        if (masterSlider != null)
        {
            float volume = masterSlider.value;
            myMixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
            PlayerPrefs.SetFloat("masterVolume", volume);
        }
    }

    public void SetMusicVolume()
    {
        if (musicSlider != null)
        {
            float volume = musicSlider.value;
            myMixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
            PlayerPrefs.SetFloat("musicVolume", volume);
        }
    }

    public void SetSFXVolume()
    {
        if (sfxSlider != null)
        {
            float volume = sfxSlider.value;
            myMixer.SetFloat("SfxVol", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
            PlayerPrefs.SetFloat("sfxVolume", volume);
        }
    }

    private void LoadVolume()
    {
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("masterVolume", 1f);
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("musicVolume", 1f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("sfxVolume", 1f);

        SetMasterVolume();
        SetMusicVolume();
        SetSFXVolume();
    }
}
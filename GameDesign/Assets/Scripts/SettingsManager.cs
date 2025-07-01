using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public float Volume { get; private set; } = 1f;
    public bool IsFullscreen { get; private set; } = true;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static SettingsManager GetInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("SettingsManager");
            Instance = go.AddComponent<SettingsManager>();
            Instance.LoadSettings();
            DontDestroyOnLoad(go);
        }
        return Instance;
    }


    public void LoadSettings()
    {
        Volume = PlayerPrefs.GetFloat("Volume", 1f);
        IsFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        ApplySettings();
    }

    public void ApplySettings()
    {
        AudioListener.volume = Volume;
        Screen.fullScreen = IsFullscreen;
    }

    public void SetVolume(float volume)
    {
        Volume = volume;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        IsFullscreen = fullscreen;
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}

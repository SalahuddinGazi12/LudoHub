using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private const string SoundPrefKey = "SoundEnabled";
    private bool isSoundEnabled;

    public Button soundToggleButton; // Assign your sound on/off button in the Inspector
    public Sprite soundOnIcon; // Assign an "on" icon in the Inspector
    public Sprite soundOffIcon; // Assign an "off" icon in the Inspector
    public AudioSource[] soundEffects; // Assign all sound effects in the scene

    private void Start()
    {
        // Load saved sound preference
        isSoundEnabled = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;

        // Apply the saved preference
        UpdateSoundState();

        // Add button click listener
        if (soundToggleButton != null)
        {
            soundToggleButton.onClick.AddListener(ToggleSound);
            UpdateButtonIcon();
        }
    }

    private void UpdateSoundState()
    {
        // Mute or unmute all sound effects
        foreach (AudioSource audio in soundEffects)
        {
            audio.mute = !isSoundEnabled;
        }
    }

    private void ToggleSound()
    {
        // Toggle the sound state
        isSoundEnabled = !isSoundEnabled;

        // Save the preference
        PlayerPrefs.SetInt(SoundPrefKey, isSoundEnabled ? 1 : 0);
        PlayerPrefs.Save();

        // Apply the new state
        UpdateSoundState();
        UpdateButtonIcon();
    }

    private void UpdateButtonIcon()
    {
        // Update the button's icon based on the current sound state
        if (soundToggleButton != null)
        {
            Image buttonImage = soundToggleButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = isSoundEnabled ? soundOnIcon : soundOffIcon;
            }
        }
    }
}

using UnityEngine;

public class StaticAudioPlayer : MonoBehaviour
{
    // make a singleton
    public static StaticAudioPlayer Instance { get; private set; }

    private AudioSource prevSound;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlaySound(AudioSource sound)
    {
        if (prevSound != null && prevSound.isPlaying) prevSound.Stop();
        if (sound != null)
        {
            prevSound = Instantiate<AudioSource>(sound);
            prevSound.Play();
            Destroy(prevSound.gameObject, prevSound.clip.length + 0.1f); // destroy .1 second later after playing
        }
    }

    public bool IsPlaying()
    {
        if (prevSound != null) return prevSound.isPlaying;
        return false;
    }

    public void StopPlaying()
    {
        if (prevSound != null && prevSound.isPlaying)
        {
            Destroy(prevSound.gameObject);
        }
    }
}

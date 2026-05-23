using UnityEngine;

public class EnemySound : MonoBehaviour
{
    public AudioSource soundPrefab;
    private AudioSource sound;

    public void Awake()
    {
        if (soundPrefab != null)
        {
            sound = Instantiate<AudioSource>(soundPrefab);
        }
    }

    public void PlaySound()
    {
        if (sound != null && !sound.isPlaying)
        {
            Debug.Log("PlaySound");
            sound.Play();
        }
    }

    public void StopSound()
    {
        if (sound != null && sound.isPlaying) sound.Stop();
    }
}

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MovementSoundController : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip currentClip;
    private float targetVolume;
    private float targetPitch = 1f;

    public AudioClip aSound;
    public AudioClip sSound;
    public AudioClip wSound;
    public AudioClip dSound;
    public float idleVolume = 0.05f;
    public float maxVolume = 0.65f;
    public float idlePitch = 0.9f;
    public float maxPitch = 1.2f;
    public float responseSpeed = 8f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
        audioSource.pitch = idlePitch;
    }

    void Update()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, responseSpeed * Time.deltaTime);
        audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, responseSpeed * Time.deltaTime);

        if (audioSource.isPlaying && audioSource.volume < 0.02f)
        {
            audioSource.Stop();
            currentClip = null;
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        if (currentClip != clip)
        {
            currentClip = clip;
            audioSource.clip = clip;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void SetMovementState(float intensity, bool boosting)
    {
        intensity = Mathf.Clamp01(intensity);
        targetVolume = Mathf.Lerp(idleVolume, maxVolume, intensity);
        targetPitch = Mathf.Lerp(idlePitch, maxPitch + (boosting ? 0.08f : 0f), intensity);
    }

    public void StopMovementSounds()
    {
        targetVolume = 0f;
        targetPitch = idlePitch;
    }
}

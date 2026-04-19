using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SpaceSoundController : MonoBehaviour
{
    private AudioSource audioSource;
    private float targetVolume;
    private float targetPitch = 1f;

    public AudioClip spaceSound;
    public float activeVolume = 0.55f;
    public float idlePitch = 0.95f;
    public float boostPitch = 1.15f;
    public float responseSpeed = 10f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
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

        if (audioSource.isPlaying && audioSource.volume < 0.02f && Mathf.Approximately(targetVolume, 0f))
        {
            audioSource.Stop();
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.clip = clip;
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void SetBoostActive(bool isActive)
    {
        targetVolume = isActive ? activeVolume : 0f;
        targetPitch = isActive ? boostPitch : idlePitch;

        if (isActive)
        {
            PlaySound(spaceSound);
        }
    }

    public void StopSpaceSound()
    {
        targetVolume = 0f;
        targetPitch = idlePitch;
    }
}

using UnityEngine;

public class MovementSoundController : MonoBehaviour
{
    private AudioSource audioSource; // Referencia al componente AudioSource

    public AudioClip aSound;     // Sonido para la tecla A
    public AudioClip sSound;     // Sonido para la tecla S
    public AudioClip wSound;     // Sonido para la tecla W
    public AudioClip dSound;     // Sonido para la tecla D

    void Start()
    {
        // Obtener el componente AudioSource del GameObject
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Detectar teclas específicas (ASDW) y reproducir sonido mientras estén presionadas

        if (Input.GetKey(KeyCode.A))
        {
            PlaySound(aSound); // Reproducir sonido de A
        }
        if (Input.GetKey(KeyCode.S))
        {
            PlaySound(sSound); // Reproducir sonido de S
        }
        if (Input.GetKey(KeyCode.W))
        {
            PlaySound(wSound); // Reproducir sonido de W
        }
        if (Input.GetKey(KeyCode.D))
        {
            PlaySound(dSound); // Reproducir sonido de D
        }
    }

    // Método para reproducir un sonido específico
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            if (!audioSource.isPlaying) // Reproducir solo si no está ya reproduciéndose
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }

    // Método para detener el sonido de las teclas ASDW
    public void StopMovementSounds()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}

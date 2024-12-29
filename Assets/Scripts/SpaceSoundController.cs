using UnityEngine;

public class SpaceSoundController : MonoBehaviour
{
    private AudioSource audioSource; // Referencia al componente AudioSource

    public AudioClip spaceSound; // Sonido para la tecla Espacio
    private bool spaceSoundPlayed = false; // Para asegurar que el sonido de espacio solo se reproduce una vez

    void Start()
    {
        // Obtener el componente AudioSource del GameObject
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Si se pulsa la tecla Espacio y no se ha reproducido el sonido aún
        if (Input.GetKeyDown(KeyCode.Space) && !spaceSoundPlayed)
        {
            PlaySound(spaceSound); // Reproducir sonido de Espacio
            spaceSoundPlayed = true; // Marcar que ya se ha reproducido el sonido de espacio
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
                audioSource.loop = true; // Hacer el sonido de espacio en bucle
                audioSource.Play();
            }
        }
    }

    // Método para detener el sonido de la tecla Espacio
    public void StopSpaceSound()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            spaceSoundPlayed = false; // Reiniciar el estado del sonido de espacio
        }
    }
}

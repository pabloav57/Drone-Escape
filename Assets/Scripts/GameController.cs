using UnityEngine;
using TMPro; // Necesario para TextMeshPro
using UnityEngine.SceneManagement; // Necesario para cargar la escena de nuevo

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI gameOverText; // Referencia al TextMeshProUGUI para mostrar el mensaje de fin de juego

    private bool isGameOver = false;   // Para controlar si el juego ha terminado

    // Start is called before the first frame update
    void Start()
    {
        // Asegúrate de que el mensaje esté oculto al principio
        gameOverText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameOver)
        {
            // Si el juego ha terminado, mostramos el mensaje y esperamos que el jugador presione espacio para reiniciar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RestartGame(); // Reiniciar el juego
            }
        }
    }

    // Método que se llama cuando el dron colisiona, se llama desde DroneController
    public void EndGame()
    {
        // Pausar el juego
        Time.timeScale = 0f;

        // Mostrar el mensaje en pantalla
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "Game Over! \nPress Space to Restart";  // Cambia el texto

        // Establecer que el juego ha terminado
        isGameOver = true;
    }

    // Método para reiniciar el juego
    void RestartGame()
    {
        // Restablecer el tiempo de juego
        Time.timeScale = 1f;

        // Reiniciar el juego cargando la misma escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

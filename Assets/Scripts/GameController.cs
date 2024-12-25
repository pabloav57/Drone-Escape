using UnityEngine;
using UnityEngine.UI; // Para botones y paneles de UI
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public GameObject gameOverMenu; // Panel del menú de Game Over

    private bool isGameOver = false;

    void Start()
    {
        // Nos aseguramos de que el menú está oculto al inicio
        gameOverMenu.SetActive(false);
    }

    void Update()
    {
        if (isGameOver)
        {
            // El menú se muestra, no hacemos nada más
            return;
        }
    }

    // Método que se llama al perder
    public void EndGame()
    {
    // Mostrar el menú de fin de juego
    gameOverMenu.SetActive(true);

    // Pausar el tiempo
    Time.timeScale = 0f;

    // Establecer que el juego ha terminado
    isGameOver = true;
}

    // Método para reiniciar el juego
    public void RestartGame()
{
    // Restablecer el tiempo antes de cargar la escena
    Time.timeScale = 1f;

    // Reiniciar el juego cargando la misma escena
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Método para salir al menú principal
    public void QuitToMainMenu()
    {
        // Aquí deberías cargar tu escena de menú principal
        SceneManager.LoadScene("MainMenu"); // Cambia "MainMenu" por el nombre de tu escena de menú
    }
}

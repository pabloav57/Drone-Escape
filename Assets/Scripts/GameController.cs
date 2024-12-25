using UnityEngine;
using TMPro;
using UnityEngine.UI;  // Importante para trabajar con botones

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI gameOverText;  // Referencia al texto de Game Over (y Pausa)
    public TextMeshProUGUI timerText;     // Referencia al texto del cronómetro
    public Button restartButton;          // Referencia al botón de reiniciar
    public Button exitButton;             // Referencia al botón de salir
    public Button resumeButton;           // Referencia al botón de reanudar
    public GameObject gameOverMenu;       // Referencia al panel del menú (Game Over y Pausa)

    private float elapsedTime = 0f;       // Tiempo transcurrido
    private bool isGameOver = false;      // Estado del juego
    private bool isPaused = false;        // Estado de la pausa

    void Start()
    {
        // Asegurarse de que todo esté desactivado al principio
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
        
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
        
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
        
        if (resumeButton != null)
            resumeButton.gameObject.SetActive(false);

        if (gameOverMenu != null)
            gameOverMenu.SetActive(false); // Asegurarse de que el panel de Game Over/Pausa esté desactivado

        timerText.text = "Tiempo: 0.00s"; // Inicializa el cronómetro

        // Configura los eventos para los botones
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
    }

    void Update()
    {
        if (!isGameOver && !isPaused)
        {
            // Incrementar el tiempo transcurrido
            elapsedTime += Time.deltaTime;
            timerText.text = "Tiempo: " + elapsedTime.ToString("F2") + "s"; // Actualizar texto
        }

        // Detectar la tecla ESC para mostrar el menú de pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGameOver)
            {
                // Si el juego terminó, mostrar las opciones de reiniciar o salir
                ShowGameOverMenu();
            }
            else if (!isPaused)
            {
                // Si no está en pausa, pausar el juego
                ShowPauseMenu();
            }
            else
            {
                // Si está en pausa, reanudar el juego
                ResumeGame();
            }
        }

        // Detectar si se presiona la tecla Espacio para reiniciar el juego en Game Over
        if (isGameOver && Input.GetKeyDown(KeyCode.Space))
        {
            RestartGame();
        }
    }

    public void EndGame()
    {
        isGameOver = true; // Detener el cronómetro
        Time.timeScale = 0f;

        // Mostrar el menú de Game Over
        ShowGameOverMenu();
    }

    void ShowGameOverMenu()
    {
        // Activar el panel de Game Over
        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);

        // Activar el texto de Game Over
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);  // Asegúrate de que el texto esté activado
            gameOverText.text = "<size=60><color=red>Game Over!</color></size>\n\n" +
                                "Duración: " + elapsedTime.ToString("F2") + "s\n\n" +
                                "Presiona Espacio o haz clic en el botón para reiniciar el juego!";
        }

        // Activar los botones de Game Over
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
        }
    }

    void ShowPauseMenu()
    {
        // Pausar el tiempo
        Time.timeScale = 0f;
        isPaused = true;

        // Activar el panel de pausa (usando el menú de Game Over como pausa)
        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);

        // Actualizar el texto a "Pausa"
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "<size=60><color=yellow>Pausa</color></size>\n\n" +
                                "Duración: " + elapsedTime.ToString("F2") + "s\n\n" +
                                "Presiona ESC para reanudar o el botón de abajo!";
        }

        // Activar los botones de pausa
        if (resumeButton != null)
            resumeButton.gameObject.SetActive(true);

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    void ResumeGame()
    {
        // Reanudar el tiempo del juego
        Time.timeScale = 1f;
        isPaused = false;

        // Ocultar el menú de pausa
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);
    }

    void RestartGame()
    {
        // Reiniciar el juego
        Time.timeScale = 1f;
        elapsedTime = 0f; // Reiniciar el cronómetro
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    void ExitGame()
    {
        // Salir del juego
        Application.Quit();
    }
}

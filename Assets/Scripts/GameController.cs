using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    // Referencias al menú principal
    public GameObject startMenu;  // Panel del menú de inicio
    public TextMeshProUGUI startText;  // Texto para empezar el juego

    // Referencias al menú de Game Over y Pausa
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI timerText;
    public Button restartButton;
    public Button exitButton;
    public Button resumeButton;
    public GameObject gameOverMenu;

    private float elapsedTime = 0f;
    private bool isGameOver = false;
    private bool isPaused = true;  // Inicialmente, el juego está en pausa
    private float spawnInterval = 2f; // Ejemplo: intervalo de aparición de enemigos
    private float spawnTimer = 0f;   // Temporizador para controlar el spawn

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
            gameOverMenu.SetActive(false);

        if (startMenu != null)
            startMenu.SetActive(true);  // Mostrar el menú de inicio

        // Inicializar cronómetro
        timerText.text = "Tiempo: 0.00s";

        // Asegurarse de que el texto de "Pulsa espacio para empezar" esté visible
        if (startText != null)
            startText.gameObject.SetActive(true);

        // Pausar el juego inicialmente
        Time.timeScale = 0f;  // Esto pausa el juego antes de darle al botón de iniciar
    }

    void Update()
    {
        // Esperar a que el jugador presione la tecla espacio para comenzar el juego
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }

        if (!isGameOver && !isPaused)
        {
            // Incrementar el tiempo transcurrido
            elapsedTime += Time.deltaTime;
            timerText.text = "Tiempo: " + elapsedTime.ToString("F2") + "s"; // Actualizar texto

            // Incrementar el temporizador de spawn
            spawnTimer += Time.deltaTime;

            // Generar un objeto o enemigo cuando el temporizador supere el intervalo
            if (spawnTimer >= spawnInterval)
            {
                SpawnObject();
                spawnTimer = 0f; // Reiniciar el temporizador
            }
        }

        // Detectar la tecla ESC para mostrar el menú de pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGameOver)
            {
                ShowGameOverMenu();
            }
            else if (!isPaused)
            {
                ShowPauseMenu();
            }
            else
            {
                ResumeGame();
            }
        }

        if (isGameOver && Input.GetKeyDown(KeyCode.Space))
        {
            RestartGame();
        }
    }

    void SpawnObject()
    {
        // Aquí añades la lógica para generar enemigos u objetos
        Debug.Log("Objeto generado!");
    }

    public void StartGame()
    {
        // Iniciar el juego al presionar "Espacio"
        startMenu.SetActive(false);  // Ocultar el menú de inicio
        Time.timeScale = 1f;         // Reanudar el tiempo del juego
        isPaused = false;            // El juego ya no está en pausa

    }

    void SetDifficulty(int difficultyIndex)
    {
        switch (difficultyIndex)
        {
            case 0: // Fácil
                spawnInterval = 3f; // Ejemplo: Intervalo de spawn más largo
                break;
            case 1: // Normal
                spawnInterval = 2f;
                break;
            case 2: // Difícil
                spawnInterval = 1.5f;
                break;
            case 3: // Imposible
                spawnInterval = 1f;
                break;
        }
    }

    public void EndGame()
    {
        isGameOver = true; // Detener el cronómetro
        Time.timeScale = 0f;  // Pausar el juego

        ShowGameOverMenu();
    }

    void ShowGameOverMenu()
    {
        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "<size=60><color=red>Game Over!</color></size>\n\n" +
                                "Duración: " + elapsedTime.ToString("F2") + "s\n\n" +
                                "Presiona Espacio o haz clic en el botón para reiniciar el juego!";
        }

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
        Time.timeScale = 0f;
        isPaused = true;

        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "<size=60><color=yellow>Pausa</color></size>\n\n" +
                                "Duración: " + elapsedTime.ToString("F2") + "s\n\n" +
                                "Presiona ESC para reanudar o el botón de abajo!";
        }

        if (resumeButton != null)
            resumeButton.gameObject.SetActive(true);

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        elapsedTime = 0f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    void ExitGame()
    {
        Application.Quit();
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;  // Importante para trabajar con botones

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI gameOverText;  // Referencia al texto de Game Over
    public TextMeshProUGUI timerText;    // Referencia al texto del cronómetro
    public Button restartButton;         // Referencia al botón de reiniciar
    public Button exitButton;            // Referencia al botón de salir
    public GameObject gameOverMenu;      // Referencia al panel del menú de Game Over

    private float elapsedTime = 0f;      // Tiempo transcurrido
    private bool isGameOver = false;     // Estado del juego

    void Start()
    {
        // Asegurarse de que todo esté desactivado al principio
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);
        
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
        
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
        
        if (gameOverMenu != null)
            gameOverMenu.SetActive(false); // Asegurarse de que el panel de Game Over esté desactivado

        timerText.text = "Tiempo: 0.00s"; // Inicializa el cronómetro

        // Configura el evento para el botón de reiniciar
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        
        // Configura el evento para el botón de salir
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    void Update()
        {
        if (!isGameOver)
    {
        // Incrementar el tiempo transcurrido
        elapsedTime += Time.deltaTime;
        timerText.text = "Tiempo: " + elapsedTime.ToString("F2") + "s"; // Actualizar texto
    }
    else
    {
        // Detectar la tecla ESC para mostrar el menú
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowGameOverMenu();
        }

        // Detectar si se presiona la tecla Espacio para reiniciar el juego
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RestartGame();
        }
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
        gameOverText.text = "<size=60><color=red>Game Over!</size></color>\n\n" +
                            "Duración: " + elapsedTime.ToString("F2") + "s\n\n" +
                            "Presiona Espacio o haz clic en el botón para reiniciar el juego!";
        Debug.Log("GameOver text updated");
    }

    // Activar los botones
    if (restartButton != null)
    {
        restartButton.gameObject.SetActive(true);
        Debug.Log("Restart button activated");
    }

    if (exitButton != null)
    {
        exitButton.gameObject.SetActive(true);
        Debug.Log("Exit button activated");
    }
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

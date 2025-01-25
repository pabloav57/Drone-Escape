using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Dropdown difficultyDropdown;  // Dropdown de dificultad
    public GameObject startMenu;         // Menú de inicio
    public Text controlStatusText;       // Texto para mostrar el estado del control
    public Button startButton;           // Botón para empezar el juego

    private bool useGyroscope = false;   // Controla si se usa el giroscopio

    void Start()
    {
        // Asegurarse de que el botón "Empezar" está asociado con la función "StartGame"
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    public void StartGame()
    {
        // Obtiene el índice de la dificultad seleccionada
        int difficultyIndex = difficultyDropdown.value;
        PlayerPrefs.SetInt("SelectedDifficulty", difficultyIndex); // Guarda el índice seleccionado en PlayerPrefs

        // Ocultar el menú de inicio
        startMenu.SetActive(false);

        // Cargar la escena de la cuenta regresiva
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    public void ToggleControlMode()
    {
        // Cambiar entre giroscopio y teclado
        useGyroscope = !useGyroscope;
        UpdateControlStatusText();
    }

    private void UpdateControlStatusText()
    {
        if (useGyroscope && SystemInfo.supportsGyroscope)
        {
            controlStatusText.text = "Control: Giroscopio";
        }
        else
        {
            controlStatusText.text = "Control: Teclado";
        }
    }
}

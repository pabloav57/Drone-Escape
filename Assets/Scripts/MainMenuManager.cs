using UnityEngine;
using UnityEngine.UI; // Necesario para usar UI en Unity

public class MainMenu : MonoBehaviour
{
    public Dropdown difficultyDropdown; // Dropdown de dificultad
    public GameObject startMenu;        // Menú de inicio
    public Text controlStatusText;      // Texto para mostrar el estado del control

    private bool useGyroscope = false;  // Controla si se usa el giroscopio

    public void StartGame()
    {
        int difficultyIndex = difficultyDropdown.value; // Obtiene el índice de la dificultad
        PlayerPrefs.SetInt("SelectedDifficulty", difficultyIndex); // Guarda el índice seleccionado

        // Ocultar el menú de inicio
        startMenu.SetActive(false);

        // Cargar la escena del juego
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

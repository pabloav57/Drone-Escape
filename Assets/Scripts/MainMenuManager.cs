using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Dropdown difficultyDropdown; // Asigna tu Dropdown desde el Inspector
    public GameObject startMenu;

    public void StartGame()
    {
        // Guardar la dificultad seleccionada
        int difficultyIndex = difficultyDropdown.value; // Obtiene el índice seleccionado
        PlayerPrefs.SetInt("SelectedDifficulty", difficultyIndex); // Guarda el índice

        // Ocultar el menú de inicio
        startMenu.SetActive(false);

        // Cargar la escena del juego
        SceneManager.LoadScene("SampleScene");
    }
}

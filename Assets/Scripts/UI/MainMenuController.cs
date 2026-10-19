
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Controls")]
    [SerializeField] private Slider vanCountSlider;
    [SerializeField] private TMP_Text vanCountText;
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private void Start()
    {
        if (vanCountSlider != null)
        {
            vanCountSlider.minValue = 3;
            vanCountSlider.maxValue = 10;
            vanCountSlider.wholeNumbers = true;

            vanCountSlider.SetValueWithoutNotify(
                Mathf.Clamp(
                    PlayerPrefs.GetInt("MaxVans", 5), 3, 10
                )
            );

            vanCountSlider.onValueChanged.AddListener(
                UpdateVanCountText
            );

            UpdateVanCountText(vanCountSlider.value);
        }

        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify(
                Mathf.Clamp(
                    PlayerPrefs.GetInt("VanDifficulty", 0),
                    0,
                    2
                )
            );

            difficultyDropdown.RefreshShownValue();
        }
    }

    private void UpdateVanCountText(float value)
    {
        if (vanCountText != null)
            vanCountText.text =
                $"NUMBER OF VANS: {Mathf.RoundToInt(value)}";
    }

    public void PlayGame()
    {
        int vans = vanCountSlider != null
            ? Mathf.RoundToInt(vanCountSlider.value)
            : 5;

        int difficulty = difficultyDropdown != null
            ? difficultyDropdown.value
            : 0;

        PlayerPrefs.SetInt("MaxVans", Mathf.Clamp(vans, 3, 10));
        PlayerPrefs.SetInt(
            "VanDifficulty",
            Mathf.Clamp(difficulty, 0, 2)
        );
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnDestroy()
    {
        if (vanCountSlider != null)
            vanCountSlider.onValueChanged.RemoveListener(
                UpdateVanCountText
            );
    }
}

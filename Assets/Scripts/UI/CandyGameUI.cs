using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CandyGameUI : MonoBehaviour
{
    [Header("Gameplay")]
    [Min(1)] public int candyGoal = 30;
    public PlayerCandy playerCandy;
    public PlayerMovement playerMovement;

    [Header("HUD (TextMeshPro UI)")]
    public TMP_Text candyCounterText;
    public TMP_Text objectiveText;

    [Header("Victory Screen")]
    public GameObject victoryPanel;
    public TMP_Text victoryText;

    private bool hasWon;

    public bool HasWon => hasWon;
    public bool HasEnoughCandy => playerCandy != null && playerCandy.CandyCount >= candyGoal;

    private void Awake()
    {
        if (playerCandy == null)
            playerCandy = FindFirstObjectByType<PlayerCandy>();

        if (playerMovement == null && playerCandy != null)
            playerMovement = playerCandy.GetComponent<PlayerMovement>();

        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (playerCandy == null || hasWon) return;

        int current = playerCandy.CandyCount;
        if (candyCounterText != null)
            candyCounterText.text = $"Candy: {current} / {candyGoal}";

        if (objectiveText != null)
            objectiveText.text = current >= candyGoal
                ? "Enough candy! Reach the EXIT!"
                : $"Collect {candyGoal - current} more candy!";
    }

    // Called by the EscapeZone when the player enters with enough candy.
    public void Win()
    {
        if (hasWon || !HasEnoughCandy) return;
        hasWon = true;

        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        if (victoryText != null)
            victoryText.text = $"YOU ESCAPED!\nCandy collected: {playerCandy.CandyCount}";

        if (playerMovement != null)
            playerMovement.enabled = false;

        // Keep the win screen safe from continuing attacks.
        foreach (VanAI van in FindObjectsByType<VanAI>(FindObjectsSortMode.None))
            van.enabled = false;

        // Stop future vans from appearing.
        foreach (VanSpawner spawner in FindObjectsByType<VanSpawner>(FindObjectsSortMode.None))
            spawner.enabled = false;
    }

    // Assign this to the Restart button's On Click event.
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }    
}

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CandyEscapeZone : MonoBehaviour
{
    public CandyGameUI gameUI;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            Debug.LogWarning("CandyEscapeZone requires Is Trigger enabled on its collider.", this);

        if (gameUI == null)
            gameUI = FindFirstObjectByType<CandyGameUI>();
    }

    private void OnTriggerEnter(Collider other) => TryEscape(other);
    private void OnTriggerStay(Collider other) => TryEscape(other);

    private void TryEscape(Collider other)
    {
        if (gameUI == null || gameUI.HasWon) return;

        // The CharacterController belongs to the root Player object.
        PlayerCandy candy = other.GetComponentInParent<PlayerCandy>();
        if (candy == null || candy != gameUI.playerCandy) return;

        if (gameUI.HasEnoughCandy)
            gameUI.Win();
    }
}

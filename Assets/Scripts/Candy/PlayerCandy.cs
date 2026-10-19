using UnityEngine;

public class PlayerCandy : MonoBehaviour
{
    [SerializeField] private int startingCandy = 0;
    [SerializeField] private int candyLostPerHit = 5;
    [SerializeField] private float minimumImpactSpeed = 2f;
    [SerializeField] private float impactGraceSeconds = 1.5f;
    [SerializeField] private float knockbackSpeed = 9f;
    [SerializeField] private float knockbackDecay = 8f;
    [SerializeField] private CandyPickup droppedCandyPrefab;
    [SerializeField] private float scatterImpulse = 5f;

    public int CandyCount { get; private set; }
    private float protectedUntil;
    private Vector3 knockbackVelocity;
    private CharacterController characterController;
    private Rigidbody playerBody;

    private void Awake()
    {
        CandyCount = Mathf.Max(0, startingCandy);
        characterController = GetComponent<CharacterController>();
        playerBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (characterController != null && characterController.enabled)
            characterController.Move(knockbackVelocity * Time.deltaTime);
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
    }

    //public void AddCandy(int amount) => CandyCount = Mathf.Max(0, CandyCount + amount);
public void AddCandy(int amount)
{
    CandyCount = Mathf.Max(0, CandyCount + amount);

}
    public void ReceiveVanImpact(Vector3 contactPoint, float impactSpeed, Vector3 vanPosition)
    {
        if (Time.time < protectedUntil || impactSpeed < minimumImpactSpeed) return;
        protectedUntil = Time.time + impactGraceSeconds;
        Vector3 away = transform.position - vanPosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = transform.forward;
        away.Normalize();
        knockbackVelocity = away * knockbackSpeed + Vector3.up * 1.5f;
        if (playerBody != null && !playerBody.isKinematic)
            playerBody.AddForce(away * knockbackSpeed, ForceMode.VelocityChange);

        int lost = Mathf.Min(CandyCount, candyLostPerHit);
        CandyCount -= lost;
        if (droppedCandyPrefab == null) return;
        for (int i = 0; i < lost; i++)
        {
            Vector3 position = transform.position + Vector3.up * 1.1f + Random.insideUnitSphere * 0.2f;
            CandyPickup pickup = Instantiate(droppedCandyPrefab, position, Random.rotation);
            pickup.SetDropped(true);
            Rigidbody rb = pickup.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) + 0.45f;
                rb.AddForce(direction.normalized * scatterImpulse * Random.Range(0.75f, 1.25f), ForceMode.Impulse);
            }
        }
    }
}

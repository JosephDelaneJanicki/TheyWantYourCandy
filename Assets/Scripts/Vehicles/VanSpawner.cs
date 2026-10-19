
using System.Collections.Generic;
using UnityEngine;

public class VanSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VanAI vanPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawning")]
    [SerializeField] private float firstSpawnDelay = 20f;
    [SerializeField] private float timeBetweenSpawns = 30f;
    [SerializeField] private int maximumVans = 5;
    [SerializeField] private float minimumPlayerDistance = 18f;
    [SerializeField] private LayerMask blockingLayers = ~0;

    private readonly List<VanAI> activeVans = new List<VanAI>();
    private float nextSpawnTime;
    private int selectedDifficulty;

    private void Start()
    {
        if (player == null)
        {
            GameObject candidate =
                GameObject.FindGameObjectWithTag("Player");

            if (candidate != null)
                player = candidate.transform;
        }

        // Read the player's menu selections.
        maximumVans = Mathf.Clamp(
            PlayerPrefs.GetInt("MaxVans", maximumVans),
            3,
            10
        );

        selectedDifficulty = Mathf.Clamp(
            PlayerPrefs.GetInt("VanDifficulty", 0),
            0,
            2
        );

        nextSpawnTime = Time.time + firstSpawnDelay;

        Debug.Log(
            $"Van setup: {maximumVans} vans, " +
            $"difficulty {selectedDifficulty}"
        );
    }

    private void Update()
    {
        if (Time.time < nextSpawnTime ||
            vanPrefab == null ||
            player == null)
            return;

        activeVans.RemoveAll(v => v == null);

        if (activeVans.Count >= maximumVans)
            return;

        Transform spawn = ChooseSpawn();

        if (spawn == null)
        {
            nextSpawnTime = Time.time + 2f;
            return;
        }

        VanAI van = Instantiate(
            vanPrefab,
            spawn.position,
            spawn.rotation
        );

        // Apply difficulty to this van instance.
        van.ConfigureDifficulty(selectedDifficulty);

        activeVans.Add(van);

        nextSpawnTime =
            Time.time + Mathf.Max(1f, timeBetweenSpawns);
    }

    private Transform ChooseSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        List<Transform> valid = new List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            if (Vector3.Distance(
                point.position,
                player.position
            ) < minimumPlayerDistance)
                continue;

            if (Physics.CheckBox(
                point.position + Vector3.up * 0.85f,
                new Vector3(1.1f, 0.75f, 2.2f),
                point.rotation,
                blockingLayers,
                QueryTriggerInteraction.Ignore
            ))
                continue;

            valid.Add(point);
        }

        return valid.Count == 0
            ? null
            : valid[Random.Range(0, valid.Count)];
    }
}

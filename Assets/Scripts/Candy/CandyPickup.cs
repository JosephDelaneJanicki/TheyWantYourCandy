using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CandyPickup : MonoBehaviour
{
    [SerializeField] private int candyValue = 1;
    [SerializeField] private float droppedLifetime = 90f;
    [SerializeField] private float pickupDelay = 0.65f;
    [SerializeField] private int maxDroppedPickups = 100;

    private static readonly Queue<CandyPickup> droppedPickups = new Queue<CandyPickup>();
    private bool dropped;
    private bool collected = false;
    private float collectibleAt;

    private void Awake()
    {
        // Trigger detection is handled separately from the solid collider, if any.
        Collider[] colliders = GetComponents<Collider>();
        bool hasTrigger = false;
        foreach (Collider c in colliders) if (c.isTrigger) hasTrigger = true;
        if (!hasTrigger)
        {
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
        }
    }

    public void SetDropped(bool value)
    {
        dropped = value;
        collectibleAt = Time.time + pickupDelay;
        if (!dropped) return;
        droppedPickups.Enqueue(this);
        while (droppedPickups.Count > Mathf.Max(1, maxDroppedPickups))
        {
            CandyPickup oldest = droppedPickups.Dequeue();
            if (oldest != null) Destroy(oldest.gameObject);
        }
        Destroy(gameObject, droppedLifetime);
    }

    private void OnTriggerEnter(Collider other) => TryCollect(other);
    private void OnTriggerStay(Collider other) => TryCollect(other);

    private void TryCollect(Collider other)
    {
        if (collected) return;
        if (Time.time < collectibleAt) return;
        PlayerCandy inventory = other.GetComponentInParent<PlayerCandy>();
        if (inventory == null) return;
        collected = true;
        inventory.AddCandy(candyValue);
        Destroy(gameObject);
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class VanAI : MonoBehaviour
{
    private enum Mode { Pursue, Charge, Recover }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Header("Driving")]
    [SerializeField] private float pursuitAcceleration = 19f;
    [SerializeField] private float maxPursuitSpeed = 13f;
    [SerializeField] private float steeringAcceleration = 9f;
    [SerializeField] private float sidewaysGrip = 5f;
    [SerializeField] private float normalDrag = 0.3f;

    [Header("Charge")]
    [SerializeField] private float chargeStartDistance = 15f;
    [SerializeField] private float chargeFacingAngle = 13f;
    [SerializeField] private float chargeAcceleration = 38f;
    [SerializeField] private float maxChargeSpeed = 24f;
    [SerializeField] private float chargeDuration = 2.2f;
    [SerializeField] private float chargeSteeringMultiplier = 0.23f;

    [Header("Obstacles / Recovery")]
    [SerializeField] private float sensorLength = 7f;
    [SerializeField] private float sensorRadius = 0.6f;
    [SerializeField] private float crashSpeedThreshold = 7f;
    [SerializeField] private float recoverSeconds = 2.5f;
    [SerializeField] private float emergencyResetSeconds = 13f;
    [SerializeField] private float unstuckAfterSeconds = 3f;
    [SerializeField] private float reverseSeconds = 1.4f;
    [SerializeField] private float maxDistanceFromSpawn = 160f;
    [SerializeField] private float minimumY = -10f;


    public void ConfigureDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            case 1: // They REALLY Want Your Candy
                steeringAcceleration = 37f;
                chargeAcceleration = 48f;
                maxChargeSpeed = 32f;
                recoverSeconds = 1.9f;
                unstuckAfterSeconds= 2f;
                break;

            case 2: // Swiggity Swandy
                steeringAcceleration = 50f;
                chargeAcceleration = 61f;
                maxChargeSpeed = 40f;
                recoverSeconds = 1f;
                chargeStartDistance = 10f;
                chargeFacingAngle = 10f;
                unstuckAfterSeconds = 1.8f;
                break;
            
            case 3: // THEY'RE CRASHING OUT
                steeringAcceleration = 50f;
                chargeAcceleration = 10000f;
                maxChargeSpeed = 10000f;
                recoverSeconds = 0.5f;
                chargeStartDistance = 17f;
                chargeFacingAngle = 1f;
                unstuckAfterSeconds = 1.5f;
                maxPursuitSpeed = 22f;
                pursuitAcceleration = 25;
                break;


            default: // They Want Your Candy
                steeringAcceleration = 23f;
                chargeAcceleration = 38f;
                maxChargeSpeed = 24f;
                break;
        }
    }

    private Rigidbody body;
    private Collider[] ownColliders;
    private Mode mode = Mode.Pursue;
    private float stateTime;
    private float stalledTime;
    private float reverseUntil;
    private float flipTime;
    private float lastDistance = float.PositiveInfinity;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Vector3 chargeDirection;
    private float obstacleTurnPreference = 1f;
    private bool followingWall;
    private float wallSide = 1f;
    private Vector3 wallNormal = Vector3.back;
    private float wallFollowStart;
    private Vector3 stallPosition;
    private float stallCheckTime;

    [Header("Wall Following")]
    [SerializeField] private float wallClearanceBias = 0.55f;
    [SerializeField] private float minimumWallFollowSeconds = 1.2f;
    [SerializeField] private float obstacleProbeHeight = 0.8f;
    [SerializeField] private bool showAvoidanceDebug = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        ownColliders = GetComponentsInChildren<Collider>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearDamping = normalDrag;
        stallPosition = transform.position;
        stallCheckTime = Time.time;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject candidate = GameObject.FindGameObjectWithTag("Player");
            if (candidate != null) player = candidate.transform;
        }
        if (player == null) Debug.LogError($"{name}: No Player tag found; assign Player or tag the player 'Player'.", this);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        stateTime += dt;
        if (transform.position.y < minimumY || Vector3.Distance(transform.position, spawnPosition) > maxDistanceFromSpawn)
        {
            EmergencyReset();
            return;
        }

        float upright = Vector3.Dot(transform.up, Vector3.up);
        if (upright < 0.45f) flipTime += dt;
        else flipTime = 0f;
        if (mode != Mode.Recover && flipTime > 0.65f) EnterRecovery();

        if (mode == Mode.Recover)
        {
            Recover(dt, upright);
            return;
        }
        if (player == null) return;

        Vector3 toPlayer = Flat(player.position - transform.position);
        if (toPlayer.sqrMagnitude < 0.01f) return;
        Vector3 desired = toPlayer.normalized;

        if (mode == Mode.Charge)
        {
            Drive(chargeDirection, chargeAcceleration, maxChargeSpeed, chargeSteeringMultiplier, false);
            if (stateTime >= chargeDuration) EnterRecovery();
            return;
        }

        // Omniscient pursuit, but the vehicle still must navigate around geometry.
        Vector3 targetDirection = AvoidObstacles(desired, toPlayer.magnitude);
        float headingError = Vector3.Angle(Flat(transform.forward), desired);
        float distance = toPlayer.magnitude;
        bool unobstructed = !BlockedInDirection(desired, distance);

        // Keep applying throttle while steering. Do not stop moving just because the angle is large.
        bool reversing = Time.time < reverseUntil;
        Drive(targetDirection, reversing ? -pursuitAcceleration * 0.65f : pursuitAcceleration,
            maxPursuitSpeed, 1f, true);

        if (!reversing && !followingWall && distance <= chargeStartDistance && headingError <= chargeFacingAngle && unobstructed)
        {
            chargeDirection = desired;
            ChangeMode(Mode.Charge);
            return;
        }

        // Track actual displacement over a short interval, not only proximity to the player.
        // Wall following legitimately moves sideways, so distance-to-player alone is misleading.
        if (Time.time - stallCheckTime >= 0.75f)
        {
            float traveled = Flat(transform.position - stallPosition).magnitude;
            if (!reversing && traveled < 0.6f)
                stalledTime += Time.time - stallCheckTime;
            else if (!reversing)
                stalledTime = 0f;
            stallPosition = transform.position;
            stallCheckTime = Time.time;
        }
        if (stalledTime >= unstuckAfterSeconds)
        {
            reverseUntil = Time.time + reverseSeconds;
            wallSide = -wallSide;
            obstacleTurnPreference = wallSide;
            followingWall = false; // Reconsider the obstruction after backing up.
            stalledTime = 0f;
        }
    }

    private void Drive(Vector3 direction, float acceleration, float speedLimit, float steerScale, bool strongGrip)
    {
        direction = Flat(direction);
        if (direction.sqrMagnitude < 0.001f) return;
        direction.Normalize();
        Vector3 forward = Flat(transform.forward).normalized;
        float signedAngle = Vector3.SignedAngle(forward, direction, Vector3.up);
        float yawSpeed = Vector3.Dot(body.angularVelocity, Vector3.up);
        float steering = Mathf.Clamp(signedAngle / 40f, -1f, 1f);
        body.AddTorque(Vector3.up * ((steering * steeringAcceleration * steerScale) - yawSpeed * 3.2f), ForceMode.Acceleration);

        float forwardSpeed = Vector3.Dot(Flat(body.linearVelocity), forward);
        if (acceleration < 0f || forwardSpeed < speedLimit)
            body.AddForce(forward * acceleration, ForceMode.Acceleration);

        Vector3 sideways = Vector3.Project(Flat(body.linearVelocity), Flat(transform.right).normalized);
        body.AddForce(-sideways * sidewaysGrip * (strongGrip ? 1f : 0.35f), ForceMode.Acceleration);
    }

    // The obstacle sensor selects a wall-following direction and remembers it.
    // We do NOT steer straight back at the player as soon as the front ray clears a corner.
    private Vector3 AvoidObstacles(Vector3 desired, float playerDistance)
    {
        Vector3 forward = Flat(transform.forward).normalized;
        Vector3 origin = SensorOrigin();
        bool blockedAhead = TryObstacle(forward, sensorLength, out RaycastHit aheadHit);
        bool directPathBlocked = TryObstacle(desired, playerDistance, out RaycastHit directHit);

        if (!followingWall && (blockedAhead || directPathBlocked))
        {
            // Start avoiding only once the building is actually within sensor range.
            if (!blockedAhead && directHit.distance > sensorLength) return desired;

            RaycastHit hit = blockedAhead ? aheadHit : directHit;
            wallNormal = Flat(hit.normal).normalized;
            if (wallNormal.sqrMagnitude < 0.01f) wallNormal = -forward;

            // Choose whichever direction along the wall makes better initial progress,
            // favoring the side whose near-future corridor is more open.
            Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
            float positive = ClearDistance(tangent, sensorLength * 1.4f);
            float negative = ClearDistance(-tangent, sensorLength * 1.4f);
            float score = Vector3.Dot(tangent, desired) - Vector3.Dot(-tangent, desired);
            score += (positive - negative) / Mathf.Max(sensorLength, 0.1f);
            wallSide = Mathf.Abs(score) < 0.08f ? obstacleTurnPreference : (score >= 0f ? 1f : -1f);
            obstacleTurnPreference = wallSide;
            followingWall = true;
            wallFollowStart = Time.time;
        }

        if (!followingWall) return desired;

        // Once a corner is cleared, leave the wall only when there is a genuine clear
        // line toward the player AND we've committed long enough not to oscillate.
        if (!directPathBlocked && Time.time - wallFollowStart >= minimumWallFollowSeconds)
        {
            followingWall = false;
            return desired;
        }

        // Follow the wall's tangent. A slight outward bias keeps the van from scraping
        // it continuously. Update the normal as the front or player-facing probe hits it.
        if (blockedAhead)
        {
            Vector3 n = Flat(aheadHit.normal).normalized;
            if (n.sqrMagnitude > 0.1f) wallNormal = n;
        }
        else if (directPathBlocked && directHit.distance < sensorLength * 1.2f)
        {
            Vector3 n = Flat(directHit.normal).normalized;
            if (n.sqrMagnitude > 0.1f) wallNormal = n;
        }

        Vector3 followTangent = Vector3.Cross(Vector3.up, wallNormal).normalized * wallSide;
        Vector3 followDirection = (followTangent + wallNormal * wallClearanceBias).normalized;
        if (showAvoidanceDebug)
        {
            Debug.DrawRay(origin, followDirection * sensorLength, Color.cyan);
            Debug.DrawRay(origin, desired * Mathf.Min(playerDistance, sensorLength), Color.red);
        }
        return followDirection;
    }

    private Vector3 SensorOrigin()
    {
        return transform.position + Vector3.up * obstacleProbeHeight
            + Flat(transform.forward).normalized * 1.2f;
    }

    private float ClearDistance(Vector3 direction, float length)
    {
        return TryObstacle(direction, length, out RaycastHit hit) ? hit.distance : length;
    }

    private bool BlockedInDirection(Vector3 direction, float distance)
    {
        return TryObstacle(direction, distance, out _);
    }

    private bool TryObstacle(Vector3 direction, float distance, out RaycastHit nearestHit)
    {
        nearestHit = default;
        direction = Flat(direction);
        if (direction.sqrMagnitude < 0.001f || distance <= 0f) return false;
        Vector3 origin = SensorOrigin();
        RaycastHit[] hits = Physics.SphereCastAll(origin, sensorRadius, direction.normalized,
            distance, obstacleLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsOwnCollider(hit.collider)) continue;
            if (player != null && hit.collider.transform.root == player.root) continue;
            if (hit.distance < nearest)
            {
                nearestHit = hit;
                nearest = hit.distance;
                found = true;
            }
        }
        return found;
    }

    private bool IsOwnCollider(Collider candidate)
    {
        foreach (Collider c in ownColliders) if (c == candidate) return true;
        return false;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private void OnCollisionEnter(Collision collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        PlayerCandy candy = collision.collider.GetComponentInParent<PlayerCandy>();
        if (candy != null) candy.ReceiveVanImpact(collision.GetContact(0).point, speed, transform.position);
        if (speed >= crashSpeedThreshold && mode != Mode.Recover) EnterRecovery();
    }

    private void Recover(float dt, float upright)
    {
        if (upright < 0.8f)
        {
            Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
            if (axis.sqrMagnitude < 0.01f) axis = transform.forward;
            float assist = Mathf.Lerp(9f, 24f, Mathf.Clamp01(stateTime / 6f));
            body.AddTorque(axis.normalized * assist, ForceMode.Acceleration);
            if (stateTime > 5f && Mathf.Repeat(stateTime, 1.5f) < dt)
                body.AddForce(Vector3.up * 2.5f, ForceMode.VelocityChange);
        }
        if (stateTime > emergencyResetSeconds)
        {
            EmergencyReset();
            return;
        }
        if (stateTime >= recoverSeconds && upright >= 0.8f && Mathf.Abs(Vector3.Dot(body.angularVelocity, Vector3.up)) < 2f)
            ChangeMode(Mode.Pursue);
    }

    private void EnterRecovery()
    {
        ChangeMode(Mode.Recover);
        stalledTime = 0f;
        reverseUntil = 0f;
        followingWall = false;
    }

    private void ChangeMode(Mode next)
    {
        mode = next;
        stateTime = 0f;
        lastDistance = float.PositiveInfinity;
    }

    private void EmergencyReset()
    {
        // Last resort: prefer a physically plausible recovery, but never strand a van forever.
        Vector3 location = spawnPosition;
        if (player != null && Vector3.Distance(location, player.position) < 8f)
            location += (location - player.position).normalized * 9f;
        body.position = location;
        body.rotation = spawnRotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        flipTime = 0f;
        stalledTime = 0f;
        reverseUntil = 0f;
        followingWall = false;
        stallPosition = transform.position;
        stallCheckTime = Time.time;
        ChangeMode(Mode.Pursue);
    }
}

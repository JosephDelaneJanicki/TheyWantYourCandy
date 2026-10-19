
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class VanEngineAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private Rigidbody rb;

    [Header("Engine Pitch")]
    [SerializeField] private float idlePitch = 0.75f;
    [SerializeField] private float maxPitch = 1.8f;
    [SerializeField] private float speedForMaxPitch = 35f;
    [SerializeField] private float pitchSmoothness = 5f;

    [Header("Volume")]
    [SerializeField] private float idleVolume = 0.15f;
    [SerializeField] private float maxVolume = 0.55f;

    private void Awake()
    {
        if (engineAudio == null)
            engineAudio = GetComponent<AudioSource>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        engineAudio.loop = true;
        engineAudio.spatialBlend = 1f;
        engineAudio.playOnAwake = true;
    }

    private void Start()
    {
        if (engineAudio == null)
        return;

        engineAudio.loop = true;

        if (engineAudio.resource != null && !engineAudio.isPlaying)
            engineAudio.Play();
    }

    private void Update()
    {
        if (engineAudio == null || rb == null)
            return;

        float speed = rb.linearVelocity.magnitude;

        float speedRatio = Mathf.Clamp01(
            speed / speedForMaxPitch
        );

        float targetPitch = Mathf.Lerp(
            idlePitch,
            maxPitch,
            speedRatio
        );

        float targetVolume = Mathf.Lerp(
            idleVolume,
            maxVolume,
            speedRatio
        );

        engineAudio.pitch = Mathf.Lerp(
            engineAudio.pitch,
            targetPitch,
            Time.deltaTime * pitchSmoothness
        );

        engineAudio.volume = Mathf.Lerp(
            engineAudio.volume,
            targetVolume,
            Time.deltaTime * pitchSmoothness
        );
    }
}

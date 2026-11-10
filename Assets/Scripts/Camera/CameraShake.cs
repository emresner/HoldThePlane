using UnityEngine;

/// <summary>
/// Shakes the camera for a short moment (a crash, for example) and puts it back exactly where it was.
/// The strength is a share of the visible height, not a world distance, so the shake looks equally strong on every screen shape:
/// on a narrow screen the camera zooms out (see <see cref="CameraFit"/>) and the same world distance would look smaller.
/// Only the camera's position is touched; the zoom stays with <see cref="CameraFit"/>.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    [Tooltip("How far the camera moves at the start, as a share of the visible height (0.035 = 3.5%).")]
    [SerializeField, Range(0f, 0.15f)] private float strength = 0.035f;
    [Tooltip("How long the shake lasts, in seconds.")]
    [SerializeField, Min(0.05f)] private float seconds = 0.35f;
    [Tooltip("How fast the camera jitters (higher = nervous, lower = a swaying push).")]
    [SerializeField, Min(1f)] private float frequency = 40f;

    private Camera cam;
    private Vector3 restPosition;
    private float timer;
    private bool isShaking;
    private Vector2 noiseSeed;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        restPosition = transform.localPosition;
    }

    private void OnDisable()
    {
        StopShake();
    }

    /// <summary>Starts a shake (a running one starts over).</summary>
    public void Shake()
    {
        timer = 0f;
        isShaking = true;
        noiseSeed = new Vector2(Random.value * 100f, Random.value * 100f);
    }

    private void LateUpdate()
    {
        if (!isShaking)
        {
            return;
        }

        // Unscaled time: the shake must also finish if the game is slowed or paused.
        timer += Time.unscaledDeltaTime;
        float t = timer / seconds;
        if (t >= 1f)
        {
            StopShake();
            return;
        }

        float fade = (1f - t) * (1f - t); // strong at first, then dies away
        float amplitude = strength * 2f * cam.orthographicSize * fade;
        float x = (Mathf.PerlinNoise(noiseSeed.x, timer * frequency) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(noiseSeed.y, timer * frequency) - 0.5f) * 2f;
        transform.localPosition = restPosition + new Vector3(x, y, 0f) * amplitude;
    }

    private void StopShake()
    {
        isShaking = false;
        transform.localPosition = restPosition;
    }
}

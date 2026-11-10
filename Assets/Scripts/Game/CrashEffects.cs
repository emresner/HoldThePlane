using UnityEngine;

/// <summary>
/// The look of a crash: the plane disappears in a burst of smoke and sparks and the camera shakes.
/// It only listens to <see cref="PlaneCrash.Crashed"/>; the crash itself and the game over flow do not know about it.
/// </summary>
public class CrashEffects : MonoBehaviour
{
    [SerializeField] private PlaneCrash plane;
    [Tooltip("The plane's picture. It is hidden when the explosion starts (a reload of the scene brings it back).")]
    [SerializeField] private SpriteRenderer planeVisual;
    [SerializeField] private ParticleSystem smoke;
    [SerializeField, Min(0)] private int smokeCount = 10;
    [SerializeField] private ParticleSystem sparks;
    [SerializeField, Min(0)] private int sparkCount = 8;
    [SerializeField] private CameraShake cameraShake;

    private void OnEnable()
    {
        plane.Crashed += Play;
    }

    private void OnDisable()
    {
        plane.Crashed -= Play;
    }

    private void Play()
    {
        Vector3 position = plane.transform.position;
        planeVisual.enabled = false;
        ParticleBurst.At(smoke, position, smokeCount);
        ParticleBurst.At(sparks, position, sparkCount);
        cameraShake.Shake();
    }
}

using UnityEngine;

/// <summary>
/// Plays a one-off burst of an existing Particle System at a place in the world.
/// One system stays in the scene and is reused for every burst (it is moved and told to emit),
/// so a burst costs no object creation and no garbage collection, which matters on WebGL and phones.
/// The system must simulate in World space, otherwise the particles would follow the system when it moves.
/// </summary>
public static class ParticleBurst
{
    public static void At(ParticleSystem system, Vector3 position, int count)
    {
        Vector3 own = system.transform.position;
        system.transform.position = new Vector3(position.x, position.y, own.z);
        system.Emit(count);
    }
}

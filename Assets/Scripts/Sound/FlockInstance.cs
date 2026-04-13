using UnityEngine;
using AK.Wwise;

public class FlockInstance : MonoBehaviour
{
    [SerializeField] private ParticleSystem ravenParticles;
    [SerializeField] private AkGameObj wiseEmitter;
    [SerializeField] private AK.Wwise.Event flockSoundEvent;

    private ParticleSystem.Particle[] particles;

    void Start()
    {
        flockSoundEvent.Post(wiseEmitter.gameObject);
    }

    void LateUpdate()
    {
        int count = ravenParticles.particleCount;
        if (count == 0)
        {
            Destroy(gameObject); // Flock is done, clean up
            return;
        }

        if (particles == null || particles.Length < count)
            particles = new ParticleSystem.Particle[count];

        ravenParticles.GetParticles(particles, count);

        bool isLocalSpace = ravenParticles.main.simulationSpace == ParticleSystemSimulationSpace.Local;

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;
            if (isLocalSpace)
                pos = ravenParticles.transform.TransformPoint(pos);
            sum += pos;
        }

        wiseEmitter.transform.position = sum / count;
    }

    void OnDestroy()
    {
        // Stop sound when flock is cleaned up
        flockSoundEvent.Stop(wiseEmitter.gameObject);
    }
}
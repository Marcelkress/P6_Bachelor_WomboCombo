using UnityEngine;

public class ParticleSoundFollower : MonoBehaviour
{
    [SerializeField] private ParticleSystem ravenParticles;
    [SerializeField] private GameObject wiseEmitter;

    private ParticleSystem.Particle[] particles;

    void LateUpdate()
    {
        if (ravenParticles == null) return;

        int count = ravenParticles.particleCount;
        if (count == 0) return;

        if (particles == null || particles.Length < count)
            particles = new ParticleSystem.Particle[count];

        ravenParticles.GetParticles(particles, count);

        bool isLocalSpace = ravenParticles.main.simulationSpace == ParticleSystemSimulationSpace.Local;

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;

            // Convert to world space if the system simulates in local space
            if (isLocalSpace)
                pos = ravenParticles.transform.TransformPoint(pos);

            sum += pos;
        }

        wiseEmitter.transform.position = sum / count;
    }
}
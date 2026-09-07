using UnityEngine;

public class VFXSpeed : MonoBehaviour
{
    [SerializeField] private float simulationSpeed = 0.5f;

    private void Awake()
    {
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            var main = particle.main;
            main.simulationSpeed = simulationSpeed;
        }
    }
}
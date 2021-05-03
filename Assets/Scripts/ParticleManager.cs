using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ParticleType {
    TypeAppear,
    TypeMatch
}

public class ParticleManager : MonoBehaviour
{
    public List<ParticleSystem> particles;

    public void PlayParticle(ParticleType particleType) {
        particles[(int) particleType].Play();
    }
}

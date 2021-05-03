using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType {
    TypeSelect,
    TypeMatch,
    TypeGameOver
};

public class SoundManager : MonoBehaviour
{
    public List<AudioClip> clips;
    public static SoundManager Instance { get; private set; }
    AudioSource audioSource;

    void Awake() {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(SoundType clipType) {
        audioSource.PlayOneShot(clips[(int) clipType]);
    }
}

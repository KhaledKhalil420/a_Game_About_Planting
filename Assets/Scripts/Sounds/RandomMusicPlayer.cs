using UnityEngine;
using UnityEngine.Audio;

public class RandomMusicPlayer : MonoBehaviour
{
    public AudioClip[] musicClips;
    private AudioClip lastPickedClip;
    public float minDelay = 30f;
    public float maxDelay = 120f;
    public AudioMixerGroup musicGroup;

    private AudioSource source;
    private float timer;

    void Start()
    {
        musicClips = Resources.LoadAll<AudioClip>("Music");
        source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = musicGroup;
        timer = Random.Range(minDelay, maxDelay);
    }

    void Update()
    {
        if (!source.isPlaying)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                PlayRandom();
        }

        if (Input.GetKey(KeyCode.K))
        {
            source.Stop();    
        }
    }

    void ScheduleNext()
    {
        timer = Random.Range(minDelay, maxDelay);
    }

    void PlayRandom()
    {
        if (musicClips.Length == 0) return;

        AudioClip audioClip = PickMusicClip();
        if (audioClip == null)
        {
            PlayRandom();
            return;
        }

        source.clip = audioClip;
        source.Play();
        ScheduleNext();
    }

    private AudioClip PickMusicClip()
    {
        AudioClip clip = musicClips[Random.Range(0, musicClips.Length)];

        if (clip != lastPickedClip)
        {
            lastPickedClip = clip;
            return clip;
        }

        return null;
    }
}

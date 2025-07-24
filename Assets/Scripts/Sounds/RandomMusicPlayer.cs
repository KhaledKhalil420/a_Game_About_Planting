using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class RandomMusicPlayer : MonoBehaviour
{
    public AudioMixerGroup musicGroup;
    public float minDelay = 30f;
    public float maxDelay = 120f;

    private AudioSource source;
    private float timer;

    private List<AudioClip> playlist = new List<AudioClip>();
    private List<AudioClip> playedClips = new List<AudioClip>();

    void Start()
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Music");
        playlist.AddRange(clips);
        ShufflePlaylist();

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
                PlayNext();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            source.Stop();
            timer = 0f;
        }
    }

    void ScheduleNext()
    {
        timer = Random.Range(minDelay, maxDelay);
    }

    void PlayNext()
    {
        if (playlist.Count == 0)
        {
            playlist.AddRange(playedClips);
            playedClips.Clear();
            ShufflePlaylist();
        }

        if (playlist.Count == 0) return;

        AudioClip clip = playlist[0];
        playlist.RemoveAt(0);
        playedClips.Add(clip);

        source.clip = clip;
        source.Play();

        ScheduleNext();
    }

    void ShufflePlaylist()
    {
        for (int i = 0; i < playlist.Count; i++)
        {
            AudioClip temp = playlist[i];
            int randomIndex = Random.Range(i, playlist.Count);
            playlist[i] = playlist[randomIndex];
            playlist[randomIndex] = temp;
        }
    }
}

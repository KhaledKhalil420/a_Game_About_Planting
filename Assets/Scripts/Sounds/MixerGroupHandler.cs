using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


#region Mixer Handler

public class MixerGroupHandler : MonoBehaviour
{
    public static List<AudioMixerDataGroup> AudioMixerDataGroups = new();
    
    [SerializeField] private List<AudioMixerDataGroup> _audioMixerDataGroups = new();

    void Awake()
    {
        AudioMixerDataGroups = _audioMixerDataGroups;

        foreach (var group in AudioMixerDataGroups)
        {
            group.Initialize();
        }
    }

    public static AudioMixerDataGroup GetAudioMixer(MixerType _type)
    {
        foreach (var item in AudioMixerDataGroups)
        {
            if (item.type == _type)
            {
                return item;
            }
        }

        return null;
    }
}

#endregion

#region Mixer Data Group

public enum MixerType { Master, Music, Sfx }

[Serializable]
public class AudioMixerDataGroup
{
    public MixerType type;
    public AudioMixerGroup mixer;
    public bool fadeIn;

    public void Initialize()
    {

    }

    public void SetMixerVolume(float volume)
    {
        mixer.audioMixer.SetFloat("Volume", volume);
    }

    public void SetMixerVolume(float from, float to, float duration)
    {
        //DoTween here
    }
}

#endregion
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;

namespace WG_Casino
{
    public class WG_AudioManager : MonoBehaviour
    {
        public static WG_AudioManager Instance;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource m_seSource;
        [SerializeField] private AudioSource m_meSource;
        [SerializeField] private AudioSource m_bgmSource;
        [SerializeField] private AudioSource ambience;

        [Header("Audio Mixer References")]
        public AudioMixer masterMixer;

        [System.Serializable]
        public class AudioGroup
        {
            public string displayName;
            public string exposedParameter;
        }

        [Header("Grupos de Áudio")]
        public AudioGroup master = new AudioGroup { displayName = "Master", exposedParameter = "MasterVol" };
        public AudioGroup me = new AudioGroup { displayName = "ME", exposedParameter = "MEVol" };
        public AudioGroup sfx = new AudioGroup { displayName = "SFX", exposedParameter = "SFXVol" };

        [Header("Audio Clips")]
        public AudioList m_audioList;
        public static AudioList AudioClips
        {
            get
            {
                if (Instance != null)
                {
                    return Instance.m_audioList;
                }
                return null;
            }
        }

        [System.Serializable]
        public class AudioList
        {
            [Header("Audio References"), Space(10)]
            public List<AudioClip> m_slotStop = new List<AudioClip>();
            public AudioClip m_slotCoin;
            public AudioClip m_slotsWin;
            public AudioClip m_slotsBigWin;
            public AudioClip m_error;
            public AudioClip m_spinner;
            public AudioClip m_line;
            public AudioClip m_play;

            [Header("Audio References Bonus")]
            public AudioClip m_slotHit;
        }

        // Pool dinâmico de AudioSources para SE
        private List<AudioSource> m_seSources = new List<AudioSource>();
        private Dictionary<AudioClip, float> m_lastPlayTime = new Dictionary<AudioClip, float>();
        [SerializeField] private float m_minTimeBetweenSameClip = 0.05f;

        // Sistema de clips por nome (do SoundManager original)
        private Dictionary<string, AudioSource> clips = new Dictionary<string, AudioSource>();
        private List<AudioSource> lastPlayed = new List<AudioSource>();

        // Volumes
        [SerializeField] private float m_mainVolume = 1f;
        [SerializeField] private float volume;
        [SerializeField] private float volumeMusic;
        [SerializeField] private float volumeEffect;

        public float Volume
        {
            get => volume;
            set
            {
                Debug.Log("set volume " + value);
                volume = value;
                ClearDone();
                foreach (AudioSource c in lastPlayed)
                {
                    c.volume = VolumeEffect * volume;
                }
                if (ambience != null)
                    ambience.volume = volumeMusic * volume;

                SetMixerVolume(master.exposedParameter, volume);
                PlayerPrefs.SetFloat("volume", volume);
            }
        }

        public float VolumeMusic
        {
            get => volumeMusic;
            set
            {
                volumeMusic = value;
                if (ambience != null)
                    ambience.volume = volumeMusic * volume;

                SetMixerVolume(me.exposedParameter, volumeMusic * volume);
                PlayerPrefs.SetFloat("volumeMusic", volumeMusic);
            }
        }

        public float VolumeEffect
        {
            get => volumeEffect;
            set
            {
                volumeEffect = value;

                SetMixerVolume(sfx.exposedParameter, volumeEffect * volume);
                SetMixerVolume(me.exposedParameter, volumeMusic * volume);

                ClearDone();
                foreach (AudioSource c in lastPlayed)
                {
                    c.volume = VolumeEffect * volume;
                }
                PlayerPrefs.SetFloat("volumeEffect", volumeEffect);
            }
        }

        public float MainVolume
        {
            get => m_mainVolume;
            set => m_mainVolume = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Carregar preferências
            volume = PlayerPrefs.GetFloat("volume", 0.5f);
            volumeMusic = PlayerPrefs.GetFloat("volumeMusic", 1);
            volumeEffect = PlayerPrefs.GetFloat("volumeEffect", 1);

            if (ambience != null)
                ambience.volume = volumeMusic * volume;

            SetMixerVolume(master.exposedParameter, volume);
            SetMixerVolume(me.exposedParameter, volumeMusic);
            SetMixerVolume(sfx.exposedParameter, volumeEffect);

            // Inicializar pool SE
            if (m_seSource != null)
            {
                m_seSources.Add(m_seSource);
            }

            // Carregar clips do SoundManager (por nome)
            AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
            foreach (AudioSource source in audioSources)
            {
                if (!clips.ContainsKey(source.name.ToLower()))
                {
                    clips.Add(source.name.ToLower(), source);
                }
            }
        }

        private void Start()
        {
            m_mainVolume = 1f;
        }

        private void SetMixerVolume(string exposedParameter, float volume)
        {
            if (masterMixer == null) return;

            float volumeDB = VolumeToDecibels(volume);
            masterMixer.SetFloat(exposedParameter, volumeDB);
        }

        private float VolumeToDecibels(float volume)
        {
            if (volume <= 0.0001f)
                return -80f;
            return Mathf.Log10(volume) * 20f;
        }

        private void ClearDone()
        {
            for (int i = lastPlayed.Count - 1; i >= 0; i--)
            {
                if (lastPlayed[i] != null && !lastPlayed[i].isPlaying)
                {
                    lastPlayed.RemoveAt(i);
                }
            }
        }

        private AudioSource GetOrCreateSESource()
        {
            foreach (AudioSource source in m_seSources)
            {
                if (source != null && !source.isPlaying)
                {
                    return source;
                }
            }

            if (m_seSource == null) return null;

            GameObject newSourceObj = new GameObject($"SE_Source_{m_seSources.Count}");
            newSourceObj.transform.SetParent(transform);
            AudioSource newSource = newSourceObj.AddComponent<AudioSource>();
            newSource.outputAudioMixerGroup = m_seSource.outputAudioMixerGroup;
            newSource.playOnAwake = false;
            m_seSources.Add(newSource);

            return newSource;
        }

        #region Métodos Públicos (WG_AudioManager originais)

        public void _PlayME(AudioClip _audio, float _volume = 1f)
        {
            if (_audio == null || m_meSource == null) return;
            m_meSource.PlayOneShot(_audio, _volume * m_mainVolume);
        }

        public void _PlaySE(AudioClip _audio, float _volume = 1f)
        {
            if (_audio == null) return;

            if (m_lastPlayTime.ContainsKey(_audio))
            {
                float timeSinceLastPlay = Time.time - m_lastPlayTime[_audio];
                if (timeSinceLastPlay < m_minTimeBetweenSameClip)
                {
                    return;
                }
            }

            m_lastPlayTime[_audio] = Time.time;

            AudioSource source = GetOrCreateSESource();
            if (source != null)
            {
                source.clip = _audio;
                source.volume = _volume * m_mainVolume;
                source.pitch = 1f;
                source.Play();
            }
        }

        public void _PlayBGM(AudioClip _audio, float _volume = 1f, bool _loop = true)
        {
            if (_audio == null || m_bgmSource == null) return;

            m_bgmSource.clip = _audio;
            m_bgmSource.loop = _loop;
            m_bgmSource.volume = _volume * m_mainVolume;
            m_bgmSource.Play();
        }

        public void _PlaySEUnrestricted(AudioClip _audio, float _volume = 1f)
        {
            if (_audio == null) return;

            AudioSource source = GetOrCreateSESource();
            if (source != null)
            {
                source.clip = _audio;
                source.volume = _volume * m_mainVolume;
                source.pitch = 1f;
                source.Play();
            }
        }

        public void _PlaySEWithPitchVariation(AudioClip _audio, float _volume = 1f, float maxPitchVariation = 0.15f)
        {
            if (_audio == null) return;

            AudioSource source = GetOrCreateSESource();
            if (source != null)
            {
                source.clip = _audio;
                source.volume = _volume * m_mainVolume;
                source.pitch = 1f + Random.Range(-maxPitchVariation, maxPitchVariation);
                source.Play();
            }
        }

        #endregion

        #region Métodos Estáticos (WG_AudioManager)

        public static void PlaySE(AudioClip _audio, float _volume = 1f)
        {
            if (Instance != null && _audio != null)
            {
                Instance._PlaySE(_audio, _volume);
            }
        }

        public static void PlaySEUnrestricted(AudioClip _audio, float _volume = 1f)
        {
            if (Instance != null && _audio != null)
            {
                Instance._PlaySEUnrestricted(_audio, _volume);
            }
        }

        public static void PlaySEWithPitchVariation(AudioClip _audio, float _volume = 1f, float maxPitchVariation = 0.15f)
        {
            if (Instance != null && _audio != null)
            {
                Instance._PlaySEWithPitchVariation(_audio, _volume, maxPitchVariation);
            }
        }

        public static void PlaySEStop()
        {
            if (Instance != null)
            {
                foreach (AudioSource source in Instance.m_seSources)
                {
                    if (source != null)
                    {
                        source.Stop();
                    }
                }
            }
        }

        public static void StopSpecificSE(AudioClip clip)
        {
            if (Instance != null && clip != null)
            {
                foreach (AudioSource source in Instance.m_seSources)
                {
                    if (source != null && source.isPlaying && source.clip == clip)
                    {
                        source.Stop();
                    }
                }
            }
        }

        public static void PlayME(AudioClip _audio, float _volume = 1f)
        {
            if (Instance != null && _audio != null)
            {
                Instance._PlayME(_audio, _volume);
            }
        }

        public static void PlayBGM(AudioClip _audio, float _volume = 1f, bool _loop = true)
        {
            if (Instance != null && _audio != null)
            {
                Instance._PlayBGM(_audio, _volume, _loop);
            }
        }

        #endregion

        #region Métodos do SoundManager (por nome/string)

        public void PlayBgMusic()
        {
            if (ambience != null && !ambience.isPlaying)
            {
                ambience.Play();
            }
        }

        public void Play(string id)
        {
            ClearDone();
            if (clips.TryGetValue(id.ToLower(), out AudioSource c))
            {
                lastPlayed.Add(c);
                c.volume = VolumeEffect * Volume;
                c.Play();
            }
            else
            {
                Debug.Log("Som nao encontrado: " + id);
            }
        }

        public void PlayFrom(string id, float time)
        {
            ClearDone();
            if (clips.TryGetValue(id.ToLower(), out AudioSource c))
            {
                lastPlayed.Add(c);
                c.volume = VolumeEffect * Volume;
                c.time = time;
                c.Play();
            }
            else
            {
                Debug.Log("Som nao encontrado: " + id);
            }
        }

        public void Stop(string id)
        {
            Debug.Log("stop " + id);
            if (clips.TryGetValue(id.ToLower(), out AudioSource c))
            {
                c.Stop();
            }
        }

        public void StopAll()
        {
            Debug.Log("Stop all");
            Clear();
        }

        public void Clear()
        {
            foreach (AudioSource c in lastPlayed)
            {
                if (c != null)
                {
                    c.Stop();
                }
            }
            lastPlayed.Clear();
        }

        #endregion

        #region Métodos de Compatibilidade (SoundManager estático)

        public static SoundManagerHelper InstanceHelper
        {
            get
            {
                if (Instance != null)
                {
                    return new SoundManagerHelper(Instance);
                }
                return null;
            }
        }

        #endregion

        private void Update()
        {
            // Limpar entradas antigas do dicionário de cooldown
            var keysToRemove = new List<AudioClip>();
            foreach (var kvp in m_lastPlayTime)
            {
                if (Time.time - kvp.Value > 30f)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                m_lastPlayTime.Remove(key);
            }

            // Limpar fontes de áudio não utilizadas
            CleanupUnusedAudioSources();
        }

        private void CleanupUnusedAudioSources()
        {
            if (m_seSources.Count > 10)
            {
                var sourcesToRemove = m_seSources
                    .Where(source => source != m_seSource && (source == null || !source.isPlaying))
                    .Skip(10)
                    .ToList();

                foreach (var source in sourcesToRemove)
                {
                    if (source != null)
                    {
                        m_seSources.Remove(source);
                        if (source.gameObject != null)
                        {
                            Destroy(source.gameObject);
                        }
                    }
                }
            }
        }
    }

    // Classe auxiliar para compatibilidade com chamadas SoundManager .instance
    public class SoundManagerHelper
    {
        private WG_AudioManager manager;

        public SoundManagerHelper(WG_AudioManager manager)
        {
            this.manager = manager;
        }

        public void Play(string id) => manager.Play(id);
        public void PlayFrom(string id, float time) => manager.PlayFrom(id, time);
        public void Stop(string id) => manager.Stop(id);
        public void Stop() => manager.StopAll();
        public void Clear() => manager.Clear();
        public void PlayBgMusic() => manager.PlayBgMusic();

        public float Volume
        {
            get => manager.Volume;
            set => manager.Volume = value;
        }

        public float VolumeMusic
        {
            get => manager.VolumeMusic;
            set => manager.VolumeMusic = value;
        }

        public float VolumeEffect
        {
            get => manager.VolumeEffect;
            set => manager.VolumeEffect = value;
        }
    }
}
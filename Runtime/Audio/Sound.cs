using UnityEngine;
using UnityEngine.UI;

namespace WG_Casino
{
    public class Sound : MonoBehaviour
    {
        [SerializeField] Slider slider;
        [SerializeField] Slider sliderEffect;
        [SerializeField] Slider sliderMusic;
        bool canClose;
        void Start()
        {
            //Debug.Log("start");
            slider.value = WG_AudioManager.Instance.Volume;
            //Debug.Log("start b ");
            sliderMusic.value = WG_AudioManager.Instance.VolumeMusic;
            //Debug.Log("start c ");
            sliderEffect.value = WG_AudioManager.Instance.VolumeEffect;
            //Debug.Log("start d ");

            slider.onValueChanged.AddListener((v) =>
            {
                Debug.Log("geral " + v);
                WG_AudioManager.Instance.Volume = v;
                if (v == 0)
                {
                    //openImg.sprite = MuteIcon;
                }
                else
                {
                    //openImg.sprite = soundIcon;
                }
                sliderEffect.SetValueWithoutNotify(WG_AudioManager.Instance.VolumeEffect);
                sliderMusic.SetValueWithoutNotify(WG_AudioManager.Instance.VolumeMusic);
            });

            sliderEffect.onValueChanged.AddListener((v) => WG_AudioManager.Instance.VolumeEffect = v);

            sliderMusic.onValueChanged.AddListener((v) => WG_AudioManager.Instance.VolumeMusic = v);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

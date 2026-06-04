using System.Collections;
using UnityEngine;
using WG_Casino.SlotMachine;

namespace WG_Casino
{
    public class BonusManager : MonoBehaviour
    {
        public static BonusManager Instance;
        public bool isAnimating
        {
            get;
            set;
        }

        public bool isAnimatingBonus
        {
            get;
            set;
        }

        bool onBonus = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            WG_SlotMachine.Instance.OnReelsCompleted += OnReelsCompleted;
            WG_SlotMachine.Instance.OnReelsStarted += OnReelsStarted;
        }

        private void OnReelsStarted()
        {
            //Debug.Log("Reels started spinning!");
            //panelBonus.SetActive(false);
            isAnimating = false;
            if (SlotClient.Instance.bonusResponse.bonusOver)
                onBonus = false;
        }

        private void OnReelsCompleted()
        {
            //Debug.Log("All reels completed!");

            var bonusResponse = SlotClient.Instance.bonusResponse;
            if (bonusResponse.bonusMultipliers.Count == 0 || bonusResponse.bonusOver)
            {
                return;
            }
            if (bonusResponse != null && bonusResponse.bonusReel.rows.Length != 0)
            {
                isAnimating = true;

                StartCoroutine(StartBonus());
            }
        }

        IEnumerator StartBonus()
        {
            var bonusResponse = SlotClient.Instance.bonusResponse;

            yield return new WaitUntil(() => WG_SlotMachine.Instance.AreAllReelsCompleted());
            yield return new WaitUntil(() => !PayoutAnimation.Instance.isPayout);

            yield return new WaitForSeconds(0.5f);
            isAnimating = false;
            if (bonusResponse.freespinsRemaining == 0) onBonus = false;

            if (bonusResponse.freespinsRemaining == 0) onBonus = false;
            else onBonus = true;
        }
    }
}
// WG_PaymentSetup.cs
using UnityEngine;
using System.Collections.Generic;

namespace WG_Casino.Setup
{
    [CreateAssetMenu(fileName = "PaymentConfig", menuName = "WG Casino/Payment Configuration")]
    public class WG_PaymentSetup : ScriptableObject
    {
        public List<PaymentPattern> paymentPatterns;
        public float baseCoinValue = 1.0f;
        public float bonusMultiplier = 5.0f;
    }
}
﻿using UnityEngine;

namespace BumiMobile
{
    [System.Serializable]
    public class CurrencyData
    {
        [SerializeField] bool displayAlways = false;
        public bool DisplayAlways => displayAlways;

        public void Init(Currency currency)
        {

        }
    }
}
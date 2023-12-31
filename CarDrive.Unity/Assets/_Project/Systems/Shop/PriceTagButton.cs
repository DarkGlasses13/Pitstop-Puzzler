﻿using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Systems.Shop
{
    public class PriceTagButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text _price;
        [SerializeField] private AudioSource 
            _dealSound,
            _failSound;

        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
        }

        public void SetPrice(int value) => _price.text = value.ToString();

        public void OnDeal() => _dealSound.Play();

        public void OnFail() => _failSound.Play();
    }
}
﻿using UnityEngine;

namespace Assets._Project.Systems.Collectabling
{
    [CreateAssetMenu(menuName = "Item")]
    public class ItemReference : ScriptableObject, IItem
    {
        [field: SerializeField] public string ID { get; private set; }
        [field: SerializeField] public string Title  { get; private set; }
        [field: SerializeField] public string Description  { get; private set; }
    }
}
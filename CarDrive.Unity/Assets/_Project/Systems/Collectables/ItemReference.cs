﻿using UnityEngine;

namespace Assets._Project.Systems.Collecting
{
    [CreateAssetMenu(menuName = "Item")]
    public class ItemReference : ScriptableObject, IItem
    {
        [field: SerializeField] public string ID { get; private set; }
        [field: SerializeField] public string Title { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public ItemType Type { get; private set; }
        [field: SerializeField] public int MergeLevel { get; private set; }
        [field: SerializeField] public float Stat { get; private set; }
    }
}
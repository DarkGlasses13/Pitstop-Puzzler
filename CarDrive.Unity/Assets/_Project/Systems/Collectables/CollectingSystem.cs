﻿using Assets._Project.Architecture;
using Assets._Project.Architecture.UI;
using UnityEditor.Rendering;
using UnityEngine;

namespace Assets._Project.Systems.Collecting
{
    public class CollectingSystem : GameSystem
    {
        private readonly CollectablesConfig _config;
        private readonly Money _money;
        private readonly IItemDatabase _database;
        private readonly IInventory _inventory;
        private readonly ICanCollectItems _collector;
        private readonly IUICounter _uiMoneyCounter;
        private float _collectingRadius = 2;
        private Collider[] _detecables = new Collider[50];

        public CollectingSystem(CollectablesConfig config, Money money, IItemDatabase database,
            IInventory inventory,ICanCollectItems collector, IUICounter uiMoneyCounter)
        {
            _config = config;
            _money = money;
            _database = database;
            _inventory = inventory;
            _collector = collector;
            _uiMoneyCounter = uiMoneyCounter;
        }

        public override void Initialize()
        {
            _uiMoneyCounter.Set(_money.ToString());
        }

        public override void FixedTick()
        {
            int detectablesCount = Physics.OverlapSphereNonAlloc(_collector.Center,
                _collectingRadius, _detecables, _config.LayerMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < detectablesCount; i++)
            {
                if (_detecables[i].TryGetComponent(out ItemObject itemObject))
                {
                    if (itemObject.Type == ItemType.Money)
                    {
                        itemObject.gameObject.SetActive(false);
                        _money.Add();
                        _collector?.OnCollect();
                        _uiMoneyCounter.Set(_money.ToString());
                        continue;
                    }

                    if (_inventory.TryAdd(_database.GetByID(itemObject.ID)))
                    {
                        _collector?.OnCollect();
                        itemObject.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
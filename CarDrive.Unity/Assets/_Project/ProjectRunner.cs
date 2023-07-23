﻿using Assets._Project.Architecture;
using Assets._Project.Architecture.DI;
using Assets._Project.CameraControl;
using Assets._Project.Helpers;
using Assets._Project.Input;
using Assets._Project.SceneChange;
using Assets._Project.Systems.Collecting;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets._Project
{
    public class ProjectRunner : RunnerWithAutomaticSystemsInitialization
    {
        private ISceneChanger _sceneChanger;

        protected override async Task CreateSystems()
        {
            DontDestroyOnLoad(this);
            Application.targetFrameRate = 60;
            DIContainer container = new GameObject("Project DI Container").AddComponent<DIContainer>();
            Coroutiner coroutiner = new GameObject("Coroutiner").AddComponent<Coroutiner>();
            DontDestroyOnLoad(container);
            DontDestroyOnLoad(coroutiner);
            LocalAssetLoader assetLoader = new();
            PlayerInputConfig playerInputConfig = await assetLoader.Load<PlayerInputConfig>("Player Input Config");
            IPlayerInput playerInput = new UniversalPlayerInput(playerInputConfig);
            _sceneChanger = new SceneChanger();
            IItemDatabase itemDatabase = await assetLoader.Load<ItemDatabase>("Item Database");
            CollectablingConfig moneyControlConfig = await assetLoader.Load<CollectablingConfig>("Money Control Config");
            container.Bind(assetLoader);
            container.Bind(playerInput);
            container.Bind(_sceneChanger);
            container.Bind(coroutiner);
            container.Bind(itemDatabase);
            container.Bind(moneyControlConfig);
            container.Bind(new Money(moneyControlConfig, 0));
            container.Bind(new Cinematographer());

            _systems = new()
            {

            };
        }

        protected override void OnInitializationCompleted()
        {
            _sceneChanger.Change("Level");
        }
    }
}

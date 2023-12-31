using Assets._Project.Architecture;
using Assets._Project.CameraControl;
using Assets._Project.GameStateControl;
using Assets._Project.Helpers;
using Assets._Project.Input;
using Assets._Project.Systems.Collecting;
using Cinemachine;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets._Project.Systems.Driving
{
    public class DrivingSystem : GameSystem, IGameStateSwitchHandler
    {
        public event Action<int> OnGasRegulated;
        private readonly LocalAssetLoader _assetLoader;
        private readonly IPlayerInput _playerInput;
        private readonly IDrivable _drivable;
        private readonly Player _player;
        private readonly GameState _gameState;
        private readonly Coroutiner _coroutiner;
        private readonly Cinematographer _conematographer;
        private DrivingConfig _config;
        private int _currentRoadLineIndex;
        private float _gasValue;
        private Coroutine _gasRegulationRoutine;
        private float _gasRegulation = 1;
        private float[] _roadLines;
        private bool _isMeneuver;
        private float _maneuverCooldown;
        private float _maneuverDirection;
        private CinemachineBasicMultiChannelPerlin _cameraShake;
        private bool _canDrive;
        private Vector2 _roadWidth;
        private float _stearInput;
        private bool _isStearing;
        private bool _isGasregulationEnabled = true;

        public DrivingSystem(LocalAssetLoader assetLoader, IPlayerInput playerInput, IDrivable drivable,
            Player player, GameState gameState, Coroutiner coroutiner, Cinematographer cinematographer, Vector2 roadWidth)
        {
            _assetLoader = assetLoader;
            _playerInput = playerInput;
            _drivable = drivable;
            _player = player;
            _gameState = gameState;
            _coroutiner = coroutiner;
            _conematographer = cinematographer;
            _roadWidth = roadWidth;
        }

        public override async Task InitializeAsync()
        {
            _config = await _assetLoader.Load<DrivingConfig>("Driving Config");
            _currentRoadLineIndex = _config.RoadLines / 2;
            _roadLines = new float[_config.RoadLines];
            _roadLines[0] = _config.RoadLines / 2 * -_config.StearStep;

            if (_config.RoadLines % 2 == 0)
                _roadLines[0] += _config.StearStep / 2f;

            for (int i = 1; i < _roadLines.Length; i++)
            {
                _roadLines[i] = _roadLines[i - 1] + _config.StearStep;
            }

            _drivable.SetToLine(_roadLines[_currentRoadLineIndex]);
            _cameraShake = _conematographer
                .GetCamera(GameCamera.Run).Instance
                .GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }

        public override void OnEnable()
        {
            _playerInput.OnSwipe += OnSwipe;
            _playerInput.OnSwipeEnded += OnSwipeEnded;
            _playerInput.OnVerticalSwipeWithThreshold += RegulateGas;
            _gameState.OnSwitched += OnSateSwitched;
        }

        private void OnSwipe(Vector2 value)
        {
            if (_canDrive)
            {
                if (Mathf.Approximately(value.x, 0))
                {
                    if (_isStearing != false)
                        OnSwipeEnded(value);

                    return;
                }

                _isStearing = true;
                _stearInput = Mathf.Clamp(value.x, -_config.DeltaInputLimit, _config.DeltaInputLimit);
                _drivable?.Stear(_stearInput, _player.GetStat(ItemType.Wheel) * 2, _config.StearAngle, _roadWidth);

                //if (value < 0 && _currentRoadLineIndex == 0)
                //    return;

                //if (value > 0 && _currentRoadLineIndex == _roadLines.Length - 1)
                //    return;

                //_currentRoadLineIndex += (int)value;
                //_currentRoadLineIndex = Mathf.Clamp(_currentRoadLineIndex, 0, _roadLines.Length - 1);
                //_drivable?.ChangeLine(_roadLines[_currentRoadLineIndex], _config.StearDuration 
                //    / _player.GetStat(ItemType.Wheel), value * _config.StearAngle);
            }
        }

        private void OnSwipeEnded(Vector2 value)
        {
            _isStearing = false;
            _drivable?.EndStear();
        }

        public override void Restart()
        {
            _drivable.SetToLine(_roadLines[_currentRoadLineIndex]);
        }

        private void RegulateGas(float value)
        {
            if (_isGasregulationEnabled == false)
                return;

            if (_gameState.Current == GameStates.Run)
            {
                if (value == _maneuverDirection)
                    return;

                if (_isMeneuver == false && _maneuverCooldown > 0)
                    return;

                if (_isMeneuver && value != _maneuverDirection)
                {
                    _coroutiner.StopCoroutine(_gasRegulationRoutine);

                    if (value < 0)
                        _drivable?.Break();

                    _coroutiner.StartCoroutine(ResetGasRoutine());
                    return;
                }

                float target = 0;

                if (value > 0)
                    target = _config.GasRegulationRange.y * _player.GetStat(ItemType.Accelerator);

                if (value < 0)
                    target = _config.GasRegulationRange.x / _player.GetStat(ItemType.Brakes);

                _maneuverDirection = value;
                _gasRegulationRoutine = _coroutiner.StartCoroutine(GasManeuverRoutine(target));
                OnGasRegulated?.Invoke((int)_maneuverDirection);
            }
        }

        public override void Tick()
        {
            if (_gameState.Current == GameStates.Run)
            {
                if (_isMeneuver == false)
                    _maneuverCooldown -= Time.deltaTime;
                
                _cameraShake.m_AmplitudeGain = _gasValue / 100;
                _gasValue = _config.Speed * _player.GetStat(ItemType.Engine) * _gasRegulation;
                _drivable?.Accelerate(_gasValue * Time.deltaTime);

                if (_isStearing == false)
                    _drivable?.ResetStear();

                return;
            }
        }

        private IEnumerator GasManeuverRoutine(float target)
        {
            _isMeneuver = true;

            if (target < _gasRegulation)
            {
                _drivable?.Break();
                _drivable?.FireStop();
            }

            if (target > _gasRegulation)
                _drivable?.Fire();

            while (Mathf.Approximately(_gasRegulation, target) == false)
            {
                _gasRegulation = Mathf.MoveTowards(_gasRegulation, target, _config.Speed / 10 * Time.deltaTime);
                yield return null;
            }

            _drivable?.EndBreak();
            float time = _config.ManeuverTime <= -1 ? float.PositiveInfinity : _config.ManeuverTime;
            yield return new WaitForSeconds(time);
            yield return ResetGasRoutine();
        }

        public IEnumerator ResetGasRoutine()
        {
            _drivable?.FireStop();

            while (Mathf.Approximately(_gasRegulation, 1) == false)
            {
                _gasRegulation = Mathf.MoveTowards(_gasRegulation, 1, _config.Speed / 10 * Time.deltaTime);
                yield return null;
            }

            _drivable?.EndBreak();
            _maneuverCooldown = _config.ManeuverCooldown;
            _maneuverDirection = 0;
            _isMeneuver = false;
        }

        public void OnSateSwitched(GameStates state)
        {
            switch (state)
            {
                case GameStates.Run:
                _coroutiner.StartCoroutine(AccelerationRoutine());
                _coroutiner.StartCoroutine(ResetGasRoutine());
                break;
                case GameStates.WaitForRun:
                case GameStates.Lose:
                case GameStates.Finish:
                _canDrive = false;
                _drivable?.Accelerate(0);
                _cameraShake.m_AmplitudeGain = 0;
                _drivable?.FireStop();
                break;
            }
        }

        private IEnumerator AccelerationRoutine()
        {
            _canDrive = false;
            _drivable?.Break();
            yield return new WaitForSeconds(0.7f);
            _drivable?.EndBreak();
            _canDrive = true;
        }

        public override void OnDisable()
        {
            _playerInput.OnSwipe -= OnSwipe;
            _playerInput.OnSwipeEnded -= OnSwipeEnded;
            _playerInput.OnVerticalSwipeWithThreshold -= RegulateGas;
            _gameState.OnSwitched -= OnSateSwitched;
            _drivable.Accelerate(0);
        }

        public void DisableGasRegulation()
        {
            _isGasregulationEnabled = false;
        }

        public void EnableGasRegulation()
        {
            _isGasregulationEnabled = true;
        }
    }
}

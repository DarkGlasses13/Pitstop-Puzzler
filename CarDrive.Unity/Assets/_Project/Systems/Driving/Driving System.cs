using Assets._Project.Architecture;
using Assets._Project.GameStateControl;
using Assets._Project.Helpers;
using Assets._Project.Input;
using Assets._Project.Systems.Collecting;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets._Project.Systems.Driving
{
    public class DrivingSystem : GameSystem
    {
        private readonly LocalAssetLoader _assetLoader;
        private readonly IPlayerInput _playerInput;
        private readonly IDrivable _drivable;
        private readonly Player _player;
        private readonly GameState _gameState;
        private readonly Coroutiner _coroutiner;
        private DrivingConfig _config;
        private int _currentRoadLineIndex;
        private float _gasValue;
        private Coroutine _gasRegulationRoutine;
        private float _gasRegulation = 1;
        private float[] _roadLines;
        private bool _isMeneuver;
        private float _maneuverCooldown;
        private float _maneuverDirection;

        public DrivingSystem(LocalAssetLoader assetLoader, IPlayerInput playerInput, IDrivable drivable,
            Player player, GameState gameState, Coroutiner coroutiner)
        {
            _assetLoader = assetLoader;
            _playerInput = playerInput;
            _drivable = drivable;
            _player = player;
            _gameState = gameState;
            _coroutiner = coroutiner;
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
        }

        public override void Enable()
        {
            _playerInput.OnStear += Stear;
            _playerInput.OnGasRegulate += RegulateGas;
        }

        public override void Restart()
        {
            _drivable.SetToLine(_roadLines[_currentRoadLineIndex]);
        }

        private void RegulateGas(float value)
        {
            if (_gameState.Current == GameStates.Run)
            {
                if (value == _maneuverDirection)
                    return;

                if (_isMeneuver == false && _maneuverCooldown > 0)
                    return;

                if (_isMeneuver && value != _maneuverDirection)
                {
                    _coroutiner.StopCoroutine(_gasRegulationRoutine);
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
            }
        }

        private void Stear(float value)
        {
            if (_gameState.Current == GameStates.Run)
            {
                _currentRoadLineIndex += (int)value;
                _currentRoadLineIndex = Mathf.Clamp(_currentRoadLineIndex, 0, _roadLines.Length - 1);
                _drivable?.ChangeLine(_roadLines[_currentRoadLineIndex], _config.StearDuration 
                    / _player.GetStat(ItemType.Wheel), _config.StearAngle);
            }
        }

        public override void Tick()
        {
            if (_gameState.Current == GameStates.Run)
            {
                if (_isMeneuver == false)
                    _maneuverCooldown -= Time.deltaTime;

                _gasValue = _config.Speed * _player.GetStat(ItemType.Engine) * _gasRegulation;
                _drivable?.Accelerate(_gasValue * Time.deltaTime);
                return;
            }

            _drivable.Accelerate(0);
        }

        private IEnumerator GasManeuverRoutine(float target)
        {
            _isMeneuver = true;

            while (Mathf.Approximately(_gasRegulation, target) == false)
            {
                _gasRegulation = Mathf.MoveTowards(_gasRegulation, target, _config.Speed * Time.deltaTime);
                yield return null;
            }

            float time = _config.ManeuverTime <= -1 ? float.PositiveInfinity : _config.ManeuverTime;
            yield return new WaitForSeconds(time);
            yield return ResetGasRoutine();
        }

        public IEnumerator ResetGasRoutine()
        {
            while (Mathf.Approximately(_gasRegulation, 1) == false)
            {
                _gasRegulation = Mathf.MoveTowards(_gasRegulation, 1, _config.Speed * Time.deltaTime);
                yield return null;
            }

            _maneuverCooldown = _config.ManeuverCooldown;
            _maneuverDirection = 0;
            _isMeneuver = false;
        }

        public override void Disable()
        {
            _playerInput.OnStear -= Stear;
            _playerInput.OnGasRegulate -= RegulateGas;
        }
    }
}

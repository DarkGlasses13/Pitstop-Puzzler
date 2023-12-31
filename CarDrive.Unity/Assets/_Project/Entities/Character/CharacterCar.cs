using Assets._Project.Systems.Collecting;
using Assets._Project.Systems.Damage;
using Assets._Project.Systems.Driving;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Assets._Project.Entities.Character
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class CharacterCar : Entity, IDrivable, IDamageable, ICanCollectItems
    {
        [SerializeField] private ParticleSystem 
            _explosionParticle,
            _smokeParticle,
            _windParticle,
            _leftFire,
            _leftSmoke,
            _rightFire,
            _rightSmoke,
            _impregnabilityAura,
            _collectParticle,
            _moneyLoseParticle,
            _crashParticle;

        [SerializeField] private TrailRenderer 
            _leftWheelTrailRenderer,
            _rightWheelTrailRenderer;

        [SerializeField] private AudioSource
            _engineSound,
            _crashSound,
            _repairSound,
            _breakSound,
            _collectSound;

        private Rigidbody _rigidbody;
        private TweenerCore<Vector3, Vector3, VectorOptions> _moveTween;
        private Tweener _rotationTween;
        private float _stearLerp = 0;
        private bool _isBreaking;
        private Vector2 _roadWidth;

        public Vector3 Center => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void SetToLine(float position)
        {
            Vector3 linePosition = new(position, 0.026f, transform.position.z);
            Quaternion rotation = Quaternion.identity;
            transform.SetPositionAndRotation(linePosition, rotation);
        }

        public void ChangeLine(float line, float duration, float stearAngle)
        {
            _moveTween?.Kill();
            _rotationTween?.Kill();
            transform.rotation = Quaternion.identity;
            Break();
            _moveTween = transform.DOMoveX(line, duration).OnComplete(() => EndBreak());
            _rotationTween = transform.DOPunchRotation(Vector3.up * stearAngle, duration);
            _moveTween?.Play();
            _rotationTween?.Play();
        }

        public void Stear(float value, float speed, float stearAngle, Vector2 roadWidth)
        {
            //_stear = Mathf.Lerp(_stear, clampedValue * speed, _stearLerp);
            //transform.position += clampedValue * speed * Time.deltaTime * Vector3.right;

            _roadWidth = roadWidth;

            float rotation = Mathf.Clamp(value, -stearAngle, stearAngle);
            transform.Rotate(rotation * (speed * 3) * Time.deltaTime * Vector3.up);

            if (Mathf.Abs(rotation) >= 10 && _isBreaking == false)
                Break();

            if (Mathf.Abs(rotation) < 10 && _isBreaking)
                EndBreak();

            //_stearLerp += 0.1f * Time.deltaTime;

            //if (_stearLerp >= 1)
            //    _stearLerp = 0;
        }

        public void EndStear()
        {
            _stearLerp = 0;
        }

        public void ResetStear()
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, _stearLerp);
            _stearLerp += 3f * Time.deltaTime;

            if (_stearLerp >= 1)
                _stearLerp = 0;
        }

        public void EndBreak()
        {
            _isBreaking = false;
            _leftWheelTrailRenderer.emitting = false;
            _rightWheelTrailRenderer.emitting = false;
        }

        public void Break()
        {
            _isBreaking = true;

            if (_breakSound)
                _breakSound.Play();

            _leftWheelTrailRenderer.emitting = true;
            _rightWheelTrailRenderer.emitting = true;
        }

        public void Fire()
        {
            if (_leftFire)
                _leftFire.Play();
            if (_leftSmoke) 
                _leftSmoke.Play();

            if (_rightFire)
                _rightFire.Play();

            if (_rightSmoke) 
                _rightSmoke.Play();
        }

        public void FireStop()
        {
            _leftFire.Stop();
            _leftSmoke.Stop();
            _rightFire.Stop();
            _rightSmoke.Stop();
        }

        public void Accelerate(float acceleration)
        {
            if (gameObject == false)
                return;

            if (_windParticle && _windParticle.isPlaying && acceleration < 0.5f)
                _windParticle.Stop();

            if (_windParticle && _windParticle.isPlaying == false && acceleration > 0.5f)
                _windParticle.Play();

            if (_engineSound)
                _engineSound.pitch = acceleration / 1.5f;

            if (_windParticle)
            {
                ParticleSystem.MainModule windParticle = _windParticle.main;
                windParticle.maxParticles = (int)(acceleration * 50);
                windParticle.simulationSpeed = acceleration * 4;
            }

            transform.position += transform.forward * acceleration;
        }

        public void OnDie()
        {
            _engineSound.Stop();

            if (_crashSound)
                _crashSound.Play();

            EndBreak();
            FireStop();

            if (_explosionParticle)
                _explosionParticle.Play();

            if (_smokeParticle)
                _smokeParticle.Play();

            _rigidbody.isKinematic = false;
            _moveTween?.Kill();
            _rigidbody.AddForce(Vector3.up * 10000, ForceMode.Impulse);
            transform.DOPunchScale(Vector3.one * 2, 0.25f).Play().SetAutoKill(true);
        }

        public void OnCrash()
        {
            if (_crashSound)
                _crashSound.Play();

            EndBreak();
            FireStop();
            _moveTween?.Kill();
            transform.DOPunchScale(Vector3.one * 2, 0.25f).Play().SetAutoKill(true);

            if (_crashParticle)
                _crashParticle.Play();
        }

        public void OnMoneyLose()
        {
            _moneyLoseParticle.Play();
        }

        public void OnRestore()
        {
            if (_engineSound)
                _engineSound.Play();

            if (_repairSound)
                _repairSound.Play();

            _rigidbody.isKinematic = true;
            _smokeParticle.Stop();
        }

        public void OnCollect()
        {
            if (_collectSound && _collectSound.enabled)
                _collectSound.Play();

            if (_collectParticle)
                _collectParticle.Play();
        }

        public void ShowAura()
        {
            _impregnabilityAura.Play();
        }

        public void HideAura()
        {
            _impregnabilityAura.Stop();
        }

        private void Update()
        {
            if (transform.position.x >= _roadWidth.y)
                transform.position = new(_roadWidth.y, transform.position.y, transform.position.z);

            if (transform.position.x <= _roadWidth.x)
                transform.position = new(_roadWidth.x, transform.position.y, transform.position.z);
        }
    }
}
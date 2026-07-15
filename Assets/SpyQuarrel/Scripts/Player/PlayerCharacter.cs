using UnityEngine;
using KinematicCharacterController;

namespace SpyQuarrelRuntime
{
    public class PlayerCharacter : MonoBehaviour, ICharacterController, IAnimatorContext
    {
        [Header("Initial References")]
        [Space(5)]
        [SerializeField] private Transform _cameraTarget;

        public KinematicCharacterMotor Motor => _motor;
        [SerializeField] private KinematicCharacterMotor _motor;

        [SerializeField] private Transform _root;

        [SerializeField] private bool _holdJumpAllowed = true;

        [Header("Movement Inputs")]
        [SerializeField] private Quaternion _requestedRotation;
        [SerializeField] private Vector3 _requestedMovement;
        [SerializeField] private bool _requestedJump;
        [SerializeField] private bool _requestedSustainedJump;
        [SerializeField] private bool _requestedCrouch;
        [SerializeField] private bool _requestedCrouchInAir;

        [SerializeField] private bool _jumpHeldLastFrame;
        

        [Space(5)]
        [Header("Move Speed Variables")]
        [SerializeField] private float _walkSpeed = 20f;
        [SerializeField] private float _jumpSpeed = 10f;
        [SerializeField] private float _jumpWindowTime = 0.2f;
        [SerializeField, Range(0, 1f)] private float _jumpSustainMultiplier = 0.5f;
        [SerializeField] private float _crouchSpeed = 7f;

        [Header("Move Speed Smoothing/Acceleration")]
        [SerializeField] private float _walkSmoothing = 25f;
        [SerializeField] private float _crouchSmoothing = 20f;
        [SerializeField] private AnimationCurve _movementCurve;

        [Header("Air Control Settings")]
        [SerializeField] private float _airControlSpeed = 7f;
        [SerializeField] private float _airAccelSpeed = 70f;
        [SerializeField] private AnimationCurve _airControlCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Space(5)]
        [Header("Player Stance Heights")]
        [SerializeField] private float _standHeight = 2f;
        [SerializeField] private float _crouchHeight = 1f;

        [Header("Slide Settings")]
        [SerializeField] private float _slideStartSpeed = 25f;
        [SerializeField] private float _slideEndSpeed = 15f;
        [SerializeField] private float _slideFriction = 0.8f;
        [SerializeField] private float _slideSteerAcceleration = 5f;
        [SerializeField] private float _slideGravity = 90f;

        [Space(5)]
        [Header("Player Stance Transition Smoothing")]
        [SerializeField] private float _crouchTransitionSpeed = 0.2f;
        [SerializeField] private float _standTransitionSpeed = 0.2f;
        [SerializeField] private AnimationCurve _transitionCurve;

        [Space(5)]
        [Header("Camera Heights")]
        [SerializeField, Range(0, 1)] private float _standCameraHeight = 0.9f;
        [SerializeField, Range(0, 1)] private float _crouchCameraHeight = 0.7f;

        [Header("Player State")]
        [SerializeField] private PlayerState _currentState;
        [SerializeField] private PlayerState _previousState;

        [Space(2)]
        [Header("Gravity Settings")]
        [SerializeField] private float _gravity = -90f;

        [SerializeField] private Collider[] _uncrouchOverlapResults;

        private float _timeSinceUngrounded = float.PositiveInfinity;
        private float _timeSinceJumpRequest = float.PositiveInfinity;

        public void Initialize()
        {
            
            
            _uncrouchOverlapResults = new Collider[8];

            _currentState.Stance = PlayerStance.Standing;
            _currentState.Grounded = false;

            _previousState = _currentState;

            SwitchStance(_currentState.Stance);

            _motor.CharacterController = this;
        }

        //remember to change this for disguise system
        public void UpdateInput(PlayerInputCommand input)
        {
            _requestedRotation = input.Rotation;

            _requestedMovement = GetRequestedMovement(input);

            bool jumpHeld = input.Jump;
            bool jumpPressedThisFrame = jumpHeld && !_jumpHeldLastFrame;

            if (_holdJumpAllowed)
            {
                _requestedJump = jumpHeld;

                if (jumpHeld)
                {
                    _timeSinceJumpRequest = 0f;
                }
                else
                {
                    _timeSinceJumpRequest = float.PositiveInfinity;
                }
            }
            else
            {
                if (jumpPressedThisFrame)
                {
                    _requestedJump = true;
                    _timeSinceJumpRequest = 0f;
                }

                if (!jumpHeld && !_requestedJump)
                {
                    _timeSinceJumpRequest = float.PositiveInfinity;
                }
            }

            _requestedSustainedJump = jumpHeld;

            _jumpHeldLastFrame = jumpHeld;

            var wasRequestingCrouch = _requestedCrouch;

            _requestedCrouch = input.Crouch switch
            {
                CrouchType.Toggle => !_requestedCrouch,
                CrouchType.None => _requestedCrouch,
                _ => _requestedCrouch
            };

            if (_requestedCrouch && !wasRequestingCrouch)
            {
                _requestedCrouchInAir = !_currentState.Grounded;
            }
            else if (!_requestedCrouch && wasRequestingCrouch)
            {
                _requestedCrouchInAir = false;
            }
        }

        public void UpdateBody(float deltaTime)
        {
            var currentHeight = _motor.Capsule.height;

            var cameraTargetHeight = currentHeight * (_currentState.Stance == PlayerStance.Standing ? _standCameraHeight : _crouchCameraHeight);
            var normalizedHeight = currentHeight / _standHeight;

            var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);
            var currentScale = _root.localScale;
            var currentPos = _cameraTarget.localPosition;
            var targetPos = new Vector3(0, cameraTargetHeight, 0);

            var transitionSpeed = _currentState.Stance switch
            {
                PlayerStance.Crouching => _crouchTransitionSpeed,
                PlayerStance.Sliding => _crouchTransitionSpeed,
                PlayerStance.Standing => _standTransitionSpeed,
                _ => _crouchTransitionSpeed
            };

            float t = 1f - Mathf.Exp(-transitionSpeed * deltaTime);
            t = _transitionCurve.Evaluate(t);

            _cameraTarget.localPosition = Vector3.Lerp(currentPos, targetPos, t);
            _root.localScale = Vector3.Lerp(currentScale, rootTargetScale, t);
        }

        private Vector3 GetRequestedMovement(PlayerInputCommand input)
        {
            var move = new Vector3(input.Movement.x, 0f, input.Movement.y);

            move = Vector3.ClampMagnitude(move, 1f);

            move = input.Rotation * move;

            move = Vector3.ProjectOnPlane(move, _motor.CharacterUp);

            return move;
        }

        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Debug.Log($"{Time.time} UpdateRotation");
            currentRotation = GetFlatGroundRotation();
        }

        public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Debug.Log($"{Time.time} UpdateVelocity");

            bool grounded = _motor.GroundingStatus.IsStableOnGround;

            if (grounded)
            {
                _timeSinceUngrounded = 0f;
                MoveOnGround(ref currentVelocity, deltaTime);
            }
            else
            {
                _timeSinceUngrounded += deltaTime;
                MoveInAir(ref currentVelocity, deltaTime);
            }

            if (_requestedJump)
            {
                _requestedCrouch = false;
                _requestedCrouchInAir = false;

                _timeSinceJumpRequest += deltaTime;

                bool canUseCoyoteTime = !grounded && _timeSinceUngrounded <= _jumpWindowTime;
                bool canUseJumpBuffer = _holdJumpAllowed || _timeSinceJumpRequest <= _jumpWindowTime;

                if ((grounded || canUseCoyoteTime) && canUseJumpBuffer)
                {
                    JumpRequest(ref currentVelocity);
                }
                else if (!_holdJumpAllowed && _timeSinceJumpRequest > _jumpWindowTime)
                {
                    _requestedJump = false;
                    _timeSinceJumpRequest = float.PositiveInfinity;
                }
            }
        }

        private void JumpRequest(ref Vector3 currentVelocity)
        {
            if (!_holdJumpAllowed)
            {
                _requestedJump = false;
                _timeSinceJumpRequest = float.PositiveInfinity;
            }

            _timeSinceUngrounded = _jumpWindowTime + 1f;

            _motor.ForceUnground(time: 0.1f);

            var currentVerticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, _jumpSpeed);

            currentVelocity += _motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
        }

        private void MoveInAir(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_requestedMovement.sqrMagnitude > 0.001f)
            {
                var movementOnPlane = Vector3.ProjectOnPlane(_requestedMovement, _motor.CharacterUp).normalized * _requestedMovement.magnitude;

                var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);

                var movementForce = movementOnPlane * (_airAccelSpeed * deltaTime);

                if (currentPlanarVelocity.magnitude < _airControlSpeed)
                {
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, _airControlSpeed);

                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0)
                {
                    var constrainedMovementForce = Vector3.ProjectOnPlane(movementForce, currentPlanarVelocity.normalized);

                    movementForce = constrainedMovementForce;
                }

                if (_motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0)
                    {
                        var obstructionNormal = Vector3.Cross
                        (
                            _motor.CharacterUp,
                            Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal)
                        ).normalized;

                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }

                currentVelocity += movementForce;
            }

            var effectiveGravity = _gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);

            if (_requestedSustainedJump && verticalSpeed > 0)
            {
                effectiveGravity *= _jumpSustainMultiplier;
            }

            currentVelocity += _motor.CharacterUp * (effectiveGravity * deltaTime);
        }

        private void MoveOnGround(ref Vector3 currentVelocity, float deltaTime)
        {
            var groundedMovement = _motor.GetDirectionTangentToSurface
            (
                direction: _requestedMovement,
                surfaceNormal: _motor.GroundingStatus.GroundNormal
            ) * _requestedMovement.magnitude;

            var moving = groundedMovement.sqrMagnitude > 0.001f;
            var crouching = _currentState.Stance == PlayerStance.Crouching;
            var wasStanding = _previousState.Stance == PlayerStance.Standing;
            var wasInAir = !_previousState.Grounded;

            if (moving && crouching && (wasStanding || wasInAir))
            {
                Debug.DrawRay(transform.position, currentVelocity, Color.red, 4f);
                Debug.DrawRay(transform.position, _previousState.Velocity, Color.green, 4f);

                SwitchStance(PlayerStance.Sliding);

                if (wasInAir)
                {
                    var previousVelocity = _previousState.Velocity;
                    var normal = _motor.GroundingStatus.GroundNormal;

                    currentVelocity = Vector3.ProjectOnPlane(previousVelocity, normal);
                }

                var effectiveSlideStartSpeed = _slideStartSpeed;

                if (!_previousState.Grounded && !_requestedCrouchInAir)
                {
                    effectiveSlideStartSpeed = 0;
                    _requestedCrouchInAir = false;
                }

                var slideSpeed = Mathf.Max(effectiveSlideStartSpeed, currentVelocity.magnitude);

                currentVelocity = _motor.GetDirectionTangentToSurface
                (
                    direction: currentVelocity,
                    surfaceNormal: _motor.GroundingStatus.GroundNormal
                );

                currentVelocity *= slideSpeed;

                Debug.DrawRay(transform.position, currentVelocity, Color.green, 4f);
            }

            if (_currentState.Stance is PlayerStance.Standing or PlayerStance.Crouching)
            {
                var speed = GetStanceSpeed();
                var accel = GetStanceAcceleration();
                var t = 1f - Mathf.Exp(-accel * deltaTime);

                var targetVelocity = groundedMovement * speed;

                currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, t);
            }
            else if (_currentState.Stance == PlayerStance.Sliding)
            {
                currentVelocity -= currentVelocity * (_slideFriction * deltaTime);

                // slopes
                {
                    var force = Vector3.ProjectOnPlane(-_motor.CharacterUp, _motor.GroundingStatus.GroundNormal);
                    force *= _slideGravity;

                    currentVelocity += force * deltaTime;
                }

                // steering
                if (moving)
                {
                    var currentSpeed = currentVelocity.magnitude;
                    var targetDirection = groundedMovement.normalized;
                    var targetVelocity = targetDirection * currentSpeed;

                    var steerForce = (targetVelocity - currentVelocity) * (_slideSteerAcceleration * deltaTime);

                    currentVelocity += steerForce;
                    currentVelocity = Vector3.ClampMagnitude(currentVelocity, currentSpeed);
                }

                if (currentVelocity.magnitude < _slideEndSpeed)
                {
                    SwitchStance(PlayerStance.Crouching);
                }
            }
        }

        private float GetStanceAcceleration()
        {
            return _currentState.Stance switch
            {
                PlayerStance.Standing => _walkSmoothing,
                PlayerStance.Crouching => _crouchSmoothing,
                PlayerStance.Sliding => _crouchSmoothing,
                _ => _walkSmoothing
            };
        }

        private float GetStanceSpeed()
        {
            return _currentState.Stance switch
            {
                PlayerStance.Standing => _walkSpeed,
                PlayerStance.Crouching => _crouchSpeed,
                PlayerStance.Sliding => _crouchSpeed,
                _ => _walkSpeed
            };
        }

        public virtual void BeforeCharacterUpdate(float deltaTime)
        {
            _previousState = _currentState;

            if (_requestedCrouch && _currentState.Stance == PlayerStance.Standing)
            {
                SwitchStance(PlayerStance.Crouching);
            }
        }

        public virtual void PostGroundingUpdate(float deltaTime)
        {
            if (!_motor.GroundingStatus.IsStableOnGround && _currentState.Stance == PlayerStance.Sliding)
            {
                SwitchStance(PlayerStance.Crouching);
            }
        }

        public virtual void AfterCharacterUpdate(float deltaTime)
        {
            if (!_requestedCrouch && _currentState.Stance != PlayerStance.Standing)
            {
                SwitchStance(PlayerStance.Standing);
            }

            _currentState.Grounded = _motor.GroundingStatus.IsStableOnGround;
            _currentState.Velocity = _motor.Velocity;
        }

        private void SwitchStance(PlayerStance newStance)
        {
            switch (newStance)
            {
                case PlayerStance.Standing:
                    HandleSwitchToStanding();
                    break;

                case PlayerStance.Crouching:
                    HandleSwitchToCrouching();
                    break;

                case PlayerStance.Sliding:
                    HandleSwitchToSliding();
                    break;

                default:
                    HandleSwitchToStanding();
                    break;
            }
        }

        private void HandleSwitchToStanding()
        {
            _motor.SetCapsuleDimensions
            (
                radius: _motor.Capsule.radius,
                height: _standHeight,
                yOffset: _standHeight * 0.5f
            );

            if (IsHeadBlocked())
            {
                _requestedCrouch = true;
                HandleSwitchToCrouching();
            }
            else
            {
                _currentState.Stance = PlayerStance.Standing;
            }
        }

        private void HandleSwitchToSliding()
        {
            _currentState.Stance = PlayerStance.Sliding;

            _motor.SetCapsuleDimensions
            (
                radius: _motor.Capsule.radius,
                height: _crouchHeight,
                yOffset: _crouchHeight * 0.5f
            );
        }

        private void HandleSwitchToCrouching()
        {
            _currentState.Stance = PlayerStance.Crouching;

            _motor.SetCapsuleDimensions
            (
                radius: _motor.Capsule.radius,
                height: _crouchHeight,
                yOffset: _crouchHeight * 0.5f
            );
        }

        private bool IsHeadBlocked()
        {
            var pos = _motor.TransientPosition;
            var rot = _motor.TransientRotation;
            var mask = _motor.CollidableLayers;

            return _motor.CharacterOverlap(pos, rot, _uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0;
        }

        public virtual bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public virtual void ProcessHitStabilityReport
        (
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation,
            ref HitStabilityReport hitStabilityReport
        )
        {
        }

        public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        public Transform GetCameraTarget() => _cameraTarget;

        private Quaternion GetFlatGroundRotation()
        {
            Vector3 forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward, _motor.CharacterUp);

            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.ProjectOnPlane(transform.forward, _motor.CharacterUp);
            }

            return Quaternion.LookRotation(forward.normalized, _motor.CharacterUp);
        }
        
        public PlayerStatePayload GetPredictedState(int tick)
        {
            return new PlayerStatePayload()
            {
                Tick = tick,

                Position = _motor.TransientPosition,
                Rotation = _motor.TransientRotation,
                Velocity = _motor.Velocity,

                CurrentState = _currentState,
                PreviousState = _previousState,

                RequestedJump = _requestedJump,
                RequestedSustainedJump = _requestedSustainedJump,
                RequestedCrouch = _requestedCrouch,
                RequestedCrouchInAir = _requestedCrouchInAir,
                JumpHeldLastFrame = _jumpHeldLastFrame,

                TimeSinceUngrounded = _timeSinceUngrounded,
                TimeSinceJumpRequest = _timeSinceJumpRequest
            };
        }
        
        public void SetPredictedState(PlayerStatePayload state)
        {
            _motor.SetPositionAndRotation(state.Position, state.Rotation);

            _currentState = state.CurrentState;
            _previousState = state.PreviousState;

            _requestedJump = state.RequestedJump;
            _requestedSustainedJump = state.RequestedSustainedJump;
            _requestedCrouch = state.RequestedCrouch;
            _requestedCrouchInAir = state.RequestedCrouchInAir;
            _jumpHeldLastFrame = state.JumpHeldLastFrame;

            _timeSinceUngrounded = state.TimeSinceUngrounded;
            _timeSinceJumpRequest = state.TimeSinceJumpRequest;

            SwitchStance(_currentState.Stance);
        }
        
       

        public void SetImpliedState(PlayerImpliedStatePayload state)
        {
            _motor.SetPositionAndRotation(
                state.Position,
                Quaternion.Euler(0f, state.Yaw, 0f)
            );

            _currentState = new PlayerState
            {
                Grounded = state.Grounded,
                Stance = state.Stance,
                Velocity = state.Velocity
            };

            _previousState = _currentState;

            _requestedJump = false;
            _requestedSustainedJump = false;
            _requestedCrouch = state.Stance != PlayerStance.Standing;
            _requestedCrouchInAir = false;
            _jumpHeldLastFrame = false;

            _timeSinceUngrounded = state.Grounded ? 0f : _jumpWindowTime + 1f;
            _timeSinceJumpRequest = float.PositiveInfinity;

            SwitchStance(_currentState.Stance);
        }

        public void Teleport(Vector3 position)
        {
            _motor.SetPosition(position);
        }

        // private Quaternion GetInputBasedDirection(Quaternion currentRotation, float deltaTime, float smoothSpeed)
        // {
        //     Vector2 move = _requestedInput;
        //
        //     if (move.sqrMagnitude <= 0.001f)
        //     {
        //         return currentRotation;
        //     }
        //
        //     Vector3 forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward, _motor.CharacterUp).normalized;
        //     Vector3 right = Vector3.Cross(_motor.CharacterUp, forward).normalized;
        //
        //     Vector3 moveDirection = forward * move.y + right * move.x;
        //
        //     if (moveDirection.sqrMagnitude <= 0.001f)
        //     {
        //         return currentRotation;
        //     }
        //
        //     moveDirection = Vector3.ProjectOnPlane(moveDirection, _motor.CharacterUp).normalized;
        //
        //     Quaternion targetRotation = Quaternion.LookRotation(moveDirection, _motor.CharacterUp);
        //
        //     float t = 1f - Mathf.Exp(-smoothSpeed * deltaTime);
        //
        //     return Quaternion.Slerp(currentRotation, targetRotation, t);
        // }
        public Vector3 Velocity => _motor.Velocity;
        public float Speed => _motor.BaseVelocity.magnitude;
        public Vector3 ForwardDirection => _motor.CharacterForward;
    }
}
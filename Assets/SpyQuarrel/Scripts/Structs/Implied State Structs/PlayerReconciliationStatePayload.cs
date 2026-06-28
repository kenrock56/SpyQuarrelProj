using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Serializable]
    public struct PlayerReconciliationStatePayload : INetworkSerializable
    {
        public int Tick;

        public Vector3 Position;
        public float Yaw;

        public Vector3 Velocity;
        public Vector3 PreviousVelocity;

        public PlayerStance CurrentStance;
        public PlayerStance PreviousStance;

        public bool CurrentGrounded;
        public bool PreviousGrounded;

        public bool RequestedJump;
        public bool RequestedSustainedJump;
        public bool RequestedCrouch;
        public bool RequestedCrouchInAir;
        public bool JumpHeldLastFrame;

        public float TimeSinceUngrounded;
        public float TimeSinceJumpRequest;

        public static PlayerReconciliationStatePayload FromFullState(PlayerStatePayload state)
        {
            return new PlayerReconciliationStatePayload
            {
                Tick = state.Tick,

                Position = state.Position,
                Yaw = state.Rotation.eulerAngles.y,

                Velocity = state.Velocity,
                PreviousVelocity = state.PreviousState.Velocity,

                CurrentStance = state.CurrentState.Stance,
                PreviousStance = state.PreviousState.Stance,

                CurrentGrounded = state.CurrentState.Grounded,
                PreviousGrounded = state.PreviousState.Grounded,

                RequestedJump = state.RequestedJump,
                RequestedSustainedJump = state.RequestedSustainedJump,
                RequestedCrouch = state.RequestedCrouch,
                RequestedCrouchInAir = state.RequestedCrouchInAir,
                JumpHeldLastFrame = state.JumpHeldLastFrame,

                TimeSinceUngrounded = state.TimeSinceUngrounded,
                TimeSinceJumpRequest = state.TimeSinceJumpRequest
            };
        }

        public PlayerStatePayload ReconcileToFull()
        {
            return new PlayerStatePayload
            {
                Tick = Tick,

                Position = Position,
                Rotation = Quaternion.Euler(0f, Yaw, 0f),
                Velocity = Velocity,

                CurrentState = new PlayerState
                {
                    Grounded = CurrentGrounded,
                    Stance = CurrentStance,
                    Velocity = Velocity
                },

                PreviousState = new PlayerState
                {
                    Grounded = PreviousGrounded,
                    Stance = PreviousStance,
                    Velocity = PreviousVelocity
                },

                RequestedJump = RequestedJump,
                RequestedSustainedJump = RequestedSustainedJump,
                RequestedCrouch = RequestedCrouch,
                RequestedCrouchInAir = RequestedCrouchInAir,
                JumpHeldLastFrame = JumpHeldLastFrame,

                TimeSinceUngrounded = TimeSinceUngrounded,
                TimeSinceJumpRequest = TimeSinceJumpRequest
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);

            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Yaw);

            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref PreviousVelocity);

            serializer.SerializeValue(ref CurrentStance);
            serializer.SerializeValue(ref PreviousStance);

            serializer.SerializeValue(ref CurrentGrounded);
            serializer.SerializeValue(ref PreviousGrounded);

            serializer.SerializeValue(ref RequestedJump);
            serializer.SerializeValue(ref RequestedSustainedJump);
            serializer.SerializeValue(ref RequestedCrouch);
            serializer.SerializeValue(ref RequestedCrouchInAir);
            serializer.SerializeValue(ref JumpHeldLastFrame);

            serializer.SerializeValue(ref TimeSinceUngrounded);
            serializer.SerializeValue(ref TimeSinceJumpRequest);
        }
    }
}
using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Serializable]
    public struct PlayerStatePayload : INetworkSerializable
    {
        public int Tick { get; set; }

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }

        public PlayerState CurrentState { get; set; }
        public PlayerState PreviousState { get; set; }

        public bool RequestedJump { get; set; }
        public bool RequestedSustainedJump { get; set; }
        public bool RequestedCrouch { get; set; }
        public bool RequestedCrouchInAir { get; set; }
        public bool JumpHeldLastFrame { get; set; }

        public float TimeSinceUngrounded { get; set; }
        public float TimeSinceJumpRequest { get; set; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            int tick = Tick;

            Vector3 position = Position;
            float yaw = Rotation.eulerAngles.y;
            Vector3 velocity = Velocity;

            PlayerState currentState = CurrentState;
            PlayerState previousState = PreviousState;

            bool requestedJump = RequestedJump;
            bool requestedSustainedJump = RequestedSustainedJump;
            bool requestedCrouch = RequestedCrouch;
            bool requestedCrouchInAir = RequestedCrouchInAir;
            bool jumpHeldLastFrame = JumpHeldLastFrame;

            float timeSinceUngrounded = TimeSinceUngrounded;
            float timeSinceJumpRequest = TimeSinceJumpRequest;

            serializer.SerializeValue(ref tick);

            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref yaw);
            serializer.SerializeValue(ref velocity);

            serializer.SerializeValue(ref currentState);
            serializer.SerializeValue(ref previousState);

            serializer.SerializeValue(ref requestedJump);
            serializer.SerializeValue(ref requestedSustainedJump);
            serializer.SerializeValue(ref requestedCrouch);
            serializer.SerializeValue(ref requestedCrouchInAir);
            serializer.SerializeValue(ref jumpHeldLastFrame);

            serializer.SerializeValue(ref timeSinceUngrounded);
            serializer.SerializeValue(ref timeSinceJumpRequest);

            Tick = tick;

            Position = position;
            Rotation = Quaternion.Euler(0, yaw, 0);
            Velocity = velocity;

            CurrentState = currentState;
            PreviousState = previousState;

            RequestedJump = requestedJump;
            RequestedSustainedJump = requestedSustainedJump;
            RequestedCrouch = requestedCrouch;
            RequestedCrouchInAir = requestedCrouchInAir;
            JumpHeldLastFrame = jumpHeldLastFrame;

            TimeSinceUngrounded = timeSinceUngrounded;
            TimeSinceJumpRequest = timeSinceJumpRequest;
        }

        // public bool Equals(PlayerStatePayload other)
        // {
        //     return Tick == other.Tick && Position.Equals(other.Position) && Rotation.Equals(other.Rotation) && Velocity.Equals(other.Velocity) && CurrentState.Equals(other.CurrentState) && PreviousState.Equals(other.PreviousState) && RequestedJump == other.RequestedJump && RequestedSustainedJump == other.RequestedSustainedJump && RequestedCrouch == other.RequestedCrouch && RequestedCrouchInAir == other.RequestedCrouchInAir && JumpHeldLastFrame == other.JumpHeldLastFrame && TimeSinceUngrounded.Equals(other.TimeSinceUngrounded) && TimeSinceJumpRequest.Equals(other.TimeSinceJumpRequest);
        // }
        //
        // public override bool Equals(object obj)
        // {
        //     return obj is PlayerStatePayload other && Equals(other);
        // }
        //
        // public override int GetHashCode()
        // {
        //     var hashCode = new HashCode();
        //     hashCode.Add(Tick);
        //     hashCode.Add(Position);
        //     hashCode.Add(Rotation);
        //     hashCode.Add(Velocity);
        //     hashCode.Add(CurrentState);
        //     hashCode.Add(PreviousState);
        //     hashCode.Add(RequestedJump);
        //     hashCode.Add(RequestedSustainedJump);
        //     hashCode.Add(RequestedCrouch);
        //     hashCode.Add(RequestedCrouchInAir);
        //     hashCode.Add(JumpHeldLastFrame);
        //     hashCode.Add(TimeSinceUngrounded);
        //     hashCode.Add(TimeSinceJumpRequest);
        //     return hashCode.ToHashCode();
        // }
    }
}
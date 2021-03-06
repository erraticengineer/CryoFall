﻿namespace AtomicTorch.CBND.CoreMod.Systems.WorldMapResourceMarks
{
    using System;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Scripting.Network;
    using AtomicTorch.GameEngine.Common.Primitives;

    public readonly struct WorldMapResourceMark
        : IRemoteCallParameter,
          IEquatable<WorldMapResourceMark>
    {
        public WorldMapResourceMark(Vector2Ushort position, IProtoStaticWorldObject protoWorldObject)
        {
            this.Position = position;
            this.ProtoWorldObject = protoWorldObject;
        }

        public Vector2Ushort Position { get; }

        public IProtoStaticWorldObject ProtoWorldObject { get; }

        public bool Equals(WorldMapResourceMark other)
        {
            return this.Position.Equals(other.Position)
                   && Equals(this.ProtoWorldObject, other.ProtoWorldObject);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is WorldMapResourceMark other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Position.GetHashCode() * 397)
                       ^ (this.ProtoWorldObject != null ? this.ProtoWorldObject.GetHashCode() : 0);
            }
        }
    }
}
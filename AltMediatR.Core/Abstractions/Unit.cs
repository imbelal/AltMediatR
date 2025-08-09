namespace AltMediatR.Core.Abstractions
{
    // Represents a void return in generic code (monadic Unit)
    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = default;
        public override string ToString() => "()";
        public bool Equals(Unit other) => true;
        public override bool Equals(object? obj) => obj is Unit;
        public override int GetHashCode() => 0;
    }
}

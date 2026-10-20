namespace VGoalsPlanner.Domain.Abstractions
{
    public record Error(string Code, string Description)
    {
        public static readonly Error None = new(string.Empty, string.Empty);

        public static readonly Error NullValue = new("Error.NullValue", "Было передано пустое (null) значение");

        public static implicit operator string(Error error) => error.Code;

        public override string ToString() => Code;
    }
}

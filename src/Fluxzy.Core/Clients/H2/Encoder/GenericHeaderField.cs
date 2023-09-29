// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.H2.Encoder
{
    public class GenericHeaderField
    {
        public GenericHeaderField(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }

        public static implicit operator GenericHeaderField(HeaderFieldInfo genericHeaderField) => new(genericHeaderField.Name.ToString(), genericHeaderField.Value.ToString());

        public static implicit operator GenericHeaderField(HeaderField genericHeaderField) => new(genericHeaderField.Name.ToString(), genericHeaderField.Value.ToString());
    }
}

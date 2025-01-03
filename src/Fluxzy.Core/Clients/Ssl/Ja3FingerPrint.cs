// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;

namespace Fluxzy.Clients.Ssl
{
    public class AdvancedTlsSettings
    {
        public AdvancedTlsSettings(Ja3FingerPrint? ja3FingerPrint)
        {
            Ja3FingerPrint = ja3FingerPrint;
        }

        public Ja3FingerPrint? Ja3FingerPrint { get;  }
    }
    
    public class Ja3FingerPrint
    {
        public Ja3FingerPrint(
            int protocolVersion,
            int[] ciphers,
            int[] clientExtensions,
            int[] supportGroups,
            int[] ellipticCurvesFormat)
        {
            ProtocolVersion = protocolVersion;
            Ciphers = ciphers;
            SupportGroups = supportGroups;
            EllipticCurvesFormat = ellipticCurvesFormat;
            ClientExtensions = clientExtensions;
            Flat = ToString();

            GreaseMode = ClientExtensions.Contains(0xFE0D); // 

            if (GreaseMode)
            {
                // Grease enable 
                EffectiveSupportGroups = new[] { 0x6A6A }.Concat(SupportGroups).ToArray();
                EffectiveClientExtensions = new[] { 56026 }.Concat(clientExtensions).ToArray();
                EffectiveClientExtensions = new[] { 56026 }.Concat(clientExtensions).ToArray();
                EffectiveCiphers = new[] { 0x8A8A }.Concat(Ciphers).ToArray();
            }
            else {
                EffectiveSupportGroups = SupportGroups;
                EffectiveClientExtensions = ClientExtensions;
                EffectiveCiphers = Ciphers;
            }
        }

        public int[] EffectiveCiphers { get; set; }

        /// <summary>
        /// As in wire format
        /// </summary>
        public int ProtocolVersion { get; }

        public int[] Ciphers { get; }

        public int[] ClientExtensions { get; }

        public int[] SupportGroups { get;  }


        public int[] EllipticCurvesFormat { get; }

        public string Flat { get; }

        public bool GreaseMode { get; set; } = true;

        public int[] EffectiveSupportGroups { get; }

        public int[] EffectiveClientExtensions { get; }

        public sealed override string ToString()
        {
            return $"{ProtocolVersion}," +
                   $"{string.Join("-", Ciphers)}," +
                   $"{string.Join("-", ClientExtensions)}," +
                   $"{string.Join("-", SupportGroups)}," +
                   $"{string.Join("-", EllipticCurvesFormat)}";
        }

        public string ToString(bool ordered)
        {
            if (!ordered)
            {
                return ToString();
            }

            return $"{ProtocolVersion}," +
                   $"{string.Join("-", Ciphers.OrderBy(c => c))}," +
                   $"{string.Join("-", ClientExtensions.OrderBy(c => c))}," +
                   $"{string.Join("-", SupportGroups)}," +
                   $"{string.Join("-", EllipticCurvesFormat.OrderBy(c => c))}";
        }

        protected bool Equals(Ja3FingerPrint other)
        {
            return Flat == other.Flat;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Ja3FingerPrint)obj);
        }

        public override int GetHashCode()
        {
            return Flat.GetHashCode();
        }

        public static bool operator ==(Ja3FingerPrint? left, Ja3FingerPrint? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Ja3FingerPrint? left, Ja3FingerPrint? right)
        {
            return !Equals(left, right);
        }

        public static Ja3FingerPrint Parse(string ja3)
        {
            var parts = ja3.Split(new[] {","}, StringSplitOptions.None);

            if (parts.Length != 5)
            {
                throw new ArgumentException("Invalid JA3 fingerprint format");
            }

            if (!int.TryParse(parts[0], out var protocolVersion)) 
            {
                throw new ArgumentException($"Invalid JA3 fingerprint format. TLS version non valid `{parts[0]}`");
            }

            var ciphers = parts[1]
                          .Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(int.Parse).ToArray();

            var clientExtensions = parts[2].Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(int.Parse).ToArray();

            var ellipticCurves = parts[3].Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(int.Parse).ToArray();

            var ellipticCurvesFormat = parts[4].Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(int.Parse).ToArray();

            return new Ja3FingerPrint(protocolVersion, ciphers, clientExtensions, ellipticCurves, ellipticCurvesFormat);
        }
    }
}

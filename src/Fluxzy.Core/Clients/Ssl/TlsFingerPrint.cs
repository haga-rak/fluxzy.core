// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl
{
    public class TlsFingerPrint
    {
        internal static readonly int GreaseLeadValue = 60138;
        internal static readonly int GreaseTrailValue = 64250;

        public TlsFingerPrint(
            int protocolVersion,
            int[] ciphers,
            int[] clientExtensions,
            int[] supportGroups,
            int[] ellipticCurvesFormat, bool ? greaseMode, 
            Dictionary<int, byte[]>? overrideClientExtensionsValues, 
            List<SignatureAndHashAlgorithm>? signatureAndHashAlgorithms)
        {
            ProtocolVersion = protocolVersion;
            Ciphers = ciphers;
            SupportGroups = supportGroups;
            EllipticCurvesFormat = ellipticCurvesFormat;
            OverrideClientExtensionsValues = overrideClientExtensionsValues;
            SignatureAndHashAlgorithms = signatureAndHashAlgorithms;
            ClientExtensions = clientExtensions;
            Ja3Flat = ToString();

            GreaseMode = ClientExtensions.Contains(0xFE0D);

            if (greaseMode != null) {
                GreaseMode = greaseMode.Value;
            }

            if (GreaseMode)
            {
                // Grease enable 
                EffectiveSupportGroups = new[] { 0x6A6A }.Concat(SupportGroups).ToArray();
                EffectiveClientExtensions = new[] { GreaseLeadValue }.Concat(clientExtensions).Concat(new[] { GreaseTrailValue }).ToArray();
                EffectiveCiphers = new[] { 0x8A8A }.Concat(Ciphers).ToArray();
            }
            else {
                EffectiveSupportGroups = SupportGroups;
                EffectiveClientExtensions = ClientExtensions;
                EffectiveCiphers = Ciphers;
            }
        }


        /// <summary>
        /// As in wire format
        /// </summary>
        public int ProtocolVersion { get; }

        /// <summary>
        /// Ciphers to be used
        /// </summary>
        public int[] Ciphers { get; }

        /// <summary>
        /// Client extensions to be used
        /// </summary>
        public int[] ClientExtensions { get; }

        /// <summary>
        /// Supported groups
        /// </summary>
        public int[] SupportGroups { get;  }

        /// <summary>
        /// Elliptic curves format
        /// </summary>
        public int[] EllipticCurvesFormat { get; }

        /// <summary>
        /// Flat JA3 fingerprint
        /// </summary>
        public string Ja3Flat { get; }

        /// <summary>
        /// Grease mode, true = ON
        /// </summary>
        public bool GreaseMode { get;  }

        /// <summary>
        /// Effective ciphers
        /// </summary>
        internal int[] EffectiveCiphers { get; set; }

        internal int[] EffectiveSupportGroups { get; }

        internal int[] EffectiveClientExtensions { get; }

        /// <summary>
        /// Override client extensions values (set manually)
        /// </summary>
        public Dictionary<int, byte[]>? OverrideClientExtensionsValues { get; }

        /// <summary>
        /// Signature and hash algorithms
        /// </summary>
        public List<SignatureAndHashAlgorithm>? SignatureAndHashAlgorithms { get; }

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

        protected bool Equals(TlsFingerPrint other)
        {
            return Ja3Flat == other.Ja3Flat;
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

            return Equals((TlsFingerPrint)obj);
        }

        public override int GetHashCode()
        {
            return Ja3Flat.GetHashCode();
        }

        public static bool operator ==(TlsFingerPrint? left, TlsFingerPrint? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TlsFingerPrint? left, TlsFingerPrint? right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ja3"></param>
        /// <param name="greaseMode">When null, greaseMode will be determined according to tls value.</param>
        /// <param name="overrideClientExtensionsValues">Instead of using the default built-in values for extension..</param>
        /// <param name="signatureAndHashAlgorithms"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TlsFingerPrint ParseFromJa3(string ja3, bool ? greaseMode = null, 
            Dictionary<int, byte[]>? overrideClientExtensionsValues = null, List<SignatureAndHashAlgorithm>? signatureAndHashAlgorithms = null)
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

            return new TlsFingerPrint(protocolVersion, ciphers, clientExtensions, ellipticCurves,
                ellipticCurvesFormat, greaseMode, overrideClientExtensionsValues, signatureAndHashAlgorithms);
        }
    }
}

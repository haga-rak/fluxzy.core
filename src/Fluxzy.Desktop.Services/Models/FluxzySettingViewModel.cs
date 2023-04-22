// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Text.Json.Serialization;

namespace Fluxzy.Desktop.Services.Models
{
    public class FluxzySettingViewModel
    {
        [JsonConstructor]
        public FluxzySettingViewModel()
        {
        }

        public FluxzySettingViewModel(FluxzySetting setting)
        {
            SetupListenInterfaces(setting);
        }

        public int Port { get; set; }

        public List<IPAddress> Addresses { get; set; } = new();

        public ListenType ListenType { get; set; }

        private void SetupListenInterfaces(FluxzySetting setting)
        {
            if (!setting.BoundPoints.Any()) {
                Port = 44344;

                Addresses = new List<IPAddress> {
                    IPAddress.Loopback
                };

                ListenType = ListenType.SelectiveAddress;

                return;
            }

            Port = setting.BoundPoints.Select(s => s.EndPoint.Port)
                          .GroupBy(p => p)
                          .OrderByDescending(p => p.Count())
                          .First().First();

            if (setting.BoundPoints.Any(a => a.EndPoint.Address.Equals(IPAddress.Any))
                || setting.BoundPoints.Any(a => a.EndPoint.Address.Equals(IPAddress.IPv6Any))) {
                Addresses = new List<IPAddress> {
                    IPAddress.Any,
                    IPAddress.IPv6Any
                };

                ListenType = ListenType.AllInterfaces;

                return;
            }

            Addresses = setting.BoundPoints.Select(s => s.EndPoint.ToIpEndPoint().Address)
                               .Distinct().ToList();

            ListenType = ListenType.SelectiveAddress;
        }

        public void ApplyToSetting(FluxzySetting target)
        {
            target.BoundPoints.Clear();

            if (ListenType == ListenType.AllInterfaces) {
                target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(
                    IPAddress.Any, 44344), true));

                target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(
                    IPAddress.IPv6Any, 44344), false));

                return;
            }

            if (!Addresses.Any()) {
                target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(
                    IPAddress.Loopback, 44344), true));

                return;
            }

            var first = true;

            foreach (var address in Addresses) {
                target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(address, Port), first));
                first = false;
            }
        }

        public void Update(FluxzySetting fluxzySetting)
        {
            ApplyToSetting(fluxzySetting);
        }
    }

    public enum ListenType
    {
        SelectiveAddress = 1,
        AllInterfaces = 2
    }
}

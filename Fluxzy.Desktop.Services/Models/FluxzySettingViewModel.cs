// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Net;
using Newtonsoft.Json;

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

        private void SetupListenInterfaces(FluxzySetting setting)
        {
            if (setting.BoundPoints.All(p => Equals(p.EndPoint.Address, IPAddress.IPv6Loopback) ||
                                             Equals(p.EndPoint.Address, IPAddress.Loopback))
                && setting.BoundPoints.Select(e => e.EndPoint.Port).Distinct().Count() == 1)
            {
                // Classic loop back configuration 
                Port = setting.BoundPoints.Select(e => e.EndPoint.Port).First();
                ListenType = ListenType.LocalHostOnly;
            }
            else
            {
                if (setting.BoundPoints.Any(p => Equals(p.EndPoint.Address, IPAddress.Any)
                                                 || Equals(p.EndPoint.Address, IPAddress.IPv6Any))
                    && setting.BoundPoints.Select(e => e.EndPoint.Port).Distinct().Count() == 1)
                {
                    Port = setting.BoundPoints.Select(e => e.EndPoint.Port).First();
                    ListenType = ListenType.AllInterfaces;
                }
                else
                {
                    ListenType = ListenType.SpecificInterface;

                    foreach (var boundPoint in setting.BoundPoints.OrderBy(t => !t.Default))
                    {
                        SpecificAddresses.Add(boundPoint.EndPoint);
                    }
                }
            }
        }
        
        public void ViewModelToSetting(FluxzySetting target)
        {
            target.BoundPoints.Clear();

            switch (ListenType)
            {
                case ListenType.LocalHostOnly:
                {
                    target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(IPAddress.Loopback, Port), true));
                    target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(IPAddress.IPv6Loopback, Port), false));
                    break; 
                }
                case ListenType.AllInterfaces:
                {
                    target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(IPAddress.Any, Port), true));
                    target.BoundPoints.Add(new ProxyBindPoint(new IPEndPoint(IPAddress.IPv6Any, Port), false));
                    break;
                }
                case ListenType.SpecificInterface:
                {
                    for (var index = 0; index < SpecificAddresses.Count; index++)
                    {
                        var endPoint = SpecificAddresses[index];
                        target.BoundPoints.Add(new ProxyBindPoint(endPoint, index == 0));
                    }

                    break;
                }
            }
        }

        public ListenType ListenType { get; set; } = ListenType.LocalHostOnly; 

        public int Port { get; set; }

        public List<IPEndPoint> SpecificAddresses { get; set; } = new();

        public void Update(FluxzySetting fluxzySetting)
        {
            ViewModelToSetting(fluxzySetting);
        }
    }


    public enum ListenType
    {
        LocalHostOnly = 1 , 
        AllInterfaces = 2, 
        SpecificInterface = 3 
    }
}

// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Echoes.Desktop.Common.Services
{
    public class SettingHolder
    {
        private readonly BehaviorSubject<ProxyStartupSetting> _startupSettingSubject 
            = new(DefaultSettingHelper.GetDefault());

        public SettingHolder()
        {
        }

        public IObservable<ProxyStartupSetting> GetStartupSetting()
        {
            return _startupSettingSubject.AsObservable(); 
        }
    }


    public static class DefaultSettingHelper
    {
        public static ProxyStartupSetting GetDefault()
        {
            var setting = ProxyStartupSetting.CreateDefault()
                .SetListenPort(10900)
                .SetArchivingPolicy(ArchivingPolicy.None)
                .SetAsSystemProxy(true)
                ;

            return setting; 
        }
    }

    
}
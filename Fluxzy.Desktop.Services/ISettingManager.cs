// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services
{
    public interface ISettingManager
    {
        Task Update(EchoesSettings settings);

        Task<EchoesSettings> Get();
    }
}
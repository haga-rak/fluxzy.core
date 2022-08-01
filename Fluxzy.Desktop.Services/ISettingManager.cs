// Copyright © 2022 Haga Rakotoharivelo

namespace Echoes.Desktop.Service
{
    public interface ISettingManager
    {
        Task Update(EchoesSettings settings);

        Task<EchoesSettings> Get();
    }
}
// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public interface IFileContentOperationManager
    {
        void AddOrUpdate(ExchangeInfo exchangeInfo);

        void Delete(FileContentDelete deleteOp);
    }
}

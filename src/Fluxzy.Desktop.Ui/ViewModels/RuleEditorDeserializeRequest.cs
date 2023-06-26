// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class RuleEditorDeserializeRequest
    {
        public RuleEditorDeserializeRequest(string content)
        {
            Content = content;
        }

        public string Content { get;  }
    }
}

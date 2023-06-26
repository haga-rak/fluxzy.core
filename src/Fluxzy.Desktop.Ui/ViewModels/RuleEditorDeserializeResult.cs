using Fluxzy.Rules;

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class RuleEditorDeserializeResult
    {
        public RuleEditorDeserializeResult(Rule? rule)
        {
            Rule = rule;
            Success = true;
            Errors = new();
        }

        public RuleEditorDeserializeResult(IEnumerable<RuleConfigReaderError> errors)
        {
            Rule = null;
            Success = false;
            Errors = errors.ToList();
        }

        public Rule?  Rule { get; }

        public bool Success { get;  }

        public List<RuleConfigReaderError> Errors { get; }
    }
}
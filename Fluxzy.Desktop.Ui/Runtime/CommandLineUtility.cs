namespace Fluxzy.Desktop.Ui.Runtime
{
    internal static class CommandLineUtility
    {
        public static bool TryGetArgsValue(string[] commandLineArgs, string parameterName, out string?  value)
        {
            value = null; 

            var parameterIndex = 
                Array.FindIndex(commandLineArgs, s => s.Equals(parameterName, StringComparison.OrdinalIgnoreCase));

            if (parameterIndex == -1)
                return false;

            if (commandLineArgs.Length <= parameterIndex + 1)
                return false;

            value =  commandLineArgs[parameterIndex + 1];
            
            return true; 
        }
    }


    internal static class ExceptionUtilities
    {
        public static Exception? FindException(this Exception original, Func<Exception, bool> predicate)
        {
            var current = original;

            while (current != null)
            {
                if (predicate(current))
                    return current;

                current = current.InnerException;
            }

            return null;
        }
    }
}
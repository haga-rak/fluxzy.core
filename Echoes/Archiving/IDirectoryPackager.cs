using System;
using System.IO;
using System.Threading.Tasks;

namespace Echoes
{
    public interface IDirectoryPackager
    {
        bool ShouldApplyTo(string fileName); 

        Task Pack(string directory, Stream outputStream);
    }

    public class EczDirectoryPackager : IDirectoryPackager
    {
        public bool ShouldApplyTo(string fileName)
        {
            return
                fileName.EndsWith(".ecz", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".eczip", StringComparison.CurrentCultureIgnoreCase) ||
                fileName.EndsWith(".ec.zip", StringComparison.CurrentCultureIgnoreCase) ; 
        }

        public async Task Pack(string directory, Stream outputStream)
        {
            await ZipHelper.Compress(new DirectoryInfo(directory),
                outputStream, fileInfo =>
                {
                    if (fileInfo.Length == 0)
                        return false;

                    if (!fileInfo.Name.EndsWith(".data")
                        && !fileInfo.Name.EndsWith(".json"))
                    {
                        return false; 
                    }

                    return true; 
                }); 
        }
    }
}
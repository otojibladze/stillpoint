using System.IO;

namespace Stillpoint
{
    static class IconGen
    {
        static void Main(string[] args) => File.WriteAllBytes(args[0], Icons.BuildIco(Icons.On));
    }
}

using System.Threading.Tasks;

namespace RunItBot
{
    class Program
    {
        public static async Task Main()
            => await new Startup().RunAsync();
    }
}

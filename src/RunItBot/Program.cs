using System.Threading.Tasks;

namespace RunIt
{
    class Program
    {
        public static async Task Main()
            => await new Startup().RunAsync();
    }
}

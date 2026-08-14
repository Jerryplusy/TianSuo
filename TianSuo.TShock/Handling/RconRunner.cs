using System.IO;
using TShockAPI;

namespace TianSuo.Handling
{
    public sealed class RconRunner
    {
        public string Run(string command)
        {
            var originalOut = Console.Out;
            var captured = new StringWriter();

            try
            {
                Console.SetOut(captured);
                Commands.HandleCommand(TSPlayer.Server, command);
            }
            catch (Exception ex)
            {
                captured.WriteLine(ex.Message);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            return captured.ToString().Trim();
        }
    }
}

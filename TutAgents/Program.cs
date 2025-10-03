namespace Tut.Agents;

internal static class Program
{
    private static void Main()
    {
        DriverAgent one = new DriverAgent("91023490066", "Pass@123");
        DriverAgent two = new DriverAgent("91023490044", "Pass@123");
        one.Start();
        two.Start();
        Console.ReadKey();
        one.Stop();
        two.Stop();
        
        Task.Delay(1000).GetAwaiter().GetResult();
        
    }
}

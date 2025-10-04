namespace Tut.Agents;

internal static class Program
{
    private static void Main()
    {
        DriverAgent one = new DriverAgent("91023490066", "Pass@123");
        DriverAgent two = new DriverAgent("91023490044", "Pass@123");
        UserAgent uone = new UserAgent("91023490066");
        one.Start();
        two.Start();
        uone.Start();
        
        Console.ReadKey();
        one.Stop();
        two.Stop();
        uone.Stop();
        
        Task.Delay(1000).GetAwaiter().GetResult();
        
    }
}

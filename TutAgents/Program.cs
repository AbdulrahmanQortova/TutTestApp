namespace Tut.Agents;

internal static class Program
{
    private static void Main(string[] args)
    {
        DriverAgent one = new DriverAgent(5);
        DriverAgent two = new DriverAgent(6);
        one.Start();
        two.Start();
        Console.ReadKey();
    }
}

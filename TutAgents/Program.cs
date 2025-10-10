namespace Tut.Agents;

internal static class Program
{
    private static void Main()
    {
        DriverAgent da1 = new DriverAgent("DA1", "Pass@123");
        DriverAgent da2 = new DriverAgent("DA2", "Pass@123");
        DriverAgent da3 = new DriverAgent("DA3", "Pass@123");
        DriverAgent da4 = new DriverAgent("DA4", "Pass@123");
        DriverAgent da5 = new DriverAgent("DA5", "Pass@123");
        UserAgent ua1 = new UserAgent("UA1");
        UserAgent ua2 = new UserAgent("UA2");
        UserAgent ua3 = new UserAgent("UA3");

        da1.Start();
        da2.Start();
        da3.Start();
        da4.Start();
        da5.Start();
        ua1.Start();
        ua2.Start();
        ua3.Start();
        
        
        Console.ReadKey();
        
        da1.Stop();
        da2.Stop();
        da3.Stop();
        da4.Stop();
        da5.Stop();
        ua1.Stop();
        ua2.Stop();
        ua3.Stop();
        
        
        Task.Delay(2000).GetAwaiter().GetResult();
        
    }
}

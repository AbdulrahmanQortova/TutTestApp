namespace Tut.Agents;

internal static class Program
{
    private static void Main()
    {

        const int numDrivers = 0;
        const int numUsers = 1;
        List<DriverAgent> drivers = [];
        List<UserAgent> users = [];
        
        for (int i = 0; i < numDrivers; i++)
        {
            drivers.Add(new DriverAgent($"DA{i+1}", "Pass@123"));
        }
        for (int i = 0; i < numUsers; i++)
        {
            users.Add(new UserAgent($"UA{i+1}"));
        }
        
        drivers.ForEach(d => d.Start());
        users.ForEach(u => u.Start());
        
        Console.ReadKey();
        
        drivers.ForEach(d => d.Stop());
        users.ForEach(u => u.Stop());
        
        Task.Delay(2000).GetAwaiter().GetResult();
        
    }
}

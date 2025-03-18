// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello from Builder app one!");
var start = DateTime.Now;
while (DateTime.Now < start.AddMinutes(5))
{
    Console.WriteLine("Builder is running...");
    Thread.Sleep(30000);
}

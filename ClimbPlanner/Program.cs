using System;

namespace ClimbPlanner
{
  public class Program
  {
    public static void Main(string[] args)
    {
      new PlanAnalyser(args[0]);

      Console.WriteLine("Hit 'Esc' to stop.");

      while (Console.ReadKey().Key != ConsoleKey.Escape);
    }
  }
}

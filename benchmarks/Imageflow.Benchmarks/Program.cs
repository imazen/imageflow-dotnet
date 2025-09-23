using BenchmarkDotNet.Running;
using Imageflow.Benchmarks;

class Program
{
    static void Main(string[] args)
            => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

// dotnet run --configuration Release --project .\benchmarks\Imageflow.Benchmarks\ --  --filter *Atomic*

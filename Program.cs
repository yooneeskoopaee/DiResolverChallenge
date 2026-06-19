using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dotnet9DiCommandRouter;

public interface IOperation
{
    string Execute();
}

public sealed class OperationA : IOperation
{
    public string Execute() => "OperationA executed";
}

public sealed class OperationB : IOperation
{
    public string Execute() => "OperationB executed";
}

public sealed class OperationResolver
{
    private readonly IServiceProvider _serviceProvider;

    public OperationResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IOperation Resolve(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));

        return input.Trim().ToLowerInvariant() switch
        {
            "a" => _serviceProvider.GetRequiredService<OperationA>(),
            "b" => _serviceProvider.GetRequiredService<OperationB>(),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input, "Only 'a' or 'b' are valid inputs.")
        };
    }
}

public sealed class Processor
{
    private readonly OperationResolver _resolver;

    public Processor(OperationResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public void Process(string input)
    {
        var operation = _resolver.Resolve(input);
        Console.WriteLine(operation.Execute());
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddTransient<OperationA>();
                services.AddTransient<OperationB>();
                services.AddTransient<OperationResolver>();
                services.AddTransient<Processor>();
            })
            .Build();

        var processor = host.Services.GetRequiredService<Processor>();

        Console.Write("Enter impl: a or b > ");
        var userInput = Console.ReadLine();

        try
        {
            processor.Process(userInput!);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}

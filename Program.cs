using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiResolverChallenge;

// --- Abstractions ---
public interface IOperation
{
    string Execute();
}

// --- Implementations ---
public sealed class OperationA : IOperation
{
    public string Execute() => "OperationA executed";
}

public sealed class OperationB : IOperation
{
    public string Execute() => "OperationB executed";
}

// --- Senior Level Resolver (No Service Locator Anti-Pattern) ---
// ما اینجا از Keyed Services استفاده می‌کنیم که در دات‌نت 8 و 9 معرفی شده است.
public sealed class OperationResolver([FromKeyedServices("OperationFactory")] Func<string, IOperation?> factory)
{
    public IOperation Resolve(string input)
    {
        var operation = factory(input?.ToLowerInvariant() ?? string.Empty);
        
        return operation ?? throw new ArgumentException($"Operation '{input}' is not supported.");
    }
}

public sealed class Processor(OperationResolver resolver)
{
    public void Process(string input)
    {
        var op = resolver.Resolve(input);
        Console.WriteLine(op.Execute());
    }
}

// --- Composition Root ---
public static class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // 1. ثبت پیاده‌سازی‌ها به صورت معمولی
                services.AddTransient<OperationA>();
                services.AddTransient<OperationB>();

                // 2. استفاده از یک Delegate Factory به عنوان Keyed Service
                // این بخش هوشمندانه‌ترین قسمت کد برای یک معمار دات‌نت است.
                services.AddKeyedSingleton<Func<string, IOperation?>>("OperationFactory", (sp, key) => 
                {
                    return (input) => input switch
                    {
                        "a" => sp.GetRequiredService<OperationA>(),
                        "b" => sp.GetRequiredService<OperationB>(),
                        _ => null
                    };
                });

                services.AddTransient<OperationResolver>();
                services.AddTransient<Processor>();
            })
            .Build();

        var processor = host.Services.GetRequiredService<Processor>();

        Console.Write("Enter impl: a or b > ");
        var userInput = Console.ReadLine();

        try
        {
            processor.Process(userInput ?? string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

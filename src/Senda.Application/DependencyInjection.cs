using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Senda.Application.Commands;
using Senda.Application.Services;
using Senda.Application.Validators;

namespace Senda.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IngestDocumentCommand).Assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<IngestDocumentCommandValidator>();

        return services;
    }
}

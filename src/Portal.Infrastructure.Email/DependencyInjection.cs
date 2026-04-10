using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Email;

public static class DependencyInjection
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        var smtpSection = configuration.GetSection("Smtp");

        if (smtpSection.Exists() && !string.IsNullOrEmpty(smtpSection["Host"]))
        {
            var settings = new SmtpSettings();
            smtpSection.Bind(settings);
            services.AddSingleton(settings);
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddTransient<IEmailSender, NullEmailSender>();
        }

        return services;
    }
}

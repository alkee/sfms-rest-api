using Microsoft.Extensions.DependencyInjection;

namespace sfms_rest_api;

public static class IMvcBuilderExt
{
    public static IMvcBuilder AddSmfsController(this IMvcBuilder builder, string routePrefix = "api/Sfms")
    {
        return builder
            .AddApplicationPart(typeof(SfmsController).Assembly)
            .AddMvcOptions(options =>
        {
            options.Conventions.Add(new RoutingOption(routePrefix));
        });
    }
}

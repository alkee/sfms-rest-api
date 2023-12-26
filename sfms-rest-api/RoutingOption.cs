using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace sfms_rest_api;


public class RoutingOption
    : IApplicationModelConvention
{ // https://stackoverflow.com/questions/56619628/asp-net-core-web-api-extending-attribute-route-generation
    public RoutingOption(string routePrefix)
    {
        this.routePrefix = routePrefix;
    }

    private readonly string routePrefix;

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerType != typeof(SfmsController))
                continue;
            controller.Selectors[0].AttributeRouteModel = new AttributeRouteModel()
            {
                Template = routePrefix
            };
        }
    }
}

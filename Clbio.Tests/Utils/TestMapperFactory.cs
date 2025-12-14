using AutoMapper;
using Clbio.Application.Mappings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq; 

namespace Clbio.Tests.Utils;

public static class TestMapperFactory
{
    public static IMapper Create()
    {
        var loggerFactory = new NullLoggerFactory();
        
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(GeneralProfile).Assembly);
        }, loggerFactory);

        //config.AssertConfigurationIsValid(); 

        return config.CreateMapper();
    }
}
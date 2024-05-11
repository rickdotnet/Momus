using Momus;
using Setup = Momus.Host.Setup;

var momus =
    WebApplication.CreateBuilder(args)
        .BuildMomusWebApplication(
            Setup.ConfigureBuilder,
            Setup.ConfigureApplication
        );

momus.Run();
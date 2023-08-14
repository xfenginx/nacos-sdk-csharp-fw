namespace Nacos.AspNetCore.ApiTests7
{
    using Nacos.AspNetCore.V2;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            ////services.AddNacosAspNet(builder.Configuration, "Nacos");

            builder.Services.AddNacosAspNet(d =>
            {
                d.ServerAddresses = new List<string> { "http://110.188.24.28:8848/" };
                d.Namespace = "dev";
                d.ServiceName = "s1";
                d.GroupName = "DEFAULT_GROUP";
                d.ClusterName = "DEFAULT";
                d.Ip = "";
                d.PreferredNetworks = "";
                d.Port = 0;
                d.Weight = 100;
                d.RegisterEnabled = true;
                d.InstanceEnabled = true;
                d.Ephemeral = true;
                d.Secure = false;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}

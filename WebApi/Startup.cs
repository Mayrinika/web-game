using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebGame.Domain;
using WebApi.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace WebApi
{

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddMvc(options => { })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddMvc(options =>
            {
                // Этот OutputFormatter позволяет возвращать данные в XML, если требуется.
                options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                // Эта настройка позволяет отвечать кодом 406 Not Acceptable на запросы неизвестных форматов.
                options.ReturnHttpNotAcceptable = true;
            })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
            });
            Mapper.Initialize(cfg =>
            {
              // Регистрация преобразования UserEntity в UserDto с дополнительным правилом.
              // Также поля и свойства с совпадающими именами будут скопировны (поведение по умолчанию).
              cfg.CreateMap<UserEntity, UserDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
              // Регистрация преобразования UserToUpdateDto в UserEntity без дополнительных правил.
              // Все поля и свойства с совпадающими именами будут скопировны (поведение по умолчанию).
              cfg.CreateMap<NewUserDto, UserEntity>();
              cfg.CreateMap<PutDto, UserEntity>();
          });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}

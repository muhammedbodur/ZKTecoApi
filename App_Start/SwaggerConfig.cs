using System.Web.Http;
using ZKTecoApi;
using System.IO;
using System;
using System.Web;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace ZKTecoApi
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "ZKTeco API")
                        .Description("ZKTeco cihazlarını yönetmek için RESTful API. Kullanıcı yönetimi, yoklama kayıtları, cihaz kontrolü ve gerçek zamanlı event'ler için kapsamlı endpoint'ler.")
                        .Contact(cc => cc
                            .Name("ZKTeco API Team")
                            .Email("support@example.com"))
                        .License(ll => ll
                            .Name("Apache 2.0")
                            .Url("https://www.apache.org/licenses/LICENSE-2.0"));

                    // XML Documentation dosyasını ekle
                    var xmlPath = GetXmlCommentsPath();
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }

                    // Route prefix'i kaldır
                    c.RootUrl(req => req.RequestUri.GetLeftPart(UriPartial.Authority) + req.GetRequestContext().VirtualPathRoot.TrimEnd('/'));

                    // Enum'ları string olarak göster
                    c.DescribeAllEnumsAsStrings();
                })
                .EnableSwaggerUi(c =>
                {
                    c.DocumentTitle("ZKTeco API Documentation");
                    c.DocExpansion(DocExpansion.List);
                    c.EnableDiscoveryUrlSelector();
                });
        }

        private static string GetXmlCommentsPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var xmlFile = "ZKTecoApi.xml";
            var xmlPath = Path.Combine(baseDirectory, "bin", xmlFile);

            if (!File.Exists(xmlPath))
            {
                xmlPath = Path.Combine(baseDirectory, xmlFile);
            }

            return xmlPath;
        }
    }
}

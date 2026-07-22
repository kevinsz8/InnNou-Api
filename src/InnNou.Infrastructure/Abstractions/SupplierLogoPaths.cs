using Microsoft.Extensions.Configuration;

namespace InnNou.Infrastructure.Abstractions
{
    // Shared by LocalSupplierLogoStorage (writes files here) and Program.cs (serves them via
    // static-files middleware) — both must resolve the exact same physical folder.
    public static class SupplierLogoPaths
    {
        public const string PublicUrlPrefix = "/uploads/supplier-logos";

        public static string ResolvePhysicalBasePath(IConfiguration configuration)
        {
            var relative = configuration["FileStorage:SupplierLogosPath"] ?? "UploadedFiles/SupplierLogos";
            return Path.IsPathRooted(relative) ? relative : Path.Combine(Directory.GetCurrentDirectory(), relative);
        }
    }
}
